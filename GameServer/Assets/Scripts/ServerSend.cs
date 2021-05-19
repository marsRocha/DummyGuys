using System;
using UnityEngine;

public class ServerSend
{
    #region methods of sending info
    private static void SendTCPData(Guid toClient, Packet packet)
    {
        packet.WriteLength();
        Server.Clients[toClient].tcp.SendData(packet);
    }

    private static void SendTCPDataToAll(Packet packet)
    {
        packet.WriteLength();
        foreach (Client client in Server.Clients.Values)
        {
            client.tcp.SendData(packet);
        }
    }

    private static void SendUDPData(Guid toClient, Packet packet)
    {
        packet.WriteLength();
        Server.Clients[toClient].udp.SendData(packet);
    }

    private static void SendUDPDataToAll(Packet packet)
    {
        packet.WriteLength();
        foreach (Client client in Server.Clients.Values)
        {
            client.udp.SendData(packet);
        }
    }

    private static void SendUDPDataToAll(Guid exceptionClient, Packet packet)
    {
        packet.WriteLength();
        foreach (Client client in Server.Clients.Values)
        {
            if (client.Id != exceptionClient)
                client.udp.SendData(packet);
        }
    }
    #endregion

    #region Packets
    public static void Welcome(Guid _toClient)
    {
        using (Packet packet = new Packet((int)ServerPackets.welcome))
        {
            Debug.Log(_toClient);
            packet.Write(_toClient);

            SendTCPData(_toClient, packet);
        }
    }

    public static void JoinedRoom(Guid _toClient, string _lobbyIP, int lobbyPort)
    {
        using (Packet packet = new Packet((int)ServerPackets.joinedRoom))
        {
            packet.Write(_lobbyIP);
            packet.Write(lobbyPort);

            SendTCPData(_toClient, packet);
        }
    }
    #endregion
}
