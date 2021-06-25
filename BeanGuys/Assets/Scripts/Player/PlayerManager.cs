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

    private Vector3 velocity;

    [Header("State")]
    public bool isOnline = false;
    public bool isRunning = false;
    public bool isRagdoled = false;
    public int multiplier;

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
            Tick = GlobalVariables.clientTick,
            SimulationFrame = simulationFrame,
            HorizontalAxis = Input.GetAxisRaw("Horizontal"),
            VerticalAxis = Input.GetAxisRaw("Vertical"),
            Jump = jump,
            Dive = dive,
            LookingRotation = playerCamera.transform.rotation
        };

        logicTimer.Update();
    }

    private void FixedTime()
    {
        if (isRunning)
        {
            //Update player's movement
            ProcessInput(currentInputState);

            //Simulate physics
            SimulatePhysics();

                //Send to server
                ClientSend.PlayerMovement(currentInputState, transform.position, transform.rotation);
                //Sent to peers
                ClientSend.PlayerMovement(new SimulationState(transform.position, transform.rotation, velocity, currentInputState.SimulationFrame));

            // Reconciliate
            if (serverSimulationState != null) Reconciliate();

            // Determine current simulationState
            SimulationState simulationState = new SimulationState(transform.position, transform.rotation, velocity, currentInputState.SimulationFrame);

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
        rb.isKinematic = false;

        rb.velocity = velocity;

        playerController.UpdateController(currentInputs);
    }

    private void SimulatePhysics()
    {
        Physics.Simulate(logicTimer.FixedDeltaTime);

        velocity = rb.velocity;
        rb.isKinematic = true;
    }

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

            //Simulate physics
            SimulatePhysics();

            // Replace the simulationStateCache index with the new value.
            SimulationState rewoundSimulationState = new SimulationState(transform.position, transform.rotation, velocity, rewindFrame);
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
    }

    public void ReceivedCorrectionState(SimulationState simulationState)
    {
        if (serverSimulationState?.simulationFrame < simulationState.simulationFrame)
            serverSimulationState = simulationState;
    }

    public void Respawn(Vector3 _position, Quaternion _rotation)
    {
        transform.position = _position;
        transform.rotation = _rotation;
        velocity = Vector3.zero;

        playerController.Respawn();
    }

    /*
    private void OnCollisionEnter(Collision collision)
    {
        if (ragdollController.enabled)
        {
            if (collision.gameObject.layer != LayerMask.NameToLayer("Floor") && ragdollController.state == RagdollState.Animated)
            {
                // Calculate direction of the collision
                Vector3 direction = collision.contacts[0].normal;

                if (collision.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
                {
                    isRagdoled = true;

                    //folow pelvis rigidbody velocity

                    return;
                }

                //Debug.Log("collision force:" + collision.impulse.magnitude);
                //Debug.Log("collision relative Velocity:" + collision.relativeVelocity.magnitude);
                if (collision.impulse.magnitude >= ragdollController.maxForce || (playerController.jumping || playerController.diving))
                {
                    //bumpPs.transform.position = collision.contacts[0].point;
                    //bumpPs.Play();

                    //ragdollController.RagdollIn();

                    rb.isKinematic = false;
                    rb.freezeRotation = false;

                    Debug.Log("1    " + rb.velocity);
                    rb.velocity = velocity;

                    rb.AddForceAtPosition(direction * (ragdollController.impactForce * multiplier), collision.contacts[0].point, ForceMode.Impulse);

                    velocity = rb.velocity;
                    Debug.Log("2    " + rb.velocity);

                    rb.freezeRotation = true;
                    rb.isKinematic = true;
                }
            }
        }
    }*/

    /*if (collision.gameObject.layer == LayerMask.NameToLayer("Bounce") && (!playerController.dashing || !playerController.dashTriggered))
            {
                // Calculate the direction of the collision force
                Vector3 direction = (collision.contacts[0].point - transform.position).normalized * -1;
                // Collision point
                Vector3 point = collision.contacts[0].point;

                playerController.ActivateDash(direction, point);
            }

            // nao entrar em ragdoll quando bate num "Bounce" obj e quando salta para cima dum cushion
            if ((collision.gameObject.layer != LayerMask.NameToLayer("Floor") && collision.gameObject.layer != LayerMask.NameToLayer("Wall")
                && collision.gameObject.layer != LayerMask.NameToLayer("Bounce")) && ragdollController.state == RagdollState.Animated)
            {
                Vector3 collisionDirection = collision.contacts[0].normal;

                //Debug.Log("collision force:" + collision.impulse.magnitude);
                //Debug.Log("collision relative Velocity:" + collision.relativeVelocity.magnitude);
                if (collision.impulse.magnitude >= ragdollController.maxForce || (playerController.jumping || playerController.diving))
                {
                    //bumpPs.transform.position = collision.contacts[0].point;
                    //bumpPs.Play();

                    ragdollController.RagdollIn();

                    rb.isKinematic = false;
                    rb.freezeRotation = false;

                    rb.velocity = velocity;

                    rb.AddForceAtPosition(-collisionDirection* (ragdollController.impactForce* 3), collision.contacts[0].point, ForceMode.Impulse);

                    velocity = rb.velocity;

                    rb.freezeRotation = true;
                    rb.isKinematic = true;
                }
            }*/

    private void OnApplicationQuit()
    {
        logicTimer.Stop();
    }
}

/// Added isOline to ClientSend messages
/// Added rb.freezeRotation = false; and true on processinputs and on simulatephysics
