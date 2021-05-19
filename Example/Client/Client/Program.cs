using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Client
{
    class Program
    {
        public delegate void PacketHandler(Packet packet);
        public static Dictionary<int, PacketHandler> packetHandlers;
        static UdpClient _udpclient;
        static int _roomPort = 7778;
        static IPAddress _multicastIPaddress = IPAddress.Parse("233.0.0.2");
        static IPAddress _localIPaddress;
        static IPEndPoint _localEndPoint;
        static IPEndPoint _remoteEndPoint;

        static void Main(string[] args)
        {
            packetHandlers = new Dictionary<int, PacketHandler>()
            {
                { (int)Packets.test, WelcomeReceived },
            };

            _localIPaddress = IPAddress.Any;

            // Create endpoints
            _remoteEndPoint = new IPEndPoint(_multicastIPaddress, _roomPort);
            _localEndPoint = new IPEndPoint(_localIPaddress, _roomPort);

            // Create and configure UdpClient
            _udpclient = new UdpClient();
            // The following three lines allow multiple clients on the same PC
            _udpclient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpclient.ExclusiveAddressUse = false;
            // Bind, Join
            _udpclient.Client.Bind(_localEndPoint);
            _udpclient.JoinMulticastGroup(_multicastIPaddress);

            // Start listening for incoming data
            _udpclient.BeginReceive(new AsyncCallback(ReceivedCallback), null);

            Console.WriteLine("[Client console initiated]\n-Press 'Any Key' to send a message\n-Press 'Enter' to exit\n");

            do
            {
                //Send to server
                SendUDP();

                //Multicast to clients
                SendUDPMulticast();
            }
            while (Console.ReadKey(true).Key != ConsoleKey.Enter);
        }

        public static void SendUDPMulticast()
        {
            using (Packet packet = new Packet((int)Packets.test))
            {
                packet.Write("Multicast from client.");

                packet.WriteLength();
                _udpclient.Send(packet.ToArray(), packet.Length(), _remoteEndPoint);
            }
            Console.WriteLine($"Multicast sent");
        }

        public static void SendUDP()
        {
            using (Packet packet = new Packet((int)Packets.test))
            {
                packet.Write("Udp from client.");

                packet.WriteLength();
                _udpclient.Send(packet.ToArray(), packet.Length(), new IPEndPoint(IPAddress.Loopback, _roomPort));
            }
            Console.WriteLine($"Udp sent");
        }

        /// <summary>
        /// Callback which is called when UDP packet is received
        /// </summary>
        /// <param name="ar"></param>
        private static void ReceivedCallback(IAsyncResult ar)
        {
            // Get received data
            IPEndPoint sender = new IPEndPoint(0, _roomPort);
            Byte[] data = _udpclient.EndReceive(ar, ref sender);

            if (data.Length < 4)
                return;

            //Handle Data
            using (Packet packet = new Packet(data))
            {
                int packetLength = packet.ReadInt();
                byte[] packetBytes = packet.ReadBytes(packetLength);

                using (Packet message = new Packet(packetBytes))
                {
                    int packetId = message.ReadInt();
                    packetHandlers[packetId](message);
                }
            }

            // Restart listening for udp data packages
            _udpclient.BeginReceive(new AsyncCallback(ReceivedCallback), null);
        }

        public static void WelcomeReceived(Packet packet)
        {
            string message = packet.ReadString();

            Console.WriteLine($"Got message: {message}");
        }
    }
}