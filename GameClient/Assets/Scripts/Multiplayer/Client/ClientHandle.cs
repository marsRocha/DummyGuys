using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Contains all methods to handle incoming messages from the server.
/// </summary>
public class ClientHandle : MonoBehaviour
{
    public delegate void PacketHandler(int id, Packet packet);
    public static Dictionary<int, PacketHandler> packetHandlers;

    /// <summary>Handles 'welcome' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void InitializeData()
    {
        packetHandlers = new Dictionary<int, PacketHandler>()
        {
            //SERVER SENT
            { (int) ServerPackets.accept, Accepted },
            { (int) ServerPackets.refuse, Refused },
            { (int) ServerPackets.disconnected, Disconnected },
            { (int) ServerPackets.joinedRoom, JoinedRoom },
            { (int) ServerPackets.playerJoined, PlayerJoined },
            { (int) ServerPackets.playerLeft, PlayerLeft },
            { (int) ServerPackets.map, Map },
            { (int) ServerPackets.startGame, StartGame },
            { (int) ServerPackets.endGame, EndGame },
            { (int) ServerPackets.playerRespawn, PlayerRespawn },
            { (int) ServerPackets.playerCorrection, PlayerCorrection },
            { (int) ServerPackets.playerFinish, PlayerFinish },
            { (int) ServerPackets.playerGrab, PlayerGrab },
            { (int) ServerPackets.playerLetGo, PlayerLetGo },
            { (int) ServerPackets.playerPush, PlayerPush },
            { (int) ServerPackets.serverTick, ServerTick },
            { (int) ServerPackets.serverClock, ServerClock },
            //CLIENT SENT
            { (int) ClientPackets.playerMovement, PlayerMovement },
        };
    }

    /// <summary>Handles 'accept' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void Accepted(int not_needed, Packet _packet)
    {
        Debug.Log($"Reached server, sending introduciton data.");
        // Send client information
        ClientSend.Introduction();

        GetAnalytics(_packet);
    }

    /// <summary>Handles 'refuse' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void Refused(int _roomId, Packet _packet)
    {
        GameManager.instance.Refused();

        GetAnalytics(_packet);
    }

    /// <summary>Handles 'disconnected' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void Disconnected(int _roomId, Packet _packet)
    {
        if (ClientInfo.instance.RoomId == _roomId)
        {
            GameManager.instance.Disconnected();
        }
        else
        {
            Debug.LogWarning("Received 'Disconnected' message from wrong room;");
        }

        GetAnalytics(_packet);
    }

    /// <summary>Handles 'joinedRoom' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void JoinedRoom(int _roomId, Packet _packet)
    {
        string _ip =_packet.GetString();
        int _port =_packet.GetInt();
        int _clientRoomId = _packet.GetInt();

        int _roomTickRate = _packet.GetInt();
        bool _roomPlayerInteraction = _packet.GetBool();

        ClientInfo.instance.RoomId = _roomId;
        RoomSettings.TICK_RATE = _roomTickRate;
        RoomSettings.PLAYER_INTERACTION = _roomPlayerInteraction;
        RoomSettings.DEBUG = true;
        RoomSettings.INTERPOLATION = 100;

        Application.targetFrameRate = RoomSettings.TICK_RATE;

        // Start listening to room
        Client.ConnectToRoom(_ip, _port);
        // Add themselves to the playerCount
        Client.instance.isConnected = true;

        GameManager.instance.JoinedRoom();

        // Add my spawnId
        ClientInfo.instance.ClientRoomId = _clientRoomId;

        Debug.Log($"Joined room {_roomId}, multicast info Ip:{_ip} Port:{_port}");

        GetAnalytics(_packet);
    }

    /// <summary>Handles 'playerJoined' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void PlayerJoined(int _roomId, Packet _packet)
    {
        if (ClientInfo.instance.RoomId == _roomId)
        {
            Guid _clientId = _packet.GetGuid();
            //If not me
            if (ClientInfo.instance.Id != _clientId)
            {
                int _clientRoomId = _packet.GetInt();
                string _username = _packet.GetString();
                int _color = _packet.GetInt();

                Client.peers.Add(_clientRoomId, new Peer(_clientId, _clientRoomId, _username, _color));
                GameManager.instance.UpdatePlayerCount();
                Debug.Log($"{_username} has joined the game!");
            }
        }
        else
        {
            Debug.LogWarning("Received 'PlayerJoined' message from wrong room;");
        }

        GetAnalytics(_packet);
    }

    /// <summary>Handles 'playerLeft' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void PlayerLeft(int _roomId, Packet _packet)
    {
        int _clientRoomId = _packet.GetInt();

        Debug.Log($"Player {_clientRoomId} has disconnected.");

        Client.peers.Remove(_clientRoomId);
        GameManager.instance.RemovePlayer(_clientRoomId);

        GetAnalytics(_packet);
    }

    /// <summary>Handles 'map' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void Map(int _roomId, Packet _packet)
    {
        int _mapIndex = _packet.GetInt();

        // LoadScene
        GameManager.instance.LoadGameScene(_mapIndex);

        GetAnalytics(_packet);
    }

