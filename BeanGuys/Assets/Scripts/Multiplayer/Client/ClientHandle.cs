using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Contains all methods to handle incoming messages from the server.
/// </summary>
public class ClientHandle : MonoBehaviour
{
    public delegate void PacketHandler(Guid id, Packet packet);
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
            //{ (int) ServerPackets.pong, Pong },
            //CLIENT SENT
            { (int) ClientPackets.playerMovement, PlayerMovement },
        };
    }

    /// <summary>Handles 'accept' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void Accepted(Guid not_needed, Packet _packet)
    {
        Debug.Log($"Reached server, sending introduciton data.");
        // Send client information
        ClientSend.Introduction();

        GetAnalytics(_packet);
    }

    /// <summary>Handles 'refuse' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void Refused(Guid _roomId, Packet _packet)
    {
        GameManager.instance.Refused();

        GetAnalytics(_packet);
    }

    /// <summary>Handles 'disconnected' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void Disconnected(Guid _roomId, Packet _packet)
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
    public static void JoinedRoom(Guid _roomId, Packet _packet)
    {
        string ip =_packet.ReadString();
        int port =_packet.ReadInt();
        int spawnId =_packet.ReadInt();
        int _roomTickrate = _packet.ReadInt();
        bool _roomPlayerInteraction = _packet.ReadBool();

        ClientInfo.instance.RoomId = _roomId;
        RoomSettings.TICKRATE = _roomTickrate;
        RoomSettings.PLAYER_INTERACTION = _roomPlayerInteraction;
        RoomSettings.DEBUG = true;
        RoomSettings.INTERPOLATION = 100;

        // Start listening to room
        Client.ConnectToRoom(ip, port);
        // Add themselves to the playerCount
        Client.instance.isConnected = true;

        GameManager.instance.JoinedRoom();

        // Add my spawnId
        ClientInfo.instance.SpawnId = spawnId;

        Debug.Log($"Joined room {_roomId}, multicast info Ip:{ip} Port:{port}");

        GetAnalytics(_packet);
    }

    /// <summary>Handles 'playerJoined' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void PlayerJoined(Guid _roomId, Packet _packet)
    {
        //TODO: MESSAGE CAN BE SENT BY THE SERVER(TCP) OR THE ROOM(UDP MULTICAST) FOR NOW BECAUSE TCP DDOES NOT READ ROOMID 
        if (_roomId == null || _roomId == Guid.Empty)
            _roomId = _packet.ReadGuid();

        if (ClientInfo.instance.RoomId == _roomId)
        {
            Guid id = _packet.ReadGuid();
            //If not me
            if (ClientInfo.instance.Id != id)
            {
                string username = _packet.ReadString();
                int color = _packet.ReadInt();
                int spawnId = _packet.ReadInt();

                Client.peers.Add(id, new Peer(id, username, color, spawnId));
                GameManager.instance.UpdatePlayerCount();
                Debug.Log($"{username} has joined the game!");
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
    public static void PlayerLeft(Guid _roomId, Packet _packet)
    {
        Guid _clientId = _packet.ReadGuid();

        Debug.Log($"Player {_clientId} has disconnected.");

        Client.peers.Remove(_clientId);
        GameManager.instance.RemovePlayer(_clientId);

        GetAnalytics(_packet);
    }

    /// <summary>Handles 'map' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void Map(Guid _roomId, Packet _packet)
    {
        int _mapIndex = _packet.ReadInt();

        // LoadScene
        GameManager.instance.LoadGameScene(_mapIndex);

        GetAnalytics(_packet);
    }

    /// <summary>Handles 'startGame' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void StartGame(Guid _roomId, Packet _packet)
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
    public static void EndGame(Guid _roomId, Packet _packet)
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
    public static void PlayerMovement(Guid _clientId, Packet _packet)
    {
        int _tick = _packet.ReadInt(); // int
        Vector3 _position = _packet.ReadVector3(); // Vector3
        Quaternion _rotation = _packet.ReadQuaternion(); // Quaternion
        bool _ragdoll = _packet.ReadBool(); // Bool
        int _animation = _packet.ReadInt(); // int

        GameManager.instance.PlayerMovement(_clientId, _tick, _position, _rotation, _ragdoll, _animation);

        GetAnalytics(_packet);
    }

    /// <summary>Handles 'playerCorrection' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void PlayerCorrection(Guid _roomId, Packet _packet)
    {
        Guid _clientId = _packet.ReadGuid();

        if (_clientId != ClientInfo.instance.Id)
            return;

        int simulationFrame = _packet.ReadInt();
        Vector3 position =_packet.ReadVector3();
        Quaternion rotation =_packet.ReadQuaternion();
        bool ragdoll =_packet.ReadBool();
        int animation =_packet.ReadInt();

        GameManager.instance.PlayerCorrection(new PlayerState(simulationFrame, position, rotation, ragdoll, animation));

        GetAnalytics(_packet);
    }

    /// <summary>Handles 'playerRespawn' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void PlayerRespawn(Guid _roomId, Packet _packet)
    {
        Guid _clientId = _packet.ReadGuid();

        if (_clientId != ClientInfo.instance.Id)
            return;

        int checkPointNum =_packet.ReadInt();

        GameManager.instance.PlayerRespawn(checkPointNum);

        GetAnalytics(_packet);
    }

    /// <summary>Handles 'playeerGrab' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void PlayerGrab(Guid _roomId, Packet _packet)
    {
        Guid _grabber = _packet.ReadGuid();
        Guid _grabbed = _packet.ReadGuid();

        GameManager.instance.PlayerGrab(_grabber, _grabbed);

        GetAnalytics(_packet);
    }

    /// <summary>Handles 'playeerLetGo' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void PlayerLetGo(Guid _roomId, Packet _packet)
    {
        Guid _grabber = _packet.ReadGuid();
        Guid _grabbed = _packet.ReadGuid();

        GameManager.instance.PlayerLetGo(_grabber, _grabbed);

        GetAnalytics(_packet);
    }

    /// <summary>Handles 'playeerPush' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void PlayerPush(Guid _roomId, Packet _packet)
    {
        Guid _pusher = _packet.ReadGuid();
        Guid _pushed = _packet.ReadGuid();

        GameManager.instance.PlayerPush(_pusher, _pushed);

        GetAnalytics(_packet);
    }

    /// <summary>Handles 'playerFinish' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void PlayerFinish(Guid _roomId, Packet _packet)
    {
        Guid clientId = _packet.ReadGuid();

        GameManager.instance.PlayerFinish(clientId);

        GetAnalytics(_packet);
    }

    /// <summary>Handles 'serverTick' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void ServerTick(Guid _roomId, Packet _packet)
    {
        int _roomTick = _packet.ReadInt();
        float _roomClock = _packet.ReadFloat();

        GameManager.instance.ServerTick(_roomTick, _roomClock);

        GetAnalytics(_packet);
    }

    /// <summary>Handles 'pong' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void Pong(Guid not_needed, Packet _packet)
    {
        // We are receive the ping packet, update the stored ping variable
        Client.instance.ping = Math.Round((DateTime.UtcNow - Client.instance.pingSent).TotalMilliseconds, 0);

        GetAnalytics(_packet);
    }

    public static void GetAnalytics(Packet _packet)
    {
        Analytics.bandwidthDown += _packet.GetByteLength();
        Analytics.packetsDown++;
    }
}
