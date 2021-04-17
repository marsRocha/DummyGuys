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
        public delegate void PacketHandler(Guid idFromClient, Packet packet);
        public static Dictionary<int, PacketHandler> packetHandlers;
        public static Dictionary<Guid, Room> Rooms;

        private static TcpListener tcpListener;
        private static UdpClient udpListener;

        //Multicast
        private static List<IPAddress> multicastAddressesInUse;
        public static int multicastPort = 7778;
        private static int[] currentInUse;

        public static void Start(int port)
        {
            Port = port;
            Console.WriteLine($"Server started on {Port}.");
            
            InitializeData();

            currentInUse = new int[4];
            currentInUse[0] = 233;
            currentInUse[1] = 0;
            currentInUse[2] = 0;
            currentInUse[3] = 0;
            GetMulticastAdresses();

            tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), Port);
            tcpListener.Start();
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

            udpListener = new UdpClient(Port);
            udpListener.BeginReceive(UDPReceiveCallback, null);

        }

        #region Callbacks

        /// <summary>
        /// Listens for connections and stores the connected clients
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

            //Console.WriteLine($"{client.Client.RemoteEndPoint} failed to connect: Server full.");
        }

        private static void UDPReceiveCallback(IAsyncResult result)
        {
            try
            {
                IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, Port);
                byte[] data = udpListener.EndReceive(result, ref clientEndPoint);
                udpListener.BeginReceive(UDPReceiveCallback, null);

                if (data.Length < 4)
                {
                    return;
                }

                using (Packet packet = new Packet(data))
                {
                    Guid clientId = packet.ReadGuid();

                    if (clientId == null)
                        return;

                    if (Clients[clientId].udp.endPoint == null)
                    {
                        Clients[clientId].udp.Connect(clientEndPoint);
                        return;
                    }

                    //verifiy if the endpoint corresponds to the endpoint that sent the data
                    //this is for security reasons otherwise hackers could inpersonate other clients by send a clientId that does not corresponds to them
                    //without the string conversion even if the endpoint matched it returned false
                    if (Clients[clientId].udp.endPoint.Equals(clientEndPoint))
                    {
                        Clients[clientId].udp.HandleData(packet);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving UDP data: {ex}");
            }
        }
        #endregion

        public static void SendUDPData(IPEndPoint clientEndPoint, Packet packet)
        {
            try
            {
                if (clientEndPoint != null)
                {
                    udpListener.BeginSend(packet.ToArray(), packet.Length(), clientEndPoint, null, null);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending UDP data: {ex}");
            }
        }

        private static void InitializeData()
        {
            Clients = new Dictionary<Guid, Client>();
            Rooms = new Dictionary<Guid, Room>();

            packetHandlers = new Dictionary<int, PacketHandler>()
            {
                { (int)ClientPackets.welcomeReceived, ServerHandle.WelcomeReceived },
            };
            Console.WriteLine("Packets initialized.");
        }

        private static void GetMulticastAdresses()
        {
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