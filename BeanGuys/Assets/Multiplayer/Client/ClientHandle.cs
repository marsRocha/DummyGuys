using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class ClientHandle : MonoBehaviour
{
    //Receives id set to player by server
    public static void WelcomeServer(Packet packet)
    {
        int myId = packet.ReadInt();

        Debug.Log($"[SERVER] Welcome, your Id is {myId}");
        Client.instance.myId = myId;
        ClientSend.WelcomeReceived();
    }

    //Receives list of peers to connect to
    public static void PeerList(Packet packet)
    {
        string ip = packet.ReadString();
        string port = packet.ReadString();

        Debug.Log($"received Ip:{ip} Port:{port}");
        if(Client.instance.clientExeID == 1)
        {
            Debug.Log($"using Ip:127.0.0.3 Port:5002");
            Client.instance.ConnectToPeer($"127.0.0.3", 5002);
        }
        else
        {
            Debug.Log($"using Ip:127.0.0.2 Port:5001");
            Client.instance.ConnectToPeer($"127.0.0.2", 5001);
        }
    }

    //Received an welcome from the peer i tried to connect to
    public static void WelcomePeer(Packet packet)
    {
        int peerID = packet.ReadInt();
        //TODO: Receive username also

        ClientSend.WelcomeReceived(peerID);

        //Initiate udp connection
        //This is a problem as peers have "..0.2+" but since testing on the same network it does not work like that
        Client.peers[peerID].udp.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), ((IPEndPoint)Client.peers[peerID].tcp.socket.Client.RemoteEndPoint).Port));
        
        //Store it's information
        //TODO: Store the players skin also
        Client.peers[peerID].SetIdentification(peerID, "legend27");
        GameManager.instance.UpdatePlayerCount();

        Debug.Log($"{"legend27"}(player {peerID}) has joined the game!");
    }

    public static void WelcomeReceived(Packet packet)
    {
        int peerID = packet.ReadInt();
        string username = packet.ReadString();

        //Store it's information
        //TODO: Store the players skin also
        Client.peers[peerID].SetIdentification(peerID, username);
        GameManager.instance.UpdatePlayerCount();

        Debug.Log($"{username}(player {peerID}) has joined the game!");
    }

    #region Game packages
    public static void StartGame(Packet packet)
    {
        GameManager.instance.StartGameDebug();
    }

    public static void PlayerMovement(Packet packet)
    {
        int peerID = packet.ReadInt();

        Vector3 position = packet.ReadVector3();
        Quaternion rotation = packet.ReadQuaternion();
        Vector3 velocity = packet.ReadVector3();
        Vector3 angular_velocity = packet.ReadVector3();
        int tick_number = packet.ReadInt();

        GameManager.instance.PlayerMovement(peerID, position, rotation, velocity, angular_velocity, tick_number);
    }

    public static void PlayerAnim(Packet packet)
    {
        int peerID = packet.ReadInt();
        int animNum = packet.ReadInt();

        GameManager.instance.PlayerAnim(peerID, animNum);
    }

    public static void PlayerRespawn(Packet packet)
    {
        int id = packet.ReadInt();
        int checkPointNum = packet.ReadInt();

        GameManager.instance.PlayerRespawn(id, checkPointNum);
    }

    public static void PlayerFinish(Packet packet)
    {
        int id = packet.ReadInt();
        float time = packet.ReadFloat();

        GameManager.instance.PlayerFinish(id, time);
    }
    #endregion
}
