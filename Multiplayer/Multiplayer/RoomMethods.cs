using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Multiplayer
{
    public partial class Room
    {
        #region Game Related
        public void StartGame()
        {
            using (Packet packet = new Packet((int)ServerPackets.startGame))
            {
                MulticastUDPData(packet);
            }
            RoomState = RoomState.playing;

            Console.WriteLine($"Game has started on Room[{Id}]");
        }
        #endregion

        #region Player Related
        public void AddPlayer(Guid id, string username, string peerIP, string peerPort)
        {
            Console.WriteLine($"Player[{id}] has joined the Room[{Id}]");
            Player p = new Player(id, username);
            Players.Add(id, p);
            Server.Clients[id].SetPlayer(p);

            using (Packet packet = new Packet((int)ServerPackets.playerJoined))
            {
                packet.Write(id);
                packet.Write(username);
                packet.Write(peerIP);
                packet.Write(peerPort);

                MulticastUDPData(packet);
            }

            if (Players.Count >= Server.MaxPlayersPerLobby)
                RoomState = RoomState.full;
        }

        public void RemovePlayer(Guid id)
        {
            Console.WriteLine($"Player[{id}] has left the Room[{Id}]");
            Players.Remove(id);

            using (Packet packet = new Packet((int)ServerPackets.playerLeft))
            {
                packet.Write(id);

                MulticastUDPData(packet);
            }

            if (Players.Count < Server.MaxPlayersPerLobby && RoomState == RoomState.full)
                RoomState = RoomState.looking;
        }

        public void CorrectPlayer(Guid id, Vector3 Position)
        {
            using (Packet packet = new Packet((int)ServerPackets.playerCorrection))
            {
                packet.Write(id);

                MulticastUDPData(packet);
            }
        }
        #endregion
    }
}
