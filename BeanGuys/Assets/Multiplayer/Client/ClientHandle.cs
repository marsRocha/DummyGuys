using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

public class ClientHandle : MonoBehaviour
{
    //Receives id set to player by server
    public static void WelcomeServer(Guid not_needed, Packet packet)
    {
        Guid myId = packet.ReadGuid();
        Client.instance.myId = myId;
        Debug.Log($"[SERVER] Welcome, your Id is {myId}");

        ClientSend.WelcomeReceived();
    }

    //Receives list of peers to connect to
    public static void PeerList(Guid not_needed, Packet packet)
    {
        Guid id = packet.ReadGuid();
        string username = packet.ReadString();
        string ip = packet.ReadString();
        string port = packet.ReadString();

        Debug.Log($"received Ip:{ip} Port:{port}");
        if(Client.instance.clientExeID == 1)
            Client.instance.ConnectToPeer(id, username, "127.0.0.1", 5002);
        else
            Client.instance.ConnectToPeer(id, username, "127.0.0.1", 5001);
    }

    public static void JoinedRoom(Guid not_needed, Packet packet)
    {
        string ip = packet.ReadString();
        int port = packet.ReadInt();

        Debug.Log($"Joined room, multicast info Ip:{ip} Port:{port}");
    }

    //Received an welcome from the peer i tried to connect to
    public static void WelcomePeer(Guid id, Packet packet)
    {
        //Connect udp
        if (Client.instance.clientExeID == 1)
            Client.peers[id].udp.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5002)); //((IPEndPoint)Client.peers[id].tcp.socket.Client.RemoteEndPoint).Port));
        else
            Client.peers[id].udp.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5001)); //((IPEndPoint)Client.peers[id].tcp.socket.Client.RemoteEndPoint).Port));

        GameManager.instance.UpdatePlayerCount();

        Debug.Log($"{"legend27"}(player {id}) has joined the game!");
    }

    #region Game packages
    public static void StartGame(Guid id, Packet packet)
    {
        GameManager.instance.StartGameDebug();
    }

    public static void PlayerMovement(Guid id, Packet packet)
    {
        Vector3 position = packet.ReadVector3();
        Quaternion rotation = packet.ReadQuaternion();
        Vector3 velocity = packet.ReadVector3();
        Vector3 angular_velocity = packet.ReadVector3();
        int tick_number = packet.ReadInt();

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
}
