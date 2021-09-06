using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server
{
    public class Server
    {
        public delegate void PacketHandler(Packet packet);
        public static Dictionary<int, PacketHandler> packetHandlers;
        
        //Multicast
        static int _roomPort = 7778;
        static IPAddress _multicastIPaddress = IPAddress.Parse("233.0.0.2");

        static IPAddress _localIPaddress;
        static IPEndPoint _localEndPoint;
        static IPEndPoint _remoteEndPoint;
        // TCP
        private static TcpListener tcpListener;
        //UDP
        static UdpClient _udpclient;

        public static List<Client> Clients;

        public static void Start()
        {
            // Packets
            packetHandlers = new Dictionary<int, PacketHandler>()
            {
                { (int)Packets.test, MessageReceived },
                { (int)Packets.connect, Connect },
            };

            Clients = new List<Client>();

            // TCP
            tcpListener = new TcpListener(IPAddress.Any, 7777);
            tcpListener.Start();
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);


            // UDP
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
        }

        #region TCP
        private static void TCPConnectCallback(IAsyncResult result)
        {
            TcpClient client = tcpListener.EndAcceptTcpClient(result);
            // Once it connects we want to still keep on listening for more clients so we call it again
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);
            Console.WriteLine($"Incoming connection from {client.Client.RemoteEndPoint}...");

            Clients.Add(new Client(client));
        }

        public static void SendTcp()
        {
            foreach(Client c in Clients)
            {
                using (Packet packet = new Packet((int)Packets.test))
                {
                    packet.Write("Tcp from server.");

                    packet.WriteLength();
                    c.tcp.SendData(packet);
                }
            }
            Console.WriteLine($"TCP sent");
        }

        #endregion

        #region UDP
        private static void ReceivedCallback(IAsyncResult ar)
        {
            // Get received data
            IPEndPoint sender = new IPEndPoint(IPAddress.Any, _roomPort);
            byte[] data = _udpclient.EndReceive(ar, ref sender);
            // Restart listening for udp data packages
            _udpclient.BeginReceive(new AsyncCallback(ReceivedCallback), null);

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

                    if (Clients[0].udp.endPoint == null)
                    {
                        Console.WriteLine($"{sender}");
                        // If this is a new connection
                        Clients[0].udp.Connect(sender);

                        return;
                    }


                    packetHandlers[packetId](message);
                }
            }
        }

        public static void SendMulticast()
        {
            using (Packet packet = new Packet((int)Packets.test))
            {
                packet.Write("Multicast from server.");

                packet.WriteLength();
                _udpclient.Send(packet.ToArray(), packet.Length(), _remoteEndPoint);
            }
            Console.WriteLine($"Multicast sent");
        }

        public static void SendUdp()
        {
            foreach (Client c in Clients)
            {
                c.udp.SendData();
            }
            Console.WriteLine($"UDP sent");
        }

        public static void SendUdp(IPEndPoint _clientEndPoint)
        {
            try
            {
                if (_clientEndPoint != null)
                {
                    using (Packet packet = new Packet((int)Packets.test))
                    {
                        packet.Write("UDP from server.");

                        packet.WriteLength();
                        _udpclient.Send(packet.ToArray(), packet.Length(), _clientEndPoint);
                    }
                }
            }
            catch (Exception _ex)
            {
                Console.Write($"Error sending data to {_clientEndPoint} via UDP: {_ex}");
            }
        }
        #endregion

        public static void MessageReceived(Packet packet)
        {
            string message = packet.ReadString();

            Console.WriteLine($"Got message: {message}");
        }

        public static void SendWelcome()
        {
            foreach (Client c in Clients)
            {
                using (Packet packet = new Packet((int)Packets.welcome))
                {
                    packet.WriteLength();
                    c.tcp.SendData(packet);
                }
            }
        }

        public static void Connect(Packet packet)
        {
            SendWelcome();
        }
    }
}
