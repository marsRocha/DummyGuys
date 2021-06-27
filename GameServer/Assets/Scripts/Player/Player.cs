using System;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public Guid id;

    private PlayerController playerController;
    private RagdollController ragdollController;
    private Rigidbody rb;
    private LogicTimer logicTimer;
    private PhysicsScene physicsScene;

    private ClientInputState currentInputState;

    private int lastFrame;
    private Queue<ClientInputState> clientInputStates;
    //  The amount of distance in units that we will allow the client's prediction to drift from it's position on the server, before a correction is necessary. 
    private float tolerance = 0.0000001f;
    public int tick = 0;

    private Vector3 velocity;

    [Header("Ragdoll Params")]
    public bool isRagdoled = false;
    [SerializeField]
    private bool isRunning;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        ragdollController = GetComponent<RagdollController>();

        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        //rb.isKinematic = true;

        lastFrame = 0;
        clientInputStates = new Queue<ClientInputState>();
        velocity = Vector3.zero;
    }

    private void Start()
    {
        physicsScene = gameObject.scene.GetPhysicsScene();

        logicTimer = new LogicTimer(() => FixedTime());
        logicTimer.Start();

        playerController.StartController(logicTimer);
        ragdollController.StartController();
    }

    private void Update()
    {
        ragdollController.UpdateController();
        //ragdollController.FixedUpdateController();

        logicTimer.Update();
    }

    public void Initialize(Guid _id)
    {
        id = _id;
    }

    public void StartPlayer()
    {
        isRunning = true;
    }

    public void FixedTime()
    {
        if (isRunning)
        {
            currentInputState = null;

            // Obtain CharacterInputState's from the queue. 
            while (clientInputStates.Count > 0 && (currentInputState = clientInputStates.Dequeue()) != null)
            {
                // Player is sending simulation frames that are in the past, dont process them
                if (currentInputState.SimulationFrame <= lastFrame)
                    continue;

                lastFrame = currentInputState.SimulationFrame;

                // Process the input.
                ProcessInput(currentInputState);

                // Obtain the current SimulationState.
                SimulationState state = new SimulationState(transform.position, transform.rotation, velocity, currentInputState.SimulationFrame, isRagdoled);

                // Send the state back to the client.
                CheckSimulationState(state);
            }
        }
    }

    private void ProcessInput(ClientInputState inputs)
    {
        if (isRagdoled)
            rb.freezeRotation = false;
        //rb.isKinematic = false;
        rb.velocity = velocity;

        playerController.UpdateController(inputs);

        //Simulate physics
        SimulatePhysics();
    }

    //Due to physicsSimULATE simulating all objects inside the scene, we store the velocity of this rb and use it from there, because other players maybe change it
    private void SimulatePhysics()
    {
        physicsScene.Simulate(logicTimer.FixedDeltaTime);

        velocity = rb.velocity;
        //rb.isKinematic = true;
        if (!isRagdoled)
            rb.freezeRotation = true;
    }


    #region Client-Server Reconciliation
    public void CheckSimulationState(SimulationState serverSimulationState)
    {
        // If there's missing cache data for either input or simulation 
        // snap the player's position to match the server.
        if (currentInputState.position == null || currentInputState.rotation == null)
        {
            CorrectPlayerSimulationState(serverSimulationState);
            return;
        }

        //CHECK POSITION
        // Find the difference between the vector's values. 
        Vector3 positionOffset = currentInputState.position - serverSimulationState.position;

        // A correction is necessary.
        if (positionOffset.sqrMagnitude > tolerance)
        {
            Debug.LogWarning("Client misprediction of position with a difference of " + positionOffset + " at frame " + serverSimulationState.simulationFrame + ".");
            // Set the player's atributes(pos, rot, vel) to match the server's state. 
            CorrectPlayerSimulationState(serverSimulationState);
        }
    }

    public void CorrectPlayerSimulationState(SimulationState state)
    {
        RoomSend.CorrectPlayer(Server.Clients[id].RoomID, id, state);
    }
    #endregion

    public void ReceivedClientState(ClientInputState _inputState)
    {
        clientInputStates.Enqueue(_inputState);
    }

    public void Respawn(Vector3 _position, Quaternion _rotation)
    {
        transform.position = _position;
        transform.rotation = _rotation;
        velocity = Vector3.zero;
    }

    public void Destroy()
    {
        logicTimer.Stop();
        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (ragdollController.enabled)
        {
            if (ragdollController.state == RagdollState.Animated)
            {
                Vector3 collisionDirection = collision.contacts[0].normal;

                if (collision.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
                {
                    isRagdoled = true;
                    playerController.EnterRagdoll(collision.contacts[0].point);
                    ragdollController.RagdollIn();

                    rb.isKinematic = false;
                    rb.freezeRotation = false;

                    // Add obstacle extra force
                    rb.AddForceAtPosition(collisionDirection * ragdollController.obstacleModifier, collision.contacts[0].point, ForceMode.Impulse);
                    velocity = rb.velocity;

                    return;
                }
                else if (collision.gameObject.layer == LayerMask.NameToLayer("Bounce"))
                {
                    // Add obstacle extra force
                    rb.AddForceAtPosition(collisionDirection * ragdollController.bounceModifier, collision.contacts[0].point, ForceMode.Impulse);
                    velocity = rb.velocity;

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
                if (collision.impulse.magnitude >= ragdollController.minForce || (playerController.jumping || playerController.diving))
                {
                    playerController.EnterRagdoll(collision.contacts[0].point);
                    // Add extra force
                    rb.AddForceAtPosition(collisionDirection * ragdollController.multiplier, collision.contacts[0].point, ForceMode.Impulse);
                }
            }
        }
    }

}
