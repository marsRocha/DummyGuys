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
            { (int) ServerPackets.playerMovement, PlayerMovement },
            { (int) ServerPackets.playerFinish, PlayerFinish },
            { (int) ServerPackets.playerGrab, PlayerGrab },
            { (int) ServerPackets.playerLetGo, PlayerLetGo },
            { (int) ServerPackets.playerPush, PlayerPush },
            { (int) ServerPackets.serverTick, ServerTick },
            { (int) ServerPackets.pong, Pong },
            //CLIENT SENT
            { (int) ClientPackets.playerMovement, PlayerMovement },
            //{ (int) ClientPackets.playerGrab, PlayerGrab },
            //{ (int) ClientPackets.playerLetGo, PlayerLetGo },
            //{ (int) ClientPackets.playerPush, PlayerPush }
        };
    }

    /// <summary>Handles 'accept' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void Accepted(Guid not_needed, Packet _packet)
    {
        Debug.Log($"Reached server, sending introduciton data.");
        // Send client information
        ClientSend.Introduction();

        Analytics.bandwidthDown += 8;
        Analytics.packetsDown++;
    }

    /// <summary>Handles 'refuse' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void Refused(Guid _roomId, Packet _packet)
    {
        GameManager.instance.Refused();
    }

    /// <summary>Handles 'disconnected' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void Disconnected(Guid _roomId, Packet _packet)
    {
        if (ClientInfo.instance.RoomId == _roomId)
        {
            GameManager.instance.LeaveRoom();
        }
        else
        {
            Debug.LogWarning("Received 'Disconnected' message from wrong room;");
        }

        Debug.Log($"Reached server, sending introduciton data.");
        // Send client information
        ClientSend.Introduction();

        Analytics.bandwidthDown += 20;
        Analytics.packetsDown++;
    }

    /// <summary>Handles 'joinedRoom' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void JoinedRoom(Guid _roomId, Packet _packet)
    {
        string ip =_packet.ReadString();
        int port =_packet.ReadInt();
        int spawnId =_packet.ReadInt();
        int _roomTickrate = _packet.ReadInt();

        Debug.Log($"SpawnId: {spawnId}");

        ClientInfo.instance.RoomId = _roomId;
        GameLogic.Tickrate = _roomTickrate;
        // Start listening to room
        Client.ConnectUdp(ip, port);
        // Add themselves to the playerCount
        GameManager.instance.UpdatePlayerCount();
        Client.instance.isConnected = true;

        GameManager.instance.isOnline = true;

        // Add my spawnId
        ClientInfo.instance.SpawnId = spawnId;

        Debug.Log($"Joined room {_roomId}, multicast info Ip:{ip} Port:{port}");

        Analytics.bandwidthDown += 45;
        Analytics.packetsDown++;
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

        Analytics.bandwidthDown += 57;
        Analytics.packetsDown++;
    }

    /// <summary>Handles 'playerLeft' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void PlayerLeft(Guid _roomId, Packet _packet)
    {
        Guid clientId = _packet.ReadGuid();
        Client.peers[clientId].Disconnect();
        Client.peers.Remove(clientId);

        Analytics.bandwidthDown += 40;
        Analytics.packetsDown++;
    }

    /// <summary>Handles 'map' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void Map(Guid _roomId, Packet _packet)
    {
        string level = _packet.ReadString();

        // LoadScene
        GameManager.instance.LoadGameScene(level);

        Analytics.bandwidthDown += 31;
        Analytics.packetsDown++;
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

        Analytics.bandwidthDown += 24;
        Analytics.packetsDown++;
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

        Analytics.bandwidthDown += 24;
        Analytics.packetsDown++;
    }

    /// <summary>Handles 'playerMovement' packet sent from other clients.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void PlayerMovement(Guid _clientId, Packet _packet)
    {
        int _tick = _packet.ReadInt();

        Vector3 _position = _packet.ReadVector3();
        Quaternion _rotation = _packet.ReadQuaternion();
        bool _ragdoll = _packet.ReadBool();
        int _animation = _packet.ReadInt();

        GameManager.instance.PlayerMovement(_clientId, _tick, _position, _rotation, _ragdoll, _animation);

        Analytics.bandwidthDown += 45;
        Analytics.packetsDown++;
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
        Vector3 velocity =_packet.ReadVector3();
        Vector3 angularVelocity =_packet.ReadVector3();
        bool ragdoll =_packet.ReadBool();

        GameManager.instance.PlayerCorrection(new SimulationState(simulationFrame, position, rotation, velocity, angularVelocity, ragdoll));

        Analytics.bandwidthDown += 81;
        Analytics.packetsDown++;
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

        Analytics.bandwidthDown += 28;
        Analytics.packetsDown++;
    }

    /// <summary>Handles 'playeerGrab' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void PlayerGrab(Guid _roomId, Packet _packet)
    {
        Guid _grabber = _packet.ReadGuid();

        GameManager.instance.PlayerGrab(_grabber);

        Analytics.bandwidthDown += 24;
        Analytics.packetsDown++;
    }

    /// <summary>Handles 'playeerLetGo' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void PlayerLetGo(Guid _roomId, Packet _packet)
    {
        Guid _grabber = _packet.ReadGuid();

        GameManager.instance.PlayerLetGo(_grabber);

        Analytics.bandwidthDown += 24;
        Analytics.packetsDown++;
    }

    /// <summary>Handles 'playeerPush' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void PlayerPush(Guid _roomId, Packet _packet)
    {
        Guid _pusher = _packet.ReadGuid();

        GameManager.instance.PlayerPush(_pusher);

        Analytics.bandwidthDown += 24;
        Analytics.packetsDown++;
    }

    /// <summary>Handles 'playerFinish' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void PlayerFinish(Guid _roomId, Packet _packet)
    {
        Guid clientId = _packet.ReadGuid();

        GameManager.instance.PlayerFinish(clientId);

        Analytics.bandwidthDown += 24;
        Analytics.packetsDown++;
    }

    /// <summary>Handles 'serverTick' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void ServerTick(Guid _roomId, Packet _packet)
    {
        int _roomTick = _packet.ReadInt();
        float _roomClock = _packet.ReadFloat();

        if (_roomTick > GameLogic.Tick)
            GameLogic.SetTick(_roomTick);

        if (_roomClock > GameLogic.Clock)
            GameLogic.SetClock(_roomClock);

        Analytics.bandwidthDown += 16;
        Analytics.packetsDown++;
    }

    /// <summary>Handles 'pong' packet sent from the server.</summary>
    /// <param name="_packet">The received packet.</param>
    public static void Pong(Guid not_needed, Packet _packet)
    {
        // We are receive the ping packet, update the stored ping variable
        Client.instance.ping = Math.Round((DateTime.UtcNow - Client.instance.pingSent).TotalMilliseconds, 0);

        Analytics.bandwidthDown += 8;
        Analytics.packetsDown++;
    }
}
