using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(Rigidbody))]

//Controls player's network related functions, while minimizing the impact on the game code 
public class PlayerManager : MonoBehaviour
{
    private Transform playerCamera;
    private PlayerController playerController;
    private PlayerInput playerInput;
    private Rigidbody rb;

    //Store previous player stuff
    private const int CACHE_SIZE = 1024;
    private SimulationState[] simStateCache;
    private ClientInputState[] inputStateCache;
    private int simulationFrame = 0;

    private ClientInputState currentInputState;

    [Header("Correction")]
    private SimulationState serverSimulationState;
    private int lastCorrectedFrame;

    [Header("States")]
    public bool isOnline;
    public bool isRunning;

    // Start is called before the first frame update
    void Start()
    {
        playerCamera = GameObject.Find("Main Camera").transform;
        rb = GetComponent<Rigidbody>();
        playerController = GetComponent<PlayerController>();
        playerInput = GetComponent<PlayerInput>();

        simulationFrame = 0;
        lastCorrectedFrame = 0;

        currentInputState = new ClientInputState();
        serverSimulationState = new SimulationState();
        simStateCache = new SimulationState[CACHE_SIZE];
        inputStateCache = new ClientInputState[CACHE_SIZE];

        playerController.StartController();
    }

    // Update is called once per frame
    private void Update()
    {
        if (isRunning)
        {
            playerInput.MovementInput();
            playerController.UpdateController();
        }
    }