    /// <summary>Handles 'startGame' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void StartGame(int _roomId, Packet _packet)
    {
        if (ClientInfo.instance.RoomId == _roomId)
        {
            GameManager.instance.StartGame();
        }
        else
        {
            Debug.LogWarning($"Received 'StartGame' message from wrong room {_roomId};");
        }

        GetAnalytics(_packet);
    }

    /// <summary>Handles 'endGame' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void EndGame(int _roomId, Packet _packet)
    {
        if (ClientInfo.instance.RoomId == _roomId)
        {
            GameManager.instance.EndGame();
        }
        else
        {
            Debug.LogWarning("Received 'EndGame' message from wrong room;");
        }

        GetAnalytics(_packet);
    }

    /// <summary>Handles 'playerMovement' packet sent from other clients.</summary>
    /// <param name="_clientId">Id form the client that sent the message.</param>
    /// <param name="_packet">The received packet.</param>
    public static void PlayerMovement(int _clientRoomId, Packet _packet)
    {
        int _tick = _packet.GetInt(); // int
        Vector3 _position = _packet.GetVector3(); // Vector3
        Quaternion _rotation = _packet.GetQuaternion(); // Quaternion
        bool _ragdoll = _packet.GetBool(); // Bool
        int _animation = _packet.GetInt(); // int

        GameManager.instance.PlayerMovement(_clientRoomId, _tick, _position, _rotation, _ragdoll, _animation);

        GetAnalytics(_packet);
    }

    /// <summary>Handles 'playerCorrection' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void PlayerCorrection(int _roomId, Packet _packet)
    {
        int _clientId = _packet.GetInt();

        if (_clientId != ClientInfo.instance.ClientRoomId)
            return;

        int _tick = _packet.GetInt();
        Vector3 _position =_packet.GetVector3();
        Quaternion _rotation =_packet.GetQuaternion();
        bool _ragdoll =_packet.GetBool();
        int _animation =_packet.GetInt();

        GameManager.instance.PlayerCorrection(new PlayerState(_tick, _position, _rotation, _ragdoll, _animation));

        GetAnalytics(_packet);
    }

    /// <summary>Handles 'playerRespawn' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void PlayerRespawn(int _roomId, Packet _packet)
    {
        int _clientId = _packet.GetInt();

        if (_clientId != ClientInfo.instance.ClientRoomId)
            return;

        int checkPointNum =_packet.GetInt();

        GameManager.instance.PlayerRespawn(checkPointNum);

        GetAnalytics(_packet);
    }

    /// <summary>Handles 'playeerGrab' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void PlayerGrab(int _roomId, Packet _packet)
    {
        int _grabber = _packet.GetInt();
        int _grabbed = _packet.GetInt();

        GameManager.instance.PlayerGrab(_grabber, _grabbed);

        GetAnalytics(_packet);
    }

    /// <summary>Handles 'playeerLetGo' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void PlayerLetGo(int _roomId, Packet _packet)
    {
        int _grabber = _packet.GetInt();
        int _grabbed = _packet.GetInt();

        GameManager.instance.PlayerLetGo(_grabber, _grabbed);

        GetAnalytics(_packet);
    }

    /// <summary>Handles 'playeerPush' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void PlayerPush(int _roomId, Packet _packet)
    {
        int _pusher = _packet.GetInt();
        int _pushed = _packet.GetInt();

        GameManager.instance.PlayerPush(_pusher, _pushed);

        GetAnalytics(_packet);
    }

    /// <summary>Handles 'playerFinish' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void PlayerFinish(int _roomId, Packet _packet)
    {
        int _clientId = _packet.GetInt();

        GameManager.instance.PlayerFinish(_clientId);

        GetAnalytics(_packet);
    }

    /// <summary>Handles 'serverTick' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void ServerTick(int _roomId, Packet _packet)
    {
        int _roomTick = _packet.GetInt();

        GameManager.instance.ServerTick(_roomTick);

        GetAnalytics(_packet);
    }

    /// <summary>Handles 'serverClock' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void ServerClock(int _roomId, Packet _packet)
    {
        float _roomClock = _packet.GetFloat();

        GameManager.instance.ServerClock(_roomClock);

        GetAnalytics(_packet);
    }

    /// <summary>Handles 'pong' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void Pong(int not_needed, Packet _packet)
    {
        //DateTime dt = DateTime.Parse(_packet.GetString());
        //Console.WriteLine($"Ping:{Math.Round((DateTime.UtcNow - dt).TotalMilliseconds, 0)}");

        Client.instance.ping = Math.Round((DateTime.UtcNow - Client.instance.pingSent).TotalMilliseconds, 0);
        if (TestData.PING)
            Console.WriteLine($"Ping:{Client.instance.ping}ms");

        GetAnalytics(_packet);
    }

    public static void GetAnalytics(Packet _packet)
    {
        Analytics.bandwidthDown += _packet.GetByteLength();
        Analytics.packetsDown++;
    }
}
