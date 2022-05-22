using System;
using System.Collections.Generic;
using System.Text;

namespace Multiplayer
{
    public class RoomSend
    {
        #region Methods of sending data
        private static void SendTCPData(Guid _roomId, Guid _toClient, Packet _packet)
        {
            _packet.WriteLength();
            Server.Rooms[_roomId].Clients[_toClient].tcp.SendData(_packet);
        }

        private static void SendTCPDataToAll(Guid _roomId, Packet _packet)
        {
            _packet.WriteLength();

            foreach (Client client in Server.Rooms[_roomId].Clients.Values)
            {
                client.tcp.SendData(_packet);
            }
        }

        private static void SendUDPData(Guid _roomId, Guid _toClient, Packet _packet)
        {
            _packet.WriteLength();
            Server.Rooms[_roomId].Clients[_toClient].udp.SendData(_packet);
        }

        private static void SendUDPDataToAll(Guid _roomId, Packet _packet)
        {
            _packet.WriteLength();

            foreach (Client client in Server.Rooms[_roomId].Clients.Values)
            {
                client.udp.SendData(_packet);
            }
        }

        public static void MulticastUDPData(Guid _roomId, Packet _packet)
        {
            _packet.WriteLength();
            Server.Rooms[_roomId].MulticastUDPData(_packet);
        }
        #endregion

        public static void Test()
        {
            foreach(Room r in Server.Rooms.Values)
            {
                foreach (Client c in r.Clients.Values)
                {
                    using (Packet _packet = new Packet((int)ServerPackets.test))
                    {
                        SendUDPData(r.RoomId, c.Id, _packet);
                    }
                }
            }
        }

        public static void JoinedRoom(Guid _roomId, Guid _toClient, string _lobbyIP, int _lobbyPort, int _spawnId)
        {
            using (Packet _packet = new Packet((int)ServerPackets.joinedRoom))
            {
                _packet.Write(_roomId);

                _packet.Write(_lobbyIP);
                _packet.Write(_lobbyPort);
                _packet.Write(_spawnId);

                SendTCPData(_roomId, _toClient, _packet);
            }
        }
    }

}
