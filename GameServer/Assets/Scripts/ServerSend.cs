using System;
using UnityEngine;

// Server only uses this class to clients that are not in a room
public class ServerSend
{
    #region methods of sending info
    private static void SendTCPData(Guid toClient, Packet packet)
    {
        packet.WriteLength();
        Server.Clients[toClient].tcp.SendData(packet);
    }

    private static void SendUDPData(Guid toClient, Packet packet)
    {
        packet.WriteLength();
        Server.Clients[toClient].udp.SendData(packet);
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

    public static void JoinedRoom(Guid _toClient, Guid _roomId, string _lobbyIP, int _lobbyPort, int _spawnPos)
    {
        using (Packet packet = new Packet((int)ServerPackets.joinedRoom))
        {
            packet.Write(_roomId);
            packet.Write(_lobbyIP);
            packet.Write(_lobbyPort);
            packet.Write(_spawnPos);

            SendTCPData(_toClient, packet);
        }
    }
    #endregion
}
