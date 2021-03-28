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

        Debug.Log($"{"legend27"}(player {peerID}) has joined the game!");
        ClientSend.WelcomeReceived(peerID);

        //Initiate udp connection
        //Client.peers[peerID].udp.Connect(((IPEndPoint)Client.peers[peerID].tcp.socket.Client.LocalEndPoint).Address, ((IPEndPoint)Client.peers[peerID].tcp.socket.Client.LocalEndPoint).Port);
        Client.peers[peerID].udp.Connect(((IPEndPoint)Client.peers[peerID].tcp.socket.Client.RemoteEndPoint));

        //TODO: Instantiate player in the world
        GameManager.instance.SpawnRemotePlayer(peerID, "legend27");
    }

    public static void WelcomeReceived(Packet packet)
    {
        int peerID = packet.ReadInt();
        string username = packet.ReadString();

        Debug.Log($"{username}(player {peerID}) has joined the game!");

        //TODO: Instantiate player in the world
        GameManager.instance.SpawnRemotePlayer(peerID, username);
    }

    #region Game packages
    public static void PlayerInput(Packet packet)
    {
        int peerID = packet.ReadInt();

        int x = packet.ReadInt();
        int y = packet.ReadInt();
        bool jump = packet.ReadBool();
        bool dive = packet.ReadBool();
        int tick_number = packet.ReadInt();

        //CSceneManager.instance.players[peerID].SetInput(inputs, rotation);
        Debug.Log($"id:{peerID}, Got input: x:{x}, y:{y}");
        GameManager.instance.AddInputMessage(peerID, x, y, jump, dive, tick_number);
    }

    public static void UDPTest(Packet packet)
    {
        string msg = packet.ReadString();

        Debug.Log($"received message:{msg}");
    }

    #endregion
}
