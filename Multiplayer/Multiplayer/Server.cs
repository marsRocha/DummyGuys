using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

namespace Multiplayer
{
    class Server
    {
        public const int MaxPlayersPerLobby = 4;
        public static int Port { get; private set; }
        public static Dictionary<Guid, Client> Clients;
        public delegate void PacketHandler(Guid ClientId, Packet packet);
        public static Dictionary<int, PacketHandler> packetHandlers;
        public static Dictionary<Guid, Room> Rooms;

        private static TcpListener tcpListener;

        //Multicast
        private static List<IPAddress> multicastAddressesInUse;
        public static int multicastPort = 7778;
        private static int[] currentInUse;

        public static void Start(int port)
        {
            Port = port;
            Console.WriteLine($"Server started on {Port}.");
            
            InitializeData();
            GetMulticastAdresses();

            tcpListener = new TcpListener(IPAddress.Any, Port);
            tcpListener.Start();
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);
        }

        /// <summary>
        /// Listens for new connections
        /// </summary>
        private static void TCPConnectCallback(IAsyncResult result)
        {
            TcpClient client = tcpListener.EndAcceptTcpClient(result);
            //Once it connects we want to still keep on listening for more clients so we call it again
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);
            Console.WriteLine($"Incoming connection from {client.Client.RemoteEndPoint}...");

            //Add and Connect to Client
            Guid pId = Guid.NewGuid();
            Clients.Add(pId, new Client(pId));
            Clients[pId].tcp.Connect(client);
        }

        private static void InitializeData()
        {
            Clients = new Dictionary<Guid, Client>();
            Rooms = new Dictionary<Guid, Room>();

            packetHandlers = new Dictionary<int, PacketHandler>()
            {
                { (int)ClientPackets.welcomeReceived, ServerHandle.WelcomeReceived },
                { (int)ClientPackets.startGame, ServerHandle.StarGame },
                { (int)ClientPackets.playerMovement, ServerHandle.PlayerMovement },
                { (int)ClientPackets.test, ServerHandle.Test },
            };
            Console.WriteLine("Packets initialized.");
        }

        private static void GetMulticastAdresses()
        {
            currentInUse = new int[4];
            currentInUse[0] = 233;
            currentInUse[1] = 0;
            currentInUse[2] = 0;
            currentInUse[3] = 0;
            multicastAddressesInUse = new List<IPAddress>();
            Console.WriteLine("Multicast addresses in use:");
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (adapter.Supports(NetworkInterfaceComponent.IPv4))
                {
                    foreach (IPAddressInformation multi in adapter.GetIPProperties().MulticastAddresses)
                    {
                        if (multi.Address.AddressFamily != AddressFamily.InterNetworkV6)
                        {
                            Console.WriteLine("    " + multi.Address);
                            multicastAddressesInUse.Add(multi.Address);
                        }
                    }
                }
            }
        }

        public static string GetNextAdress()
        {
            string ipAddress;
            do
            {
                currentInUse[3]++;
                if (currentInUse[3] >= 256)
                {
                    currentInUse[3] = 0;
                    currentInUse[2]++;
                    if (currentInUse[2] >= 256)
                    {
                        currentInUse[2] = 0;
                        currentInUse[1]++;
                        if (currentInUse[1] >= 256)
                        {
                            currentInUse[1] = 0;
                        }
                    }
                }
                ipAddress = $"{currentInUse[0]}.{currentInUse[1]}.{currentInUse[2]}.{currentInUse[3]}";
            } while (multicastAddressesInUse.Contains(IPAddress.Parse(ipAddress)));

            //Does port need to change too?

            return ipAddress;
        }
    }
}