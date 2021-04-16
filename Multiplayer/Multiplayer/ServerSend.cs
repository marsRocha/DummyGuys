using System;
using System.Collections.Generic;
using System.Text;

namespace Multiplayer
{
    class ServerSend
    {
        #region methods of sending info
        private static void SendTCPData(int toClient, Packet packet)
        {
            packet.WriteLength();
            Server.Clients[toClient].tcp.SendData(packet);
        }

        private static void SendTCPDataToAll(Packet packet)
        {
            packet.WriteLength();
            for (int i = 1; i <= Server.MaxPlayersPerLobby; i++)
            {
                Server.Clients[i].tcp.SendData(packet);
            }
        }

        private static void SendUDPData(int toClient, Packet packet)
        {
            packet.WriteLength();
            Server.Clients[toClient].udp.SendData(packet);
        }

        private static void SendUDPDataToAll(Packet packet)
        {
            packet.WriteLength();
            for (int i = 1; i <= Server.MaxPlayersPerLobby; i++)
            {
                Server.Clients[i].udp.SendData(packet);
            }
        }

        private static void SendUDPDataToAll(int exceptionClient, Packet packet)
        {
            packet.WriteLength();
            for (int i = 1; i <= Server.MaxPlayersPerLobby; i++)
            {
                if (i != exceptionClient)
                    Server.Clients[i].udp.SendData(packet);
            }
        }
        #endregion

        #region Packets
        public static void Welcome(int toClient)
        {
            using (Packet packet = new Packet((int)ServerPackets.welcome))
            {
                packet.Write(toClient);

                SendTCPData(toClient, packet);
            }
        }


        public static void Peer(int peerId, string username, string peerIP, string peerPort, int toClient)
        {
            using (Packet packet = new Packet((int)ServerPackets.peer))
            {
                packet.Write(peerId);
                packet.Write(username);
                packet.Write(peerIP);
                packet.Write(peerPort);

                SendTCPData(toClient, packet);
            }
        }

        public static void JoinedRoom(int toClient, string lobbyIP, int lobbyPort)
        {
            using (Packet packet = new Packet((int)ServerPackets.joinedRoom))
            {
                packet.Write(lobbyIP);
                packet.Write(lobbyPort);

                SendTCPData(toClient, packet);
            }
        }
        #endregion
    }
}
