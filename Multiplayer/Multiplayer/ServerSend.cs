using System;
using System.Collections.Generic;
using System.Text;

namespace Multiplayer
{
    class ServerSend
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
        public static void Welcome(Guid toClient)
        {
            using (Packet packet = new Packet((int)ServerPackets.welcome))
            {
                Console.WriteLine(toClient);
                packet.Write(toClient);

                SendTCPData(toClient, packet);
            }
        }

        public static void JoinedRoom(Guid toClient, string lobbyIP, int lobbyPort, int spawnPos)
        {
            using (Packet packet = new Packet((int)ServerPackets.joinedRoom))
            {
                packet.Write(lobbyIP);
                packet.Write(lobbyPort);
                packet.Write(spawnPos);

                SendTCPData(toClient, packet);
            }
        }
        #endregion
    }
}
