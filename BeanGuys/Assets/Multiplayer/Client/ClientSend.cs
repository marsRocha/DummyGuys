using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientSend : MonoBehaviour
{
    #region methods of sending info
    private static void SendTCPDataToServer(Packet packet)
    {
        packet.WriteLength();
        Client.instance.server.SendData(packet);
    }

    private static void SendTCPData(int toClient, Packet packet)
    {
        packet.WriteLength();
        Client.peers[toClient].tcp.SendData(packet);
    }

    private static void SendTCPDataToAll(Packet packet)
    {
        packet.WriteLength();

        foreach (Peer p in Client.peers.Values)
            p.tcp.SendData(packet);
    }

    private static void SendUDPData(int toClient, Packet packet)
    {
        packet.WriteLength();
        //Client.peers[toClient].udp.SendData(packet);
        Client.SendUDPData(Client.peers[toClient].udp.endPoint, packet);
    }

    private static void SendUDPDataToAll(Packet packet)
    {
        packet.WriteLength();

        foreach(Peer p in Client.peers.Values)
            Client.SendUDPData(p.udp.endPoint, packet);
    }
    #endregion

    #region Packets
    //Sent to server when it receives a welcome message
    public static void WelcomeReceived()
    {
        using (Packet packet = new Packet((int)ClientPackets.welcomeReceived))
        {
            packet.Write(Client.instance.myId);
            packet.Write($"legend27");

            SendTCPDataToServer(packet);
        }
    }

    public static void WelcomeReceived(int toClient)
    {
        using (Packet packet = new Packet((int)ClientPackets.welcomeReceived))
        {
            packet.Write(Client.instance.myId);
            packet.Write($"legend27");

            SendTCPData(toClient, packet);
        }
    }

    //Sent to a peer trying to connect
    public static void WelcomePeer(int toClient)
    {
        Debug.Log("WelcomePeer sent");
        using (Packet packet = new Packet((int)ClientPackets.welcome))
        {
            packet.Write(Client.instance.myId);

            SendTCPData(toClient, packet);
        }
    }
    public static void Introduction(int toPeer)
    {
        using (Packet packet = new Packet((int)ClientPackets.introduction))
        {
            packet.Write(Client.instance.myId);
            packet.Write($"legend27");

            SendTCPData(toPeer, packet);
        }
    }


    #region GameInfo
    //TODO: Remove from here, the Server should be the one to send this message
    public static void StartGame()
    {
        using (Packet packet = new Packet((int)ClientPackets.startGame))
        {
            SendTCPDataToAll(packet);
        }
    }

    public static void PlayerMovement(Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angular_velocity, float tick_number)
    {
        //Debug.Log("Sent input");
        using (Packet packet = new Packet((int)ClientPackets.playerMovement))
        {
            packet.Write(Client.instance.myId);

            packet.Write(position);
            packet.Write(rotation);
            packet.Write(velocity);
            packet.Write(angular_velocity);
            packet.Write(tick_number);

            SendUDPDataToAll(packet);
        }
    }

    public static void PlayerAnim(int anim)
    {
        //Debug.Log("Sent input");
        using (Packet packet = new Packet((int)ClientPackets.playerAnim))
        {
            packet.Write(Client.instance.myId);
            packet.Write(anim);

            SendUDPDataToAll(packet);
        }
    }

    public static void PlayerRespawn(int checkpointNum)
    {
        //Debug.Log("Sent input");
        using (Packet packet = new Packet((int)ClientPackets.playerRespawn))
        {
            packet.Write(Client.instance.myId);
            packet.Write(checkpointNum);

            SendUDPDataToAll(packet);
        }
    }

    public static void PlayerFinish(float time)
    {
        //Debug.Log("Sent input");
        using (Packet packet = new Packet((int)ClientPackets.playerFinish))
        {
            packet.Write(Client.instance.myId);
            packet.Write(time);

            SendUDPDataToAll(packet);
        }
    }
    #endregion

    #endregion
}
