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

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        //rb.freezeRotation = true;
        //rb.isKinematic = true;

        playerController = GetComponent<PlayerController>();
        playerController.StartController();

        lastFrame = 0;
        clientInputStates = new Queue<ClientState>();

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
        // Obtain CharacterInputState's from the queue. 
        while (clientInputStates.Count > 0 && (currentInputState = clientInputStates.Dequeue()) != null)
        {
            // If frames are in the past ignore them
            if (currentInputState.SimulationFrame <= lastFrame)
                continue;

            lastFrame = currentInputState.SimulationFrame;

            // Process the input.
            playerController.FixedUpdateController(currentInputState);

            // Obtain the current SimulationState of the player's object.
            SimulationState state = new SimulationState(rb.position, rb.rotation, rb.velocity, currentInputState.SimulationFrame);

            //Check if received state is correct
            //CheckSimulationState(state);
        }
    }

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
        float roationOffset = 1 - Quaternion.Dot(currentInputState.rotation, serverSimulationState.rotation);

        // A correction is necessary.
        if (roationOffset > tolerance)
        {
            Debug.LogWarning("Client misprediction of rotation with a difference of " + positionOffset + " at frame " + serverSimulationState.simulationFrame + ".");
            // Set the player's atributes(pos, rot, vel) to match the server's state. 
            CorrectPlayerSimulationState(serverSimulationState);
        }
    }

    public void CorrectPlayerSimulationState(SimulationState state)
    {
        //Server.Rooms[Server.Clients[id].RoomID].CorrectPlayer(id, state);
    }
    public void ReceivedClientState(ClientState _inputState)
    {
        clientInputStates.Enqueue(_inputState);

        //transform.position = _inputState.position;
        //Debug.Log(transform.position);
    }

    public void Respawn(Vector3 _position, Quaternion _rotation)
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
