using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class ClientHandle : MonoBehaviour
{
    #region Server Packets
    //Receives id set to player by server
    public static void WelcomeServer(Guid not_needed, Packet _packet)
    {
        Guid myId = _packet.ReadGuid();
        Client.instance.clientInfo.id = myId;
        Debug.Log($"[SERVER] Welcome, your Id is {myId}");

        ClientSend.WelcomeReceived();
    }

    //Receives the information needed to start listening to room multicast messages
    public static void JoinedRoom(Guid not_needed, Packet _packet)
    {
        Guid roomId =_packet.ReadGuid();
        string ip =_packet.ReadString();
        int port =_packet.ReadInt();
        int spawnId =_packet.ReadInt();

        Client.RoomId = roomId;
        //Start listening to room
        Client.ListenToRoom(ip, port);
        //Add themselves to the playerCount
        GameManager.instance.UpdatePlayerCount();
        //Add my spawnid
        Client.instance.clientInfo.spawnId = spawnId;

        Debug.Log($"Joined room, multicast info Ip:{ip} Port:{port}");
    }

    public static void PlayerJoined(Guid _roomId, Packet _packet)
    {
        //TODO: MESSAGE CAN BE SENT BY THE SERVER(TCP) OR THE ROOM(UDP MULTICAST) FOR NOW BECAUSE TCP DDOES NOT READ ROOMID 
        if (_roomId == null || _roomId == Guid.Empty)
            _roomId = _packet.ReadGuid();

        if (Client.RoomId == _roomId)
        {
            Guid id = _packet.ReadGuid();
            //If not me
            if (Client.instance.clientInfo.id != id)
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
    
    public static void PlayerLeft(Guid _roomId, Packet _packet)
    {
        if (Client.RoomId == _roomId)
        {
            Guid clientId = _packet.ReadGuid();
            Client.peers[clientId].Disconnect();
            Client.peers.Remove(clientId);
        }
        else
        {
            Debug.LogWarning("Received 'PlayerLeft' message from wrong room;");
        }
    }
    
    public static void Map(Guid _roomId, Packet _packet)
    {
        _roomId = Client.RoomId; //TODO: TEMPORARY SINCE WE FOR NOW USE TCP _ROOMID IS NOT SENT THUS NOT RECEIVED

        if (Client.RoomId == _roomId)
        {
            Debug.Log("Got map");
            string level = _packet.ReadString();
            //LoadScene
            GameManager.instance.LoadGameScene(level);
        }
        else
        {
            Debug.LogWarning("Received 'Map' message from wrong room;");
        }
    }

    public static void StartGame(Guid _roomId, Packet _packet)
    {
        _roomId = Client.RoomId; //TODO: TEMPORARY SINCE WE FOR NOW USE TCP _ROOMID IS NOT SENT THUS NOT RECEIVED

        if (Client.RoomId == _roomId)
        {
            GameManager.instance.StartGame();
        }
        else
        {
            Debug.LogWarning("Received 'StartGame' message from wrong room;");
        }
    }

    public static void EndGame(Guid _roomId, Packet _packet)
    {
        _roomId = Client.RoomId; //TODO: TEMPORARY SINCE WE FOR NOW USE TCP _ROOMID IS NOT SENT THUS NOT RECEIVED

        if (Client.RoomId == _roomId)
        {
            GameManager.instance.EndGame();
        }
        else
        {
            Debug.LogWarning("Received 'EndGame' message from wrong room;");
        }
    }
    #endregion

    #region Player packages
    public static void PlayerMovement(Guid _clientId, Packet _packet)
    {
        Vector3 position = _packet.ReadVector3();
        Quaternion rotation = _packet.ReadQuaternion();
        Vector3 velocity = _packet.ReadVector3();
        Vector3 angular_velocity = _packet.ReadVector3();
        float tick_number = _packet.ReadFloat();

        GameManager.instance.PlayerMovement(_clientId, position, rotation, velocity, angular_velocity, tick_number);
    }

    public static void PlayerCorrection(Guid _roomId, Packet _packet)
    {
        Guid clientId = _packet.ReadGuid();
        Vector3 position =_packet.ReadVector3();
        Quaternion rotation =_packet.ReadQuaternion();
        Vector3 velocity =_packet.ReadVector3();
        int simulationFrame =_packet.ReadInt();

        GameManager.instance.PlayerCorrection(clientId, new SimulationState(position, rotation, velocity, simulationFrame));
    }

    public static void PlayerAnim(Guid id, Packet _packet)
    {
        int animNum =_packet.ReadInt();
        GameManager.instance.PlayerAnim(id, animNum);
    }

    public static void PlayerRespawn(Guid _roomId, Packet _packet)
    {
        Guid clientId = _packet.ReadGuid();
        int checkPointNum =_packet.ReadInt();
        GameManager.instance.PlayerRespawn(clientId, checkPointNum);
    }

    public static void PlayerFinish(Guid _roomId, Packet _packet)
    {
        Guid clientId = _packet.ReadGuid();
        float time =_packet.ReadFloat();
        GameManager.instance.PlayerFinish(clientId, time);
    }
    #endregion
}
