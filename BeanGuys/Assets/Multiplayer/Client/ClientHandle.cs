﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class ClientHandle : MonoBehaviour
{
    #region Server Packets
    //Receives id set to player by server
    public static void WelcomeServer(Guid not_needed, Packet packet)
    {
        Guid myId = packet.ReadGuid();
        Client.instance.clientInfo.id = myId;
        Debug.Log($"[SERVER] Welcome, your Id is {myId}");

        ClientSend.WelcomeReceived();
    }

    //Receives the information needed to start listening to room multicast messages
    public static void JoinedRoom(Guid not_needed, Packet packet)
    {
        string ip = packet.ReadString();
        int port = packet.ReadInt();
        int spawnId = packet.ReadInt();

        //Start listening to room
        Client.ListenToRoom(ip, port);
        //Add themselves to the playerCount
        GameManager.instance.totalPlayers++;
        //Add my spawnid
        Client.instance.clientInfo.spawnId = spawnId;

        Debug.Log($"Joined room, multicast info Ip:{ip} Port:{port}");
    }
    
    public static void PlayerJoined(Guid id, Packet packet)
    {
        string username = packet.ReadString();
        string ip = packet.ReadString();
        string port = packet.ReadString();
        int spawnId = packet.ReadInt();

        Debug.Log($"received Ip:{ip} Port:{port}");
        if (Client.instance.clientExeID == 1)
            Client.instance.ConnectToPeer(id, username, spawnId, "127.0.0.1", 5002);
        else
            Client.instance.ConnectToPeer(id, username, spawnId, "127.0.0.1", 5001);
    }
    
    public static void PlayerLeft(Guid id, Packet packet)
    {
        Client.peers[id].Disconnect();
    }
    
    public static void Map(Guid id, Packet packet)
    {
        Debug.Log("Got map");
        int levelId = packet.ReadInt();
        
        //LoadScene
        GameManager.instance.LoadGameScene(levelId);
    }

    public static void StartGame(Guid id, Packet packet)
    {
        GameManager.instance.StartGame();
    }
    #endregion

    #region Peer Packets
    //Received an welcome from the peer i tried to connect to
    public static void WelcomePeer(Guid id, Packet packet)
    {
        GameManager.instance.UpdatePlayerCount();
        Debug.Log($"{"legend27"}(player {id}) has joined the game!");
    }

    #region Game packages
    public static void PlayerMovement(Guid id, Packet packet)
    {
        Vector3 position = packet.ReadVector3();
        Quaternion rotation = packet.ReadQuaternion();
        Vector3 velocity = packet.ReadVector3();
        Vector3 angular_velocity = packet.ReadVector3();
        float tick_number = packet.ReadFloat();

        GameManager.instance.PlayerMovement(id, position, rotation, velocity, angular_velocity, tick_number);
    }

    public static void PlayerAnim(Guid id, Packet packet)
    {
        int animNum = packet.ReadInt();
        GameManager.instance.PlayerAnim(id, animNum);
    }

    public static void PlayerRespawn(Guid id, Packet packet)
    {
        int checkPointNum = packet.ReadInt();
        GameManager.instance.PlayerRespawn(id, checkPointNum);
    }

    public static void PlayerFinish(Guid id, Packet packet)
    {
        float time = packet.ReadFloat();
        GameManager.instance.PlayerFinish(id, time);
    }
    #endregion
    #endregion
}
