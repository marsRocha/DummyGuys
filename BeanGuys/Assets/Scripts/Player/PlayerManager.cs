using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public int Id;
    public string Username;
    public int Checkpoint;

    // Components
    private PlayerController playerController;
    private RagdollController ragdollController;
    private Transform playerCamera;
    private LogicTimer logicTimer;
    private Rigidbody rb;

    // Store previous player stuff
    private const int CACHE_SIZE = 1024;
    private SimulationState[] simStateCache;
    private ClientInputState[] inputStateCache;
    public int simulationFrame { get; private set; }

    private ClientInputState currentInputState;

    [Header("Correction")]
    private SimulationState serverSimulationState;
    private int lastCorrectedFrame;

    private Vector3 velocity, angularVelocity;

    [Range(0,1)]
    public float packet_loss_percent;

    [Header("State")]
    public bool Online = false;
    public bool Running = false;
    public bool Ragdolled = false;
    public bool _Debug = false;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        ragdollController = GetComponent<RagdollController>();
        playerCamera = GameObject.Find("Main Camera").transform;
        
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.isKinematic = true;

        simulationFrame = 0;
        lastCorrectedFrame = 0;
        velocity = Vector3.zero;
        angularVelocity = Vector3.zero;

        currentInputState = new ClientInputState();
        serverSimulationState = new SimulationState();
        simStateCache = new SimulationState[CACHE_SIZE];
        inputStateCache = new ClientInputState[CACHE_SIZE];
    }

    void Start()
    {
        logicTimer = new LogicTimer(() => FixedTime());
        logicTimer.Start();

        playerController.StartController(logicTimer);
        ragdollController.StartController();

        // TODO: REMOVE ON FINAL
        if(_Debug)
            MapController.instance.StartRace();
    }

    public void Initialize(int _id, string _username)
    {
        Id = _id;
        Username = _username;
    }

    void Update()
    {
        // Set correspoding buttons
        bool jump = false, dive = false;
        if (Input.GetKey(KeyCode.Space))
            jump = true;
        if (Input.GetKey(KeyCode.E))
            dive = true;

        currentInputState = new ClientInputState
        {
            Tick = GameLogic.Tick,
            SimulationFrame = simulationFrame,
            HorizontalAxis = Input.GetAxisRaw("Horizontal"),
            VerticalAxis = Input.GetAxisRaw("Vertical"),
            Jump = jump,
            Dive = dive,
            LookingRotation = playerCamera.transform.rotation
        };

        ragdollController.UpdateController();

        logicTimer.Update();
    }

    private void FixedTime()
    {
        if (Running)
        {
            // Update player's movement
            ProcessInput(currentInputState);

            if(Random.value > packet_loss_percent)
            {
                if (Online)
                    SendMovement();
            }

            // Reconciliate
            if (serverSimulationState != null) Reconciliate();

            // Determine current simulationState
            SimulationState simulationState = new SimulationState(currentInputState.SimulationFrame, transform.position, transform.rotation, velocity, angularVelocity, Ragdolled);

            int cacheSlot = simulationFrame % CACHE_SIZE;

            // Store the SimulationState into the simulationStateCache 
            simStateCache[cacheSlot] = simulationState;

            // Store the ClientInputState into the inputStateCache
            inputStateCache[cacheSlot] = currentInputState;

            // Move next frame
            ++simulationFrame;
        }
    }

    private void ProcessInput(ClientInputState currentInputs)
    {
        if(Ragdolled)
            rb.freezeRotation = false;
        rb.isKinematic = false;
        rb.velocity = velocity;
        rb.angularVelocity = angularVelocity;

        playerController.UpdateController(currentInputs);

        // Simulate physics
        SimulatePhysics();
    }

    private void SimulatePhysics()
    {
        Physics.Simulate(logicTimer.FixedDeltaTime);

        velocity = rb.velocity;
        angularVelocity = rb.angularVelocity;
        rb.isKinematic = true;
        rb.freezeRotation = true;
    }

    private void SendMovement()
    {
        // Send player input/state to the server
        ClientSend.PlayerMovement(currentInputState, transform.position, transform.rotation, Ragdolled);
        // Send player state to the peers
        ClientSend.PlayerMovement(new PlayerState(currentInputState.Tick, transform.position, transform.rotation, Ragdolled, playerController.currentAnimation));
    }

    #region Client-Server Reconciliation
    private void Reconciliate()
    {
        // Don't reconciliate for old states.
        if (serverSimulationState.simulationFrame <= lastCorrectedFrame) return;

        // Determine the cache index 
        int cacheIndex = serverSimulationState.simulationFrame % CACHE_SIZE;

        // Obtain the cached input and simulation states.
        ClientInputState cachedInputState = inputStateCache[cacheIndex];
        SimulationState cachedSimulationState = simStateCache[cacheIndex];

        // If there's missing cache data for either input or simulation 
        // snap the player's position to match the server.
        if (cachedInputState == null || cachedSimulationState == null)
        {
            SetPlayerToSimulationState(serverSimulationState);

            // Set the last corrected frame to equal the server's frame.
            lastCorrectedFrame = serverSimulationState.simulationFrame;
            return;
        }

        // If the simulation time isnt equal to the serve time then return, this should never happen
        if (cachedInputState.SimulationFrame != serverSimulationState.simulationFrame || cachedSimulationState.simulationFrame != serverSimulationState.simulationFrame)
            return;

        Debug.LogWarning("Client misprediction at frame " + serverSimulationState.simulationFrame + ".");

        // Set the player's position to match the server's state. 
        SetPlayerToSimulationState(serverSimulationState);

        // Declare the rewindFrame as we're about to resimulate our cached inputs. 
        int rewindFrame = serverSimulationState.simulationFrame;

        // Loop through and apply cached inputs until we're 
        // caught up to our current simulation frame. 
        while (rewindFrame < simulationFrame)
        {
            // Determine the cache index 
            int rewindCacheIndex = rewindFrame % CACHE_SIZE;

            // Obtain the cached input and simulation states.
            ClientInputState rewindCachedInputState = inputStateCache[rewindCacheIndex];
            SimulationState rewindCachedSimulationState = simStateCache[rewindCacheIndex];

            // If there's no state to simulate, for whatever reason, 
            // increment the rewindFrame and continue.
            if (rewindCachedInputState == null || rewindCachedSimulationState == null)
            {
                ++rewindFrame;
                continue;
            }

            // Process the cached inputs. 
            ProcessInput(rewindCachedInputState);

            // Replace the simulationStateCache index with the new value.
            SimulationState rewoundSimulationState = new SimulationState(rewindFrame, transform.position, transform.rotation, velocity, angularVelocity, Ragdolled);
            simStateCache[rewindCacheIndex] = rewoundSimulationState;

            // Increase the amount of frames that we've rewound.
            ++rewindFrame;
        }

        // Once we're complete, update the lastCorrectedFrame to match.
        // NOTE: Set this even if there's no correction to be made. 
        lastCorrectedFrame = serverSimulationState.simulationFrame;
    }

    public void SetPlayerToSimulationState(SimulationState simulationState)
    {
        transform.position = simulationState.position;
        transform.rotation = simulationState.rotation;
        velocity = simulationState.velocity;
        angularVelocity = simulationState.angularVelocity;

        /*if(Ragdolled != simulationState.ragdoll)
        {
            if (!simulationState.ragdoll)
                ragdollController.RagdollOut();

            Ragdolled = simulationState.ragdoll;
        }*/
    }
    #endregion

    public void ReceivedCorrectionState(SimulationState simulationState)
    {
        if (serverSimulationState?.simulationFrame < simulationState.simulationFrame)
            serverSimulationState = simulationState;
    }

    public void Respawn(Vector3 _position, Quaternion _rotation)
    {
        rb.isKinematic = true;
        transform.position = _position;
        transform.rotation = _rotation;
        velocity = Vector3.zero;

        // Go back to animated state if not already
        ragdollController.BackToAnimated();
        rb.isKinematic = false;

        playerController.Respawn();
    }

    public void SetCheckpoint(int _checkpointIndex)
    {
        Checkpoint = _checkpointIndex;
        playerController.Checkpoint();
    }

    private void OnApplicationQuit()
    {
        logicTimer.Stop();
    }
}