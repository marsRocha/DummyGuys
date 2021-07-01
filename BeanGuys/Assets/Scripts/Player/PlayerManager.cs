using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public int id;
    public string username;

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
    private Vector3 impact = Vector3.zero, point = Vector3.zero;

    [Range(0,1)]
    public float packet_loss_percent;

    [Header("State")]
    public bool Online = false;
    public bool Running = false;
    public bool Ragdolled = false;

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

        //TODO: DEBUG
        MapController.instance.StartRace();
    }

    public void Initialize(int _id, string _username)
    {
        id = _id;
        username = _username;
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
        //ragdollController.FixedUpdateController();

        logicTimer.Update();
    }

    private void FixedTime()
    {
        if (Running)
        {
            //Update player's movement
            ProcessInput(currentInputState);

            if(Random.value > packet_loss_percent)
            {
                if (Online)
                    SendMovement();
            }

            // Reconciliate
            if (serverSimulationState != null) Reconciliate();

            // Determine current simulationState
            SimulationState simulationState = new SimulationState(currentInputState.SimulationFrame, transform.position, transform.rotation, velocity, Ragdolled);

            int cacheSlot = simulationFrame % CACHE_SIZE;

            // Store the SimulationState into the simulationStateCache 
            simStateCache[cacheSlot] = simulationState;

            // Store the ClientInputState into the inputStateCache
            inputStateCache[cacheSlot] = currentInputState;

            // Move next frame
            ++simulationFrame;

            // Add position to interpolate
            //playerManager.interpolation.PlayerUpdate(simulationFrame, transform.position);
        }
    }

    private void ProcessInput(ClientInputState currentInputs)
    {
        if(Ragdolled)
            rb.freezeRotation = false;
        rb.isKinematic = false;
        rb.velocity = velocity;
        rb.angularVelocity = angularVelocity;

        if(impact != Vector3.zero && point != Vector3.zero)
        {
            rb.freezeRotation = false;
            Ragdolled = true;
            playerController.EnterRagdoll(point);
            //ragdollController.RagdollIn();

            rb.AddForceAtPosition(impact, point, ForceMode.Impulse);
            impact = Vector3.zero; point = Vector3.zero;
        }

        playerController.UpdateController(currentInputs);

        //Simulate physics
        SimulatePhysics();
    }

    private void SimulatePhysics()
    {
        Physics.Simulate(logicTimer.FixedDeltaTime);

        velocity = rb.velocity;
        angularVelocity = rb.angularVelocity;
        rb.isKinematic = true;
        if (!Ragdolled)
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
        // Sanity check, don't reconciliate for old states.
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

        // If the simulation time isnt equal to the serve time then return
        // this should never happen
        if (cachedInputState.SimulationFrame != serverSimulationState.simulationFrame || cachedSimulationState.simulationFrame != serverSimulationState.simulationFrame)
            return;

        // Show warning about misprediction
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
            SimulationState rewoundSimulationState = new SimulationState(rewindFrame, transform.position, transform.rotation, velocity, Ragdolled);
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

        //TODO: ADD RESET TO RAGDOLL IF NEEDED 
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

    private void OnCollisionEnter(Collision collision)
    {
        if (ragdollController.enabled)
        {
            if (ragdollController.State == RagdollState.Animated)
            {
                Vector3 collisionDirection = collision.contacts[0].normal;

                if (collision.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
                {
                    // Add obstacle extra force
                    impact = collisionDirection * ragdollController.ObstacleModifier;
                    point = collision.contacts[0].point;

                    return;
                }
                else if (collision.gameObject.layer == LayerMask.NameToLayer("Bounce"))
                {
                    // Add obstacle extra force
                    rb.AddForceAtPosition(collisionDirection * ragdollController.BounceModifier, collision.contacts[0].point, ForceMode.Impulse);
                    velocity = rb.velocity;

                    playerController.jumpPs.Play();

                    return;
                }
                else if (collision.gameObject.layer == LayerMask.NameToLayer("Floor"))
                {
                    if (collision.impulse.magnitude >= 20)
                    {
                        playerController.EnterRagdoll(collision.contacts[0].point);
                    }

                    return;
                }

                Debug.Log("collision force:" + collision.impulse.magnitude);
                Debug.Log("collision relative Velocity:" + collision.relativeVelocity.magnitude);
                if (collision.impulse.magnitude >= ragdollController.MinForce || (playerController.jumping || playerController.diving))
                {
                    playerController.EnterRagdoll(collision.contacts[0].point);
                    // Add extra force
                    rb.AddForceAtPosition(collisionDirection * ragdollController.Multiplier, collision.contacts[0].point, ForceMode.Impulse);
                }
            }
        }
    }

    private void OnApplicationQuit()
    {
        logicTimer.Stop();
    }
}