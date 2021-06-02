using System;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public Guid id;

    //Player components
    public Rigidbody rb;
    
    //Prediction
    private float lastFrame;
    private Queue<ClientInputState> clientInputs = new Queue<ClientInputState>();

    private void Awake()
    {
        rb.freezeRotation = true;
        rb.isKinematic = true;
        lastFrame = 0;
    }

    public void Initialize(Guid _id)
    {
        this.id = _id;
    }

    public void FixedTime()
    {
        ProcessInputs();
    }

    public void ProcessInputs()
    {
        // Declare the ClientInputState that we're going to be using.
        ClientInputState inputState = null;

        // Obtain CharacterInputState's from the queue. 
        while (clientInputs.Count > 0 && (inputState = clientInputs.Dequeue()) != null)
        {
            // If frames are in the past ignore them
            if (inputState.simulationFrame <= lastFrame)
                continue;

            lastFrame = inputState.simulationFrame;

            // Process the input.
            ProcessInput(inputState);

            // Obtain the current SimulationState of the player's object.
            //PlayerSimulationState state = PlayerSimulationState.CurrentSimulationState(inputState, this);
        }
    }

    private void ProcessInput(ClientInputState inputs)
    {
        /*RotationCheck(inputs);

        if ((inputs.buttons & Button.Fire1) == Button.Fire1)
        {
            LagCompensation.Backtrack(id, inputs.tick, inputs.lerpAmount);
        }

        rb.isKinematic = false;
        rb.velocity = velocity;

        CalculateVelocity(inputs);
        Physics.Simulate(logicTimer.FixedDelta);

        velocity = rb.velocity;
        rb.isKinematic = true;*/

        Debug.Log("moving");
    }

    public void AddInput(ClientInputState _inputState)
    {
        Debug.Log("oiO");
        //clientInputs.Enqueue(_inputState);
        transform.position = _inputState.position;
        Debug.Log(transform.position);
    }

    public void CorrectState(ClientInputState _inputState)
    {
        //Check if state is correct


        //If not, send correction
        //Server.Rooms[_roomId].MulticastUDPData(_packet);
    }
}
