using System;
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

    private static void SendTCPData(Guid toClient, Packet packet)
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

    private static void SendUDPData(Guid toClient, Packet packet)
    {
        packet.WriteLength();
        Client.SendUDPData(Client.peers[toClient].udp.endPoint, packet);
    }

    private static void SendUDPData(Packet packet)
    {
        packet.WriteLength();
        Debug.Log("sent normal udp");
        Client.SendUDPData(packet);
    }

    private static void MulticastUDPData(Packet packet)
    {
        packet.WriteLength();
        Client.MulticastUDPData(packet);
    }
    #endregion

    #region Packets
    //Send to server when it receives a welcome message
    public static void WelcomeReceived()
    {
        using (Packet packet = new Packet((int)ClientPackets.welcomeReceived))
        {
            packet.Write(Client.instance.clientInfo.id);
            packet.Write(Client.instance.clientInfo.username);
            //packet.Write(Client.MyPort);

            SendTCPDataToServer(packet);
        }
    }

    //Send identification to peer that we want to connect to 
    public static void Introduction(Guid toPeer)
    {
        using (Packet packet = new Packet((int)ClientPackets.introduction))
        {
            packet.Write(Client.instance.clientInfo.id);
            packet.Write(Client.instance.clientInfo.username);
            packet.Write(Client.instance.clientInfo.spawnId);

            SendTCPData(toPeer, packet); //TODO: Change to udp when it's reliable
        }
    }

    //Send acknoledgement of connection
    public static void WelcomePeer(Guid toClient)
    {
        using (Packet packet = new Packet((int)ClientPackets.welcome))
        {
            packet.Write(Client.instance.clientInfo.id);

            SendTCPData(toClient, packet);
        }
    }

    #region TODO: REMOVE / P2P TEMPORARY FUNCTIONS
    public static void Map()
    {
        Debug.Log("Sent Map");
        using (Packet packet = new Packet((int)ClientPackets.map))
        {
            packet.Write(Client.instance.clientInfo.id);

            packet.Write(1);

            MulticastUDPData(packet);
        }
    }

    public static void StartGame()
    {
        using (Packet packet = new Packet((int)ClientPackets.startGame))
        {
            packet.Write(Client.instance.clientInfo.id);

            MulticastUDPData(packet);
        }
    }
    #endregion

    #region GameInfo
    /*public static void PlayerMovement(int x, int y, bool jump, bool dive, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angular_velocity, float tick_number)
    {
        using (Packet packet = new Packet((int)ClientPackets.playerMovement))
        {
            packet.Write(Client.instance.clientInfo.id);

            packet.Write(tick_number);

            packet.Write(x);
            packet.Write(y);
            packet.Write(jump);
            packet.Write(dive);

            packet.Write(position);
            packet.Write(rotation);
            packet.Write(velocity);
            packet.Write(angular_velocity);

            MulticastUDPData(packet);
        }
    }*/

    public static void PlayerMovement(Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angular_velocity, float tick_number)
    {
        using (Packet packet = new Packet((int)ClientPackets.playerMovement))
        {
            packet.Write(Client.instance.clientInfo.id);

            packet.Write(position);
            packet.Write(rotation);
            packet.Write(velocity);
            packet.Write(angular_velocity);
            packet.Write(tick_number);

            MulticastUDPData(packet);
        }
    }

    public static void PlayerAnim(int anim)
    {
        using (Packet packet = new Packet((int)ClientPackets.playerAnim))
        {
            packet.Write(Client.instance.clientInfo.id);
            packet.Write(anim);

            MulticastUDPData(packet);
        }
    }

    public static void PlayerRespawn(int checkpointNum)
    {
        using (Packet packet = new Packet((int)ClientPackets.playerRespawn))
        {
            packet.Write(Client.instance.clientInfo.id);
            packet.Write(checkpointNum);

            MulticastUDPData(packet);
        }
    }

    public static void PlayerFinish(float time)
    {
        using (Packet packet = new Packet((int)ClientPackets.playerFinish))
        {
            packet.Write(Client.instance.clientInfo.id);
            packet.Write(time);

            MulticastUDPData(packet);
        }
    }
    #endregion

    #endregion
}
