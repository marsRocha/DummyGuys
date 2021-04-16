using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Multiplayer
{
    public class Room
    {
        public Guid Id { get; set; }
        public RoomState RoomState { get; set; }
        public Dictionary<int, Player> Players { get; set; }

        public string MulticastIP { get; set; }
        public int MulticastPort { get; set; }
        
        public UdpClient RoomUDP { get; set; }
        public IPEndPoint EndPoint { get; set; }

        public Room(Guid id, string multicastIP, int multicastPort)
        {
            Id = id;
            MulticastIP = multicastIP;
            MulticastPort = multicastPort;

            RoomState = RoomState.looking;
            Players = new Dictionary<int, Player>();

            RoomUDP = new UdpClient();
            EndPoint = new IPEndPoint(IPAddress.Parse(multicastIP), multicastPort);
            RoomUDP.JoinMulticastGroup(EndPoint.Address);

            Console.WriteLine($"New lobby created [{Id}]: listenning in {multicastIP}:{multicastPort}");
        }

        public void AddPlayer(int id, string username, string peerIP, string peerPort)
        {
            Console.WriteLine($"Player[{id}] has joined the Room[{Id}]");
            Players.Add(id, new Player(id, username));

            using (Packet packet = new Packet((int)ServerPackets.playerJoined))
            {
                packet.Write(peerIP);
                packet.Write(peerPort);

                MulticastUDPData(packet);
            }
        }
        

        public void RemovePlayer(int id)
        {
            Console.WriteLine($"Player[{id}] has left the Room[{Id}]");
            Players.Remove(id);

            using (Packet packet = new Packet((int)ServerPackets.playerLeft))
            {
                packet.Write(id);

                MulticastUDPData(packet);
            }
        }


        private void MulticastUDPData(Packet packet)
        {
            packet.WriteLength();
            RoomUDP.Send(packet.ToArray(), packet.Length(), EndPoint);
        }
    }

    public enum RoomState { looking, full, playing }
}