    private void FixedUpdate()
    {
        if (isRunning)
        {
            currentInputState = new ClientInputState(MapController.instance.Game_Clock, simulationFrame, playerInput.GetInputs(), playerCamera.transform.rotation);

            //Update player's movement
            playerController.FixedUpdateController(currentInputState);

            ClientSend.PlayerMovement(currentInputState, rb.position, rb.rotation);
            ClientSend.PlayerMovement(rb.position, rb.rotation, rb.velocity, rb.angularVelocity, currentInputState.Tick);

            /*if (lastAnimSent != pController.currentAnim)
            {
                ClientSend.PlayerAnim((int)pController.currentAnim);
                lastAnimSent = pController.currentAnim;
            }*/

            // Reconciliate
            if (serverSimulationState != null) Reconciliate();

            // Determine current simulationState
            SimulationState simulationState = new SimulationState(rb.position, rb.rotation, rb.velocity, currentInputState.SimulationFrame);

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

    public void Respawn( Vector3 position, Quaternion rotation)
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.position = position;
        rb.rotation = rotation;

        playerController.Respawn();
    }

    // make sure if there are any newer state messages available, we use those instead
    // We received a new simualtion state, overwrite it
    public void ReceivedCorrectionState(SimulationState simulationState)
    {
        if (serverSimulationState?.simulationFrame < simulationState.simulationFrame)
            serverSimulationState = simulationState;
    }

    #region Client-Server Reconciliation

    public void Reconciliate()
    {
        // Don't reconciliate on old states.
        if (serverSimulationState.simulationFrame <= lastCorrectedFrame) return;

        // Determine the cache index 
        int msgCacheSlot = serverSimulationState.simulationFrame % CACHE_SIZE;

        // Obtain the cached input and simulation states.
        ClientInputState cachedInputState = inputStateCache[msgCacheSlot];
        SimulationState cachedSimulationState = simStateCache[msgCacheSlot];

        // If there's missing cache data for either input or simulation 
        // snap the player's position to match the server.
        if (cachedInputState == null || cachedSimulationState == null)
        {
            CorrectPlayerSimulationState(serverSimulationState);

            // Set the last corrected frame to equal the server's frame.
            lastCorrectedFrame = serverSimulationState.simulationFrame;
            return;
        }

        // If the simulation time isnt equal to the serve time then return
        // this should never happen
        if (cachedInputState.SimulationFrame != serverSimulationState.simulationFrame || cachedSimulationState.simulationFrame != serverSimulationState.simulationFrame)
        {
            Debug.Log("The impossible has happened");
            return;
        }

        //  The amount of distance in units that we will allow the client's prediction to drift from it's position on the server, before a correction is necessary. 
        float tolerance = 0.0000001f;

        #region Check Position

        // Find the difference between the vector's values. 
        Vector3 positionOffset = cachedSimulationState.position - serverSimulationState.position;

        // A correction is necessary.
        if (positionOffset.sqrMagnitude > tolerance)
        {
            // Show warning about misprediction
            Debug.LogWarning("Client misprediction of position with a difference of " + positionOffset + " at frame " + serverSimulationState.simulationFrame + ".");

            // Set the player's atributes(pos, rot, vel) to match the server's state. 
            CorrectPlayerSimulationState(serverSimulationState);

            // Declare the rewindFrame as we're about to resimulate our cached inputs. 
            int rewindFrame = serverSimulationState.simulationFrame;

            // Loop through and apply cached inputs until we're caught up to our current simulation frame. 
            while (rewindFrame < simulationFrame)
            {
                // Determine the cache index 
                int rewindCacheIndex = rewindFrame % CACHE_SIZE;

                // Obtain the cached input and simulation states.
                ClientInputState rewindCachedInputState = inputStateCache[rewindCacheIndex];
                SimulationState rewindCachedSimulationState = simStateCache[rewindCacheIndex];

                // If there's no state to simulate, for whatever reason, increment the rewindFrame and continue.
                if (rewindCachedInputState == null || rewindCachedSimulationState == null)
                {
                    ++rewindFrame;
                    continue;
                }

                // Process the cached inputs. 
                playerController.FixedUpdateController(rewindCachedInputState);

                // Replace the simulationStateCache index with the new value.
                SimulationState rewoundSimulationState = new SimulationState(rb.position, rb.rotation, rb.velocity, rewindFrame);
                simStateCache[rewindCacheIndex] = rewoundSimulationState;

                // Increase the amount of frames that we've rewound.
                ++rewindFrame;
            }
        }
        #endregion

        #region Check Rotation

        // Find the difference between the quaternion's values. 
        float roationOffset = 1 - Quaternion.Dot(cachedSimulationState.rotation, serverSimulationState.rotation);

        // A correction is necessary.
        if (roationOffset > tolerance)
        {
            // Show warning about misprediction
            Debug.LogWarning("Client misprediction of rotation with a difference of " + positionOffset + " at frame " + serverSimulationState.simulationFrame + ".");

            // Set the player's atributes(pos, rot, vel) to match the server's state. 
            CorrectPlayerSimulationState(serverSimulationState);

            // Declare the rewindFrame as we're about to resimulate our cached inputs. 
            int rewindFrame = serverSimulationState.simulationFrame;

            // Loop through and apply cached inputs until we're caught up to our current simulation frame. 
            while (rewindFrame < simulationFrame)
            {
                // Determine the cache index 
                int rewindCacheIndex = rewindFrame % CACHE_SIZE;

                // Obtain the cached input and simulation states.
                ClientInputState rewindCachedInputState = inputStateCache[rewindCacheIndex];
                SimulationState rewindCachedSimulationState = simStateCache[rewindCacheIndex];

                // If there's no state to simulate, for whatever reason, increment the rewindFrame and continue.
                if (rewindCachedInputState == null || rewindCachedSimulationState == null)
                {
                    ++rewindFrame;
                    continue;
                }

                // Process the cached inputs. 
                playerController.FixedUpdateController(rewindCachedInputState);

                // Replace the simulationStateCache index with the new value.
                SimulationState rewoundSimulationState = new SimulationState(rb.position, rb.rotation, rb.velocity, rewindFrame);
                simStateCache[rewindCacheIndex] = rewoundSimulationState;

                // Increase the amount of frames that we've rewound.
                ++rewindFrame;
            }
        }

        #endregion

        // Once we're complete, update the lastCorrectedFrame to match.
        // NOTE: Set this even if there's no correction to be made. 
        lastCorrectedFrame = serverSimulationState.simulationFrame;
    }

    private void CorrectPlayerSimulationState(SimulationState state)
    {
        rb.position = state.position;
        rb.rotation = state.rotation;
        rb.velocity = state.velocity;
    }
    #endregion
}

public class ClientInputState
{
    public float Tick;
    public int SimulationFrame;
    public float HorizontalAxis, VerticalAxis;
    public bool Jump, Dive;
    public Quaternion LookingRotation;

    public ClientInputState(float tick, int simulationFrame, Inputs inputs, Quaternion rotation)
    {
        this.Tick = tick;
        this.SimulationFrame = simulationFrame;
        this.HorizontalAxis = inputs.HorizontalAxis;
        this.VerticalAxis = inputs.VerticalAxis;
        this.Jump = inputs.Jump;
        this.Dive = inputs.Dive;
        this.LookingRotation = rotation;
    }

    public ClientInputState()
    {
    }
}

public class SimulationState
{
    public int simulationFrame;
    public Vector3 position, velocity;
    public Quaternion rotation;

    public SimulationState(Vector3 position, Quaternion rotation, Vector3 velocity, int simulationFrame)
    {
        this.position = position;
        this.velocity = velocity;
        this.simulationFrame = simulationFrame;
    }

    public SimulationState()
    {
    }
}
