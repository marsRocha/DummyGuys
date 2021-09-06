using System;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public Guid Id { get; private set; }
    private Guid roomId;

    private Rigidbody rb;
    private LogicTimer logicTimer;

    private PlayerState currentPlayerState;
    private Vector3 velocity, angularVelocity;

    private int lastFrame;
    private Queue<PlayerState> playerStates;
    private SimulationState lastState;
    public int tick;

    private const float AFK_TIMEOUT = 20f;
    private float _afkTimer;
    private bool _inputThisFrame;
    //-----
    private const float HEARTBEAT_TIMEOUT = 7f;
    private float _heartbeat;

    [Header("Ragdoll Params")]
    [SerializeField]
    private LayerMask collision;
    public bool Ragdolled = false;
    [SerializeField]
    private bool Running;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.isKinematic = true;

        _afkTimer = 0;
        lastFrame = 0;
        tick = 0;
        playerStates = new Queue<PlayerState>();
        velocity = Vector3.zero;
        angularVelocity = Vector3.zero;

        lastState = new SimulationState(0, transform.position, transform.rotation, velocity, angularVelocity, Ragdolled);
    }

    private void Start()
    {
        logicTimer = new LogicTimer(() => FixedTime());
        logicTimer.Start();
    }

    private void Update()
    {
        logicTimer.Update();
    }

    public void Initialize(Guid _id, Guid _roomId)
    {
        Id = _id;
        roomId = _roomId;
    }

    public void StartPlayer()
    {
        Running = true;
    }

    public void FixedTime()
    {
        if (Running)
        {
            currentPlayerState = null;

            // Obtain CharacterInputState's from the queue. 
            while (playerStates.Count > 0 && (currentPlayerState = playerStates.Dequeue()) != null)
            {
                _inputThisFrame = true;

                // Player is sending simulation frames that are in the past, dont process them
                if (currentPlayerState.tick <= lastFrame)
                    continue;

                lastFrame = currentPlayerState.tick;

                // Obtain the current SimulationState.
                SimulationState serverSimulationState = new SimulationState(currentPlayerState.tick, transform.position, transform.rotation, velocity, angularVelocity, Ragdolled);

                // Check if the state received is inside certain params
                CheckSimulationState(serverSimulationState);

                // Update latest received state
                lastState = serverSimulationState;
            }

            AFK_Check();

            Pong_Check();
        }
    }

    private bool ProcessState(SimulationState serverSimulationState)
    {
        if(currentPlayerState.position != serverSimulationState.position)
        {
            Vector3 direction = currentPlayerState.position - transform.position;
            // Check if player is inside or went through a wall/floor
            RaycastHit hitTest;
            Physics.Raycast(transform.position, direction, out hitTest, direction.magnitude, collision);
            if (hitTest.collider != null)
                return false;
        }


        return true;
    }

    #region Client-Server State Validation
    public void CheckSimulationState(SimulationState serverSimulationState)
    {
        // If there's missing cache data for either input or simulation 
        // snap the player's position to match the server.
        if (currentPlayerState.position == null || currentPlayerState.rotation == null)
        {
            SendCorrection(lastState);
            return;
        }

        // If players game tick is ahead
        // snap the player's position to match the server.
        if (currentPlayerState.tick > tick)
        {
            SendCorrection(lastState);
            return;
        }

        // Validate new position/rotation
        // Else snap the player's position to match the server.
        if (!ProcessState(serverSimulationState))
        {
            Debug.Log("Not valid.");
            SendCorrection(lastState);
            return;
        }

        // If everything is inside the norms than update server's simulationState
        SetSimulationState(currentPlayerState);
    }

    public void SendCorrection(SimulationState state)
    {
        RoomSend.CorrectPlayer(Server.Rooms[roomId].Clients[Id].RoomID, Id, state);
    }

    public void SetSimulationState(PlayerState simulationState)
    {
        transform.position = simulationState.position;
        transform.rotation = simulationState.rotation;
        Ragdolled = simulationState.ragdoll;
    }
    #endregion

    public void ReceivedClientState(PlayerState _playerState)
    {
        playerStates.Enqueue(_playerState);
    }

    public void Respawn(Vector3 _position, Quaternion _rotation)
    {
        rb.isKinematic = true;
        transform.position = _position;
        transform.rotation = _rotation;
        velocity = Vector3.zero;
        Ragdolled = false;
    }

    public void Pong()
    {
        _heartbeat = 0.0f;
    }

    public void Pong_Check()
    {
        _heartbeat += logicTimer.FixedDeltaTime;
        if (_heartbeat > HEARTBEAT_TIMEOUT)
        {
            Console.WriteLine("HEARTBEAT_TIMEOUT");
            // Disconnect player
            Server.Rooms[roomId].RemovePlayer(Id);
            Deactivate();
        }
    }

    public void AFK_Check()
    {
        if (_inputThisFrame)
        {
            _afkTimer = 0.0f;
        }
        else
        {
            _afkTimer += logicTimer.FixedDeltaTime;
            if (_afkTimer > AFK_TIMEOUT)
            {
                Console.WriteLine("AFK_TIMEOUT");
                // Disconnect player
                Server.Rooms[roomId].RemovePlayer(Id);
                Deactivate();
            }
        }

        // Reset bool
        _inputThisFrame = false;
    }

    public void Reset(Vector3 _position)
    {
        rb.freezeRotation = true;
        rb.isKinematic = true;

        lastFrame = 0;
        tick = 0;
        playerStates.Clear();

        transform.position = _position;
        transform.rotation = Quaternion.identity;
        velocity = Vector3.zero;
        angularVelocity = Vector3.zero;

        Ragdolled = false;
        Running = false;

        logicTimer.Start();
    }

    public void Deactivate()
    {
        logicTimer.Stop();
        gameObject.SetActive(false);
    }
}