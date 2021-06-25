using System;
using UnityEngine;

/// <summary>
/// Contains all methods to handle incoming messages from the server.
/// </summary>
public class ClientHandle : MonoBehaviour
{
    /// <summary>Handles 'welcome' packet sent from the server.</summary>
    /// <param name="_packet">The recieved packet.</param>
    public static void WelcomeServer(Guid not_needed, Packet _packet)
    {
        Guid myId = _packet.ReadGuid();
        ClientInfo.instance.Id = myId;
        Debug.Log($"[SERVER] Welcome, your Id is {myId}");

        ClientSend.WelcomeReceived();
    }

    /// <summary>Handles 'joinedRoom' packet sent from the server.</summary>
    /// <param name="_packet">The recieved packet.</param>
    public static void JoinedRoom(Guid not_needed, Packet _packet)
    {
        Guid roomId =_packet.ReadGuid();
        string ip =_packet.ReadString();
        int port =_packet.ReadInt();
        int spawnId =_packet.ReadInt();

        ClientInfo.instance.RoomId = roomId;
        //Start listening to room
        Client.ListenToRoom(ip, port);
        //Add themselves to the playerCount
        GameManager.instance.UpdatePlayerCount();

        GameManager.instance.isOnline = true;

        //Add my spawnid
        ClientInfo.instance.SpawnId = spawnId;

        Debug.Log($"Joined room, multicast info Ip:{ip} Port:{port}");
    }

    /// <summary>Handles 'playerJoined' packet sent from the server.</summary>
    /// <param name="_packet">The recieved packet.</param>
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
                int spawnId = _packet.ReadInt();

                Client.peers.Add(id, new Peer(id, username, spawnId));
                GameManager.instance.UpdatePlayerCount();
                Debug.Log($"{username} has joined the game!");
            }
        }
        else
        {
            Debug.LogWarning("Received 'PlayerJoined' message from wrong room;");
        }
    }

    /// <summary>Handles 'playerLeft' packet sent from the server.</summary>
    /// <param name="_packet">The recieved packet.</param>
    public static void PlayerLeft(Guid _roomId, Packet _packet)
    {
        Guid clientId = _packet.ReadGuid();
        Client.peers[clientId].Disconnect();
        Client.peers.Remove(clientId);
    }

    /// <summary>Handles 'map' packet sent from the server.</summary>
    /// <param name="_packet">The recieved packet.</param>
    public static void Map(Guid _roomId, Packet _packet)
    {
        string level = _packet.ReadString();

        // LoadScene
        GameManager.instance.LoadGameScene(level);
    }

    /// <summary>Handles 'startGame' packet sent from the server.</summary>
    /// <param name="_packet">The recieved packet.</param>
    public static void StartGame(Guid _roomId, Packet _packet)
    {
        if (ClientInfo.instance.RoomId == _roomId)
        {
            GameManager.instance.StartGame();
        }
        else
        {
            Debug.LogWarning("Received 'StartGame' message from wrong room;");
        }
    }

    /// <summary>Handles 'endGame' packet sent from the server.</summary>
    /// <param name="_packet">The recieved packet.</param>
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
    }

    /// <summary>Handles 'playerMovement' packet sent from other clients.</summary>
    /// <param name="_packet">The recieved packet.</param>
    public static void PlayerMovement(Guid _clientId, Packet _packet)
    {
        Vector3 _position = _packet.ReadVector3();
        Quaternion _rotation = _packet.ReadQuaternion();
        int _tick = _packet.ReadInt();

        GameManager.instance.PlayerMovement(_clientId, _position, _rotation, _tick);
    }

    /// <summary>Handles 'playerCorrection' packet sent from the server.</summary>
    /// <param name="_packet">The recieved packet.</param>
    public static void PlayerCorrection(Guid _roomId, Packet _packet)
    {
        Guid _clientId = _packet.ReadGuid();

        Vector3 position =_packet.ReadVector3();
        Quaternion rotation =_packet.ReadQuaternion();
        Vector3 velocity =_packet.ReadVector3();
        int simulationFrame =_packet.ReadInt();

        //TODO: FOR NOW SERVER SEND MULTICAST, SHOULD I CHANGE IT?
        //TODO: ALSO OTHER METHODS ARE CHECKING IF ROOM IS CORRECT, CHANGE THAT TO BEFORE COMING TO THIS FUNCTIONS

        if (_clientId == ClientInfo.instance.Id)
            GameManager.instance.PlayerCorrection(new SimulationState(position, rotation, velocity, simulationFrame));
    }

    /// <summary>Handles 'playerRespawn' packet sent from the server.</summary>
    /// <param name="_packet">The recieved packet.</param>
    public static void PlayerRespawn(Guid _roomId, Packet _packet)
    {
        Guid clientId = _packet.ReadGuid();
        int checkPointNum =_packet.ReadInt();
        GameManager.instance.PlayerRespawn(clientId, checkPointNum);
    }

    /// <summary>Handles 'playerFinish' packet sent from the server.</summary>
    /// <param name="_packet">The recieved packet.</param>
    public static void PlayerFinish(Guid _roomId, Packet _packet)
    {
        Guid clientId = _packet.ReadGuid();

        GameManager.instance.PlayerFinish(clientId);
    }

    /// <summary>Handles 'serverTick' packet sent from the server.</summary>
    /// <param name="_packet">The recieved packet.</param>
    public static void ServerTick(Guid _roomId, Packet _packet)
    {
        int _serverTick = _packet.ReadInt();

        if (_serverTick > GlobalVariables.serverTick)
            GlobalVariables.serverTick = _serverTick;
    }
}
