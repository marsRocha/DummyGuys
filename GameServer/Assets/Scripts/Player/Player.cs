using System;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public int Id { get; private set; }
    private int roomId;

    private Rigidbody rb;
    private LogicTimer logicTimer;
    private PlayerController playerController;

    private PlayerState currentPlayerState;

    private int lastFrame;
    private Queue<PlayerState> playerStates;
    private PlayerState lastState;
    public int tick;

    private const float AFK_TIMEOUT = 10f;
    private float _afkTimer;
    private bool _inputThisFrame;
    //-----
    private const float HEARTBEAT_TIMEOUT = 10f;
    private float _heartbeat;

    public bool Ragdolled = false;
    [SerializeField]
    private bool Running;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.isKinematic = true;
        playerController = GetComponent<PlayerController>();

        _afkTimer = 0;
        lastFrame = 0;
        tick = 0;
        playerStates = new Queue<PlayerState>();

        lastState = new PlayerState(0, transform.position, transform.rotation, Ragdolled, 0);
    }

    private void Start()
    {
        playerController.Initialize(logicTimer);

        logicTimer = new LogicTimer(() => FixedTime());
        logicTimer.Start();
    }

    private void Update()
    {
        logicTimer.Update();

        Pong_Check();

        if(Running)
            AFK_Check();
    }

    public void Initialize(int _id, int _roomId)
    {
        Id = _id;
        roomId = _roomId;
    }

    public void StartPlayer(bool _activate)
    {
        Running = _activate;
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

                // Check if the state received is inside certain params
                CheckSimulationState();
            }
        }
    }

    #region Client-Server State Validation
    public void CheckSimulationState()
    {
        // If there's missing cache data for either input or simulation 
        // snap the player's position to match the server.
        if (currentPlayerState.position == null || currentPlayerState.rotation == null)
        {
            lastState.tick = currentPlayerState.tick;
            SendCorrection(lastState);
            return;
        }

        // If players game tick is ahead
        // snap the player's position to match the server.
        if (currentPlayerState.tick > tick)
        {
            lastState.tick = currentPlayerState.tick;
            SendCorrection(lastState);
            return;
        }

        // Validate new position/rotation
        // Else snap the player's position to match the server.
        if (!ProcessState())
        {
            /*lastState.tick = currentPlayerState.tick;
            SendCorrection(lastState);
            return;*/
        }

        // If everything is inside the norms than update server's simulationState
        lastState = currentPlayerState;
        SetSimulationState(lastState);
    }
    private bool ProcessState()
    {
        return playerController.ProcessState(currentPlayerState, lastState);
    }

    public void SendCorrection(PlayerState _state)
    {
        RoomSend.CorrectPlayer(Server.Rooms[roomId].Clients[Id].RoomID, Id, _state);
    }

    public void SetSimulationState(PlayerState _playerState)
    {
        transform.position = _playerState.position;
        transform.rotation = _playerState.rotation;
        Ragdolled = _playerState.ragdoll;
    }
    #endregion

    public void ReceivedClientState(PlayerState _playerState)
    {
        playerStates.Enqueue(_playerState);
    }

    public void ReceivedRespawn(Vector3 _position, Quaternion _rotation)
    {
        rb.isKinematic = true;
        transform.position = _position;
        transform.rotation = _rotation;
        Ragdolled = false;
    }

    #region Actions
    public int TryGrab()
    {
        return playerController.TryGrab();
    }

    public void Grab()
    {
        playerController.Grab();
    }

    public bool GetGrab()
    {
        return playerController.GetGrab();
    }

    public void LetGo()
    {
        playerController.LetGo();
    }

    public int TryPush()
    {
        return playerController.TryPush();
    }
    #endregion

    public void Ping()
    {
        _heartbeat = 0.0f;
    }

    public void Pong_Check()
    {
        _heartbeat += Time.deltaTime;
        if (_heartbeat > HEARTBEAT_TIMEOUT)
        {
            Console.WriteLine("HEARTBEAT_TIMEOUT from player[" + Id + "]");
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
            _afkTimer += Time.deltaTime;
            if (_afkTimer > AFK_TIMEOUT)
            {
                Console.WriteLine("AFK_TIMEOUT from player[" + Id + "] on Room[{roomId}]");
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