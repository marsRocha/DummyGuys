﻿using System;
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
    private SimulationState lastState;

    private int lastFrame;
    private Queue<ClientInputState> clientInputStates;
    //  The amount of distance in units that we will allow the client's prediction to drift from it's position on the server, before a correction is necessary. 
    private float tolerance = 0.0000001f;
    public int tick = 0;
    public int simulationFrame;

    private Vector3 velocity, angularVelocity;
    private Vector3 impact = Vector3.zero, point = Vector3.zero;

    [Header("Ragdoll Params")]
    public bool Ragdolled = false;
    [SerializeField]
    private bool isRunning;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        ragdollController = GetComponent<RagdollController>();

        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.isKinematic = true;

        lastFrame = 0;
        clientInputStates = new Queue<ClientInputState>();
        velocity = Vector3.zero;
        angularVelocity = Vector3.zero;

        simulationFrame = 0;
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
            bool simulated = false;
            currentInputState = null;

            // Obtain CharacterInputState's from the queue. 
            while (clientInputStates.Count > 0 && (currentInputState = clientInputStates.Dequeue()) != null)
            {
                // Player is sending simulation frames that are in the past, dont process them
                if (currentInputState.SimulationFrame <= lastFrame)
                    continue;

               /* // Re-run frame now that message has reached the server
                // This is in case the server simulated a frame without a message due to delay in receiving them
                if(simulationFrame >= currentInputState.SimulationFrame && lastState != null)
                {
                    SetSimulationState(lastState);
                }*/

                // Process the input.
                ProcessInput(currentInputState);

                // Update last computed frame based on inputs from client
                lastFrame = currentInputState.SimulationFrame;

                // Obtain the current SimulationState.
                SimulationState state = new SimulationState(transform.position, transform.rotation, velocity, currentInputState.SimulationFrame, Ragdolled);
                // Save latest frame
                lastState = state;

                // Send the state back to the client.
                CheckSimulationState(state);

                simulationFrame++;
                //simulated = true;
            }

           /* if(simulated) return;

            currentInputState = new ClientInputState();
            currentInputState.HorizontalAxis = 0;
            currentInputState.VerticalAxis = 0;
            currentInputState.Jump = false;
            currentInputState.Dive = false;

            // Process the input.
            ProcessInput(currentInputState);

            simulationFrame++;*/
        }
    }

    private void ProcessInput(ClientInputState inputs)
    {
        if (Ragdolled)
            rb.freezeRotation = false;
        rb.isKinematic = false;
        rb.velocity = velocity;
        rb.angularVelocity = angularVelocity;

        if (impact != Vector3.zero && point != Vector3.zero)
        {
            rb.freezeRotation = false;
            Ragdolled = true;
            playerController.EnterRagdoll(point);
            //ragdollController.RagdollIn();

            rb.AddForceAtPosition(impact, point, ForceMode.Impulse);
            impact = Vector3.zero; point = Vector3.zero;
        }

        playerController.UpdateController(inputs);

        //Simulate physics
        SimulatePhysics();
    }

    //Due to physicsSimULATE simulating all objects inside the scene, we store the velocity of this rb and use it from there, because other players maybe change it
    private void SimulatePhysics()
    {
        physicsScene.Simulate(logicTimer.FixedDeltaTime);

        velocity = rb.velocity;
        angularVelocity = rb.angularVelocity;
        rb.isKinematic = true;
        if (!Ragdolled)
            rb.freezeRotation = true;
    }


    #region Client-Server Reconciliation
    public void CheckSimulationState(SimulationState serverSimulationState)
    {
        // If there's missing cache data for either input or simulation 
        // snap the player's position to match the server.
        if (currentInputState.position == null || currentInputState.rotation == null)
        {
            SendCorrection(serverSimulationState);
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
            SendCorrection(serverSimulationState);
        }
    }

    public void SendCorrection(SimulationState state)
    {
        RoomSend.CorrectPlayer(Server.Clients[id].RoomID, id, state);
    }

    public void SetSimulationState(SimulationState simulationState)
    {
        transform.position = simulationState.position;
        transform.rotation = simulationState.rotation;
        velocity = simulationState.velocity;
    }

    #endregion

    public void ReceivedClientState(ClientInputState _inputState)
    {
        clientInputStates.Enqueue(_inputState);
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
            if (ragdollController.State == RagdollState.Animated)
            {
                Vector3 collisionDirection = collision.contacts[0].normal;

                if (collision.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
                {
                    impact = collisionDirection * ragdollController.ObstacleModifier;
                    point = collision.contacts[0].point;

                    return;
                }
                else if (collision.gameObject.layer == LayerMask.NameToLayer("Bounce"))
                {
                    // Add obstacle extra force
                    rb.AddForceAtPosition(collisionDirection * ragdollController.BounceModifier, collision.contacts[0].point, ForceMode.Impulse);
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
                if (collision.impulse.magnitude >= ragdollController.MinForce || (playerController.jumping || playerController.diving))
                {
                    playerController.EnterRagdoll(collision.contacts[0].point);
                    // Add extra force
                    rb.AddForceAtPosition(collisionDirection * ragdollController.Multiplier, collision.contacts[0].point, ForceMode.Impulse);
                }
            }
        }
    }

}
