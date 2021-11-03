using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    public string Username;
    public int Checkpoint;

    // Components
    private PlayerInput playerInput;
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
    private PlayerState serverSimulationState;
    private int lastCorrectedFrame;

    private Vector3 velocity, angularVelocity;

    [Header("State")]
    public bool Online = false;
    [SerializeField]
    private bool Running = false;
    public bool Ragdolled = false;
    public bool PlayerInteraction = false;
    public bool _Debug = false;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
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
        serverSimulationState = new PlayerState();
        simStateCache = new SimulationState[CACHE_SIZE];
        inputStateCache = new ClientInputState[CACHE_SIZE];
    }

    void Start()
    {
        logicTimer = new LogicTimer(() => FixedTime());
        logicTimer.Start();

        playerController.StartController(logicTimer);
        ragdollController.StartController();
    }

    public void Initialize(string _username, bool _playerInteraction)
    {
        Username = _username;
        PlayerInteraction = _playerInteraction;
    }

    public void StartPlayer()
    {
        Running = true;
    }

    void Update()
    {
        // Set correspoding buttons
        bool jump = false, dive = false;
        if (Input.GetKey(playerInput.JumpKey))
            jump = true;
        if (Input.GetKey(playerInput.DiveKey))
            dive = true;

        currentInputState = new ClientInputState
        {
            Tick = MapController.instance.gameLogic.Tick,
            SimulationFrame = simulationFrame,
            ForwardAxis = Input.GetAxisRaw(playerInput.ForwardAxis),
            LateralAxis = Input.GetAxisRaw(playerInput.LateralAxis),
            Jump = jump,
            Dive = dive,
            LookingRotation = playerCamera.transform.rotation
        };

        //Actions
        if (Input.GetKeyDown(playerInput.RespawnKey))
            ClientSend.PlayerRespawn();

        // Player Actions
        if (PlayerInteraction)
        {
            // Grab player
            if (Input.GetKey(playerInput.GrabKey) && !playerController.grabbing)
            {
                if (playerController.TryGrab())
                {
                    ClientSend.PlayerGrab(MapController.instance.gameLogic.Tick);
                }
                else if (playerController.grabbing)
                {
                    ClientSend.PlayerLetGo(playerController.grabbedObj);
                    //playerController.LetGo();
                }
            }
            else if (Input.GetKeyUp(playerInput.GrabKey))
            {
                if (playerController.grabbing)
                    ClientSend.PlayerLetGo(playerController.grabbedObj);
                //playerController.LetGo();
            }
            // Push player
            if (Input.GetKeyUp(playerInput.PushKey))
            {
                if (playerController.TryPush())
                    ClientSend.PlayerPush(MapController.instance.gameLogic.Tick);
            }
        }

        ragdollController.UpdateController();
        logicTimer.Update();
    }

    private void FixedTime()
    {
        if (Running)
        {
            // Update player's movement
            ProcessInput(currentInputState);

            if (Online)
                SendMovement();

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
        Ragdolled = playerController.ragdolled;
        if (Ragdolled)
            rb.freezeRotation = false;
        rb.isKinematic = false;
        rb.velocity = velocity;
        rb.angularVelocity = angularVelocity;

        playerController.UpdateController(currentInputs);
        Ragdolled = playerController.ragdolled;
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
        // Send player state to the network
        ClientSend.PlayerMovement(new PlayerState(currentInputState.Tick, transform.position, transform.rotation, Ragdolled, playerController.currentAnimation));
    }

    #region Client-Server Reconciliation
    private void Reconciliate()
    {
        // Don't reconciliate for old states.
        if (serverSimulationState.tick <= lastCorrectedFrame) return;

        // Determine the cache index 
        int cacheIndex = serverSimulationState.tick % CACHE_SIZE;

        // Obtain the cached input and simulation states.
        ClientInputState cachedInputState = inputStateCache[cacheIndex];
        SimulationState cachedSimulationState = simStateCache[cacheIndex];

        // If there's missing cache data for either input or simulation 
        // snap the player's position to match the server.
        if (cachedInputState == null || cachedSimulationState == null)
        {
            SetPlayerToSimulationState(serverSimulationState);

            // Set the last corrected frame to equal the server's frame.
            lastCorrectedFrame = serverSimulationState.tick;
            return;
        }

        // If the simulation time isnt equal to the serve time then return, this should never happen
        if (cachedInputState.SimulationFrame != serverSimulationState.tick || cachedSimulationState.simulationFrame != serverSimulationState.tick)
            return;

        Debug.LogWarning("Client misprediction at frame " + serverSimulationState.tick + ".");

        // Set the player's position to match the server's state. 
        SetPlayerToSimulationState(serverSimulationState);

        // Declare the rewindFrame as we're about to resimulate our cached inputs. 
        int rewindFrame = serverSimulationState.tick;

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

            // Process the cached inputs. Simulate movement (and related), and physics.
            ProcessInput(rewindCachedInputState);

            // Replace the simulationStateCache index with the new value.
            SimulationState rewoundSimulationState = new SimulationState(rewindFrame, transform.position, transform.rotation, velocity, angularVelocity, Ragdolled);
            simStateCache[rewindCacheIndex] = rewoundSimulationState;

            // Increase the amount of frames that we've rewound.
            ++rewindFrame;
        }

        // Once we're complete, update the lastCorrectedFrame to match.
        // NOTE: Set this even if there's no correction to be made. 
        lastCorrectedFrame = serverSimulationState.tick;
    }

    public void SetPlayerToSimulationState(PlayerState _playerState)
    {
        transform.position = _playerState.position;
        transform.rotation = _playerState.rotation;

        /*if(Ragdolled != simulationState.ragdoll)
        {
            if (!simulationState.ragdoll)
                ragdollController.RagdollOut();

            Ragdolled = simulationState.ragdoll;
        }*/
    }
    #endregion

    public void ReceivedCorrectionState(PlayerState _playerState)
    {
        if (serverSimulationState?.tick < _playerState.tick)
            serverSimulationState = _playerState;
    }

    public void ReceivedRespawn(Vector3 _position, Quaternion _rotation)
    {
        rb.isKinematic = true;
        transform.position = _position;
        transform.rotation = _rotation;
        velocity = Vector3.zero;

        // Go back to animated state if not already
        playerController.ExitRagdoll();
        rb.isKinematic = false;

        playerController.Respawn();
    }

    public void SetCheckpoint(int _checkpointIndex)
    {
        Checkpoint = _checkpointIndex;
        playerController.Checkpoint();
    }

    /// <summary> Local player is grabbing someone</summary>
    /// <param name="_playerGrabbed">player being grabbed</param>
    public void ReceivedGrabbing(Guid _playerGrabbed)
    {
        playerController.Grab(_playerGrabbed);
    }

    /// <summary> Local player is being grabbed</summary>
    public void ReceivedGrabbed()
    {
        playerController.Grabbed(true);
    }

    /// <summary> Local player has been freed</summary>
    public void ReceivedFreed()
    {
        playerController.Grabbed(false);
    }

    /// <summary> Local player has leet go of someone</summary>
    public void ReceivedLetGo()
    {
        playerController.LetGo();
    }

    /// <summary> Local player was pushed </summary>
    /// <param name="_direction">direction of push</param>
    public void ReceivedPushed(Vector3 _direction)
    {
        playerController.Pushed(_direction);
    }

    /// <summary> Local player pushed another player </summary>
    public void ReceivedPushing()
    {
        playerController.Pushing();
    }

    public void StopPlayer()
    {
        Running = false;
    }

    private void OnApplicationQuit()
    {
        logicTimer.Stop();
    }
}