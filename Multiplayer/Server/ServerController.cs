using Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Text;

namespace Server
{
    public partial class ServerController
    {
        public const int MaxPlayersPerRoom = 8;
        private Dictionary<Guid, Player> _players;
        private UdpClient server;
        int port = 7777;
        private static IPAddress multicastIP = IPAddress.Parse("224.168.100.2");
        private int multicastPort = 11000;
        private IPEndPoint multicastChannel;


        public ServerController()
        {
            _players = new Dictionary<Guid, Player>();
            //GetMulticastIP();
            multicastChannel = new IPEndPoint(multicastIP, multicastPort);
        }

        public void Start()
        {
            Console.WriteLine("Server started - Port: " + port);
            Console.WriteLine("Multicast channel - Ip: " + multicastIP + " Port: " + multicastPort);

            using (server = new UdpClient(port))
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, port);
                while (true)
                {
                    byte[] msg = server.Receive(ref endPoint);
                    string jsonMsg = Encoding.ASCII.GetString(msg);
                    Player player = JsonConvert.DeserializeObject<Player>(jsonMsg);

                    switch (player.GameState)
                    {
                        case GameState.Connecting:
                            Console.WriteLine(player.Name + " is connecting");
                            NewPlayer(player, endPoint);
                            break;
                        case GameState.Sync:
                            Console.WriteLine(player.Name + " is syncing");
                            SyncNewPlayer(player, endPoint);
                            break;
                        case GameState.GameSync:
                            Console.WriteLine(player.Name + " is in sync");
                            SyncData(player);
                            break;
                    }
                }
            }
        }

        public void RefuseConnection(IPEndPoint endPoint)
        {
            string playerJson = JsonConvert.SerializeObject("refused");
            byte[] msg = Encoding.ASCII.GetBytes(playerJson);

            server.Send(msg, msg.Length, endPoint);
        }

        private void GetMulticastIP()
        {
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (adapter.Supports(NetworkInterfaceComponent.IPv4))
                {
                    foreach (IPAddressInformation multi in adapter.GetIPProperties().MulticastAddresses)
                    {
                        if (multi.Address.AddressFamily != AddressFamily.InterNetworkV6)
                        {
                            multicastIP = multi.Address;
                            break;
                        }
                    }
                }
            }
        }
    }
}
