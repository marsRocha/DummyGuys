using System;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public Guid id;

    //Player components
    private Rigidbody rb;
    private PlayerController playerController;

    ClientState currentInputState;

    //Prediction & Reconciliation
    private float lastFrame;
    private Queue<ClientState> clientInputStates;
    //  The amount of distance in units that we will allow the client's prediction to drift from it's position on the server, before a correction is necessary. 
    private float tolerance = 0.0000001f;


    [Header("Movement")]
    public bool grounded;
    public Transform Pelvis;
    public LayerMask collisionMask;
    public Vector3 move, groundNormal;
    private PhysicsScene physicsScene;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        //rb.freezeRotation = true;
        //rb.isKinematic = true;

        playerController = GetComponent<PlayerController>();
        //playerController.StartController();

        lastFrame = 0;
        clientInputStates = new Queue<ClientState>();

        physicsScene = gameObject.scene.GetPhysicsScene();
    }

    public void Initialize(Guid _id)
    {
        this.id = _id;
    }

    public void FixedUpdate()
    {
        ProcessInputs();
    }

    public void ProcessInputs()
    {
        currentInputState = null;

        // Obtain CharacterInputState's from the queue. 
        while (clientInputStates.Count > 0 && (currentInputState = clientInputStates.Dequeue()) != null)
        {
            // If frames are in the past ignore them
            if (currentInputState.SimulationFrame <= lastFrame)
                continue;

            lastFrame = currentInputState.SimulationFrame;

            // Process the input.
            //playerController.FixedUpdateController(currentInputState);
            ProcessInput(currentInputState);


            // Obtain the current SimulationState of the player's object.
            SimulationState state = new SimulationState(transform.position, transform.rotation, rb.velocity, currentInputState.SimulationFrame);

            //Check if received state is correct
            CheckSimulationState(state);
        }
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

        //CHECK ROTATION
        // Find the difference between the quaternion's values. 
        /*float roationOffset = 1 - Quaternion.Dot(currentInputState.rotation, serverSimulationState.rotation);

        // A correction is necessary.
        if (roationOffset > tolerance)
        {
            Debug.LogWarning("Client misprediction of rotation with a difference of " + positionOffset + " at frame " + serverSimulationState.simulationFrame + ".");
            // Set the player's atributes(pos, rot, vel) to match the server's state. 
            CorrectPlayerSimulationState(serverSimulationState);
        }*/
    }

    public void CorrectPlayerSimulationState(SimulationState state)
    {
        Server.Rooms[Server.Clients[id].RoomID].CorrectPlayer(id, state);
    }
    #endregion

    public void ReceivedClientState(ClientState _inputState)
    {
        clientInputStates.Enqueue(_inputState);
    }

    public void Respawn(Vector3 _position, Quaternion _rotation)
    {

    }

    #region Movement
    private void ProcessInput(ClientState currentInputs)
    {
        GroundCheck();
        Movement(currentInputs);
        CounterMovement();
    }

    void GroundCheck()
    {
        RaycastHit hit;
        physicsScene.Raycast(Pelvis.position, Vector3.down, out hit, -0.735f + 1.52f, collisionMask);
        if (hit.collider)
        {
            grounded = true;
            groundNormal = hit.normal;
        }
        else
        {
            grounded = false;
            groundNormal = Vector3.zero;
        }
    }
    private void Movement(ClientState currentInputs)
    {
        if (grounded)
        {
            move = new Vector3(currentInputs.HorizontalAxis, 0f, currentInputs.VerticalAxis);

            if (move.sqrMagnitude > 0.1f)
            {
                //Camera direction
                move = ToCameraSpace(currentInputs, move);

                //For better movement on slopes/ramps
                move = Vector3.ProjectOnPlane(move, groundNormal);

                move.Normalize();
                move *= 300 * Time.fixedDeltaTime;
                Debug.DrawLine(this.transform.position, this.transform.position + move, Color.yellow, 1f);

                rb.AddForce(move, ForceMode.VelocityChange);
            }

            //Player rotation
            if (move.x != 0f || move.z != 0f)
            {
                //Character rotation
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(transform.forward + new Vector3(move.x, 0f, move.z)), 10 * Time.fixedDeltaTime);
            }
        }
    }
    private Vector3 ToCameraSpace(ClientState currentInputs, Vector3 moveVector)
    {
        Vector3 camFoward = (currentInputs.LookingRotation * Vector3.forward);
        Vector3 camRight = (currentInputs.LookingRotation * Vector3.right);

        camFoward.y = 0;
        camRight.y = 0;

        camFoward.Normalize();
        camRight.Normalize();

        Vector3 moveDirection = (camFoward * moveVector.z + camRight * moveVector.x);

        return moveDirection;
    }
    private void CounterMovement()
    {
        rb.AddForce(Vector3.right * -rb.velocity.x, ForceMode.VelocityChange);
        rb.AddForce(Vector3.forward * -rb.velocity.z, ForceMode.VelocityChange);
    }
    #endregion
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
