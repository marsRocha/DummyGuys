using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientSend : MonoBehaviour
{
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

    #region GameInfo
    //inputs = x and y axis, corresponding to the translation in units
    //dive - bevaiour
    //jump - behaviour
    public static void PlayerInput(int x, int y, bool jump, bool dive, int tick_number)
    {
        Debug.Log("Sent input");
        using (Packet packet = new Packet((int)ClientPackets.playerInput))
        {
            packet.Write(Client.instance.myId);

            packet.Write(x);
            packet.Write(y);
            packet.Write(jump);
            packet.Write(dive);
            packet.Write(tick_number);

            SendUDPDataToAll(packet);
        }
    }

    #endregion

    #endregion
}
