using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Multiplayer
{
    public partial class Room
    {
        public Guid RoomId { get; set; }
        public RoomState RoomState { get; set; }
        public List<int> UsedSpawnIds { get; set; }
        public Dictionary<Guid, Client> Clients { get; set; }
        public IPAddress MulticastIP { get; set; }
        public int MulticastPort { get; set; }
        private IPAddress _localIPaddress;

        public UdpClient RoomUdp { get; set; }
        private IPEndPoint _remoteEndPoint;
        private IPEndPoint _localEndPoint;

        private MainThread _roomThread;

        public Room(Guid id, string multicastIP, int multicastPort)
        {
            RoomId = id;
            MulticastIP = IPAddress.Parse(multicastIP);
            MulticastPort = multicastPort + 1;

            _roomThread = new MainThread();
            ThreadManager.AddThread(_roomThread);

            RoomState = RoomState.looking;
            Clients = new Dictionary<Guid, Client>();
            UsedSpawnIds = new List<int>();

            _localIPaddress = IPAddress.Any;

            // Create endpoints
            _remoteEndPoint = new IPEndPoint(MulticastIP, MulticastPort);
            _localEndPoint = new IPEndPoint(_localIPaddress, MulticastPort);

            // Create and configure UdpClient
            RoomUdp = new UdpClient();
            // The following two lines allow multiple clients on the same PC
            RoomUdp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            RoomUdp.ExclusiveAddressUse = false;
            // Bind, Join
            RoomUdp.Client.Bind(_localEndPoint);
            //RoomUdp.JoinMulticastGroup(MulticastIP);

            // Start listening for incoming data
            RoomUdp.BeginReceive(new AsyncCallback(ReceivedCallback), null);

            Console.WriteLine($"New lobby created [{RoomId}]: listenning in {multicastIP}:{multicastPort}");
        }

        #region Communication
        private void ReceivedCallback(IAsyncResult result)
        {
            // Get received data
            IPEndPoint clientEndPoint = new IPEndPoint(0, MulticastPort);
            byte[] data = RoomUdp.EndReceive(result, ref clientEndPoint);
            // Restart listening for udp data packages
            RoomUdp.BeginReceive(new AsyncCallback(ReceivedCallback), null);

            if (data.Length < 4)
                return;

            //Handle Data
            using (Packet packet = new Packet(data))
            {
                int packetLength = packet.ReadInt();
                byte[] packetBytes = packet.ReadBytes(packetLength);

                _roomThread.ExecuteOnMainThread(() =>
                {
                    using (Packet message = new Packet(packetBytes))
                    {
                        int packetId = message.ReadInt();

                        Guid clientId = Guid.Empty;
                        try
                        {
                            clientId = message.ReadGuid();
                        }
                        catch { };

                        if (clientId == Guid.Empty)
                            return;

                        if (Clients[clientId] != null)
                        {
                            Console.WriteLine("{MULTICAST MESSAGE]");
                            RoomHandle.packetHandlers[packetId](RoomId, clientId, message);
                        }
                    }
                });
            }
        }

        public void MulticastUDPData(Packet _packet)
        {
            try
            {
                RoomUdp.Send(_packet.ToArray(), _packet.Length(), _remoteEndPoint);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error multicasting UDP data: {ex}");
            }
        }

        public void SendUDPData(IPEndPoint _clientEndPoint, Packet _packet)
        {
            try
            {
                if (_clientEndPoint != null)
                {
                    RoomUdp.BeginSend(_packet.ToArray(), _packet.Length(), _clientEndPoint, null, null);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending UDP data: {ex}");
            }
        }
        #endregion


        public int AddPlayer(Client _client)
        {
            Console.WriteLine($"Player[{_client.Id}] has joined the Room[{RoomId}]");
            // Add player to the room clients
            Clients.Add(_client.Id, _client);
            // Get room id (user for spawning) and set it to the client
            int spawnId = GetServerPos();
            Console.WriteLine($"Player[{_client.Id}] has joined the Room[{RoomId}]");
            _client.SpawnId = spawnId;
            // Add room id to the used list
            UsedSpawnIds.Add(spawnId);

            // Inform the client that joined the room and it's information
            RoomSend.JoinedRoom(_client.RoomID, _client.Id, MulticastIP.ToString(), MulticastPort, spawnId);

            return spawnId;
        }

        public void Stop()
        {
            //TODO: does not work
            //RoomUdp.DropMulticastGroup(MulticastIP);
            RoomUdp.Close();
            RoomUdp.Dispose();

            ThreadManager.RemoveThread(_roomThread);

            Console.WriteLine($"Room[{RoomId}] has been closed.");
            Server.Rooms.Remove(RoomId);
        }

        public void Reset()
        {
            _roomThread.Clear();

            Clients.Clear();
            UsedSpawnIds.Clear();

            RoomState = RoomState.looking;

            Console.WriteLine($"Room[{RoomId}] has been reset.");
        }

        private int GetServerPos()
        {
            System.Random r = new System.Random();
            int rInt = r.Next(0, 60);

            while (UsedSpawnIds.Contains(rInt))
            {
                rInt = r.Next(0, 60);
            }

            return rInt;
        }
    }

    public enum RoomState { looking, full, playing, closing }
}
