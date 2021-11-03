using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public partial class Room
{
    public Guid RoomId { get; set; }
    public RoomState RoomState { get; set; }

    public MulticastUDP multicastUDP { get; private set; }
    private MainThread _roomThread;

    private readonly int tickrate = ServerData.TICKRATE;
    private int mapIndex;

    public RoomScene roomScene { get; set; }
    public Dictionary<Guid, Client> Clients { get; set; }
    public List<int> UsedSpawnIds { get; set; }

    public Room(Guid _id, string _multicastIP, int _multicastPort)
    {
        RoomId = _id;

        multicastUDP = new MulticastUDP(this, IPAddress.Parse(_multicastIP), _multicastPort);

        _roomThread = new MainThread();
        ThreadManager.AddThread(_roomThread);

        RoomState = RoomState.dormant;
        Clients = new Dictionary<Guid, Client>();
        UsedSpawnIds = new List<int>();
    }

    public void Awaken()
    {
        multicastUDP.StartListening();
        RoomState = RoomState.looking;
    }

    #region Methods
    public int AddPlayer(Client _client)
    {
        Console.WriteLine($"Player[{_client.Id}] has joined the Room[{RoomId}]");
        // Add player to the room clients
        Clients.Add(_client.Id, _client);
        // Get room id (user for spawning) and set it to the client
        int spawnId = GetServerPos();
        _client.SpawnId = spawnId;
        // Add room id to the used list
        UsedSpawnIds.Add(spawnId);

        // Inform the client that joined the room and it's information
        RoomSend.JoinedRoom(_client.RoomID, _client.Id, multicastUDP.Ip.ToString(), multicastUDP.Port, _client.SpawnId, tickrate);
        // Send the players already in the room, if any
        RoomSend.PlayersInRoom(RoomId, _client.Id);

        // Finally, tell everyone in the room that the player has joined
        RoomSend.NewPlayer(RoomId, _client.Id, _client.Username, _client.Color, _client.SpawnId);

        // Check if maximum player capacity has been reached
        if (Clients.Count >= ServerData.MAX_ROOM_PLAYERS)
        {
            RoomState = RoomState.full;
            // Initialize map scene
            LoadMap();
        }

        return spawnId;
    }

    public void RemovePlayer(Guid _clientId)
    {
        // Had to put on here because it's not called by a message thus not being locked, this is called by an event
        _roomThread.ExecuteOnMainThread(() =>
        {
            Console.WriteLine($"Player[{_clientId}] has left the Room[{RoomId}]");
            Clients[_clientId].Disconnect();
            UsedSpawnIds.Remove(Clients[_clientId].SpawnId);
            Clients.Remove(_clientId);

            if (Clients.Count == 0)
            {
                Reset();
                return;
            }
            else if(Clients.Count < ServerData.MAX_ROOM_PLAYERS && RoomState != RoomState.playing)
            {
                RoomState = RoomState.looking;
            }

            RoomSend.PlayerLeft(RoomId, _clientId);
        });
    }

    public void LoadMap()
    {
        if (ServerData.DEBUG) 
        {
            mapIndex = 1; // Equivalates to the test map
        }
        else
        {
            // Chose random map
            mapIndex = 2;
        }

        //Add new room scene
        PhysicsSceneManager.AddSimulation(RoomId, mapIndex);
    }

    public void InitializeMap()
    {
        roomScene.Initialize(RoomId);

        // Used to give time for the players connected
        Thread.Sleep(2000);
        RoomSend.Map(RoomId, mapIndex);
    }

    public void PlayerReady(Guid _clientId)
    {
        Clients[_clientId].ready = true;

        // Check if room can start game
        int count = 0;
        foreach(Client c in Clients.Values)
        {
            if (c.ready) count++;
        }

        if (count == Clients.Count)
            StartGame();
    }

    public void StartGame()
    {
        Console.WriteLine($"Game has started on Room[{RoomId}]");
        RoomState = RoomState.playing;

        if (!ServerData.DEBUG)
            roomScene.StartCountdown();
        else // if is on debug, go straight into game
            roomScene.StartRace();

        RoomSend.StartGame(RoomId);
    }

    public void PlayerFinish(Guid _clientId, int _simulationFrame)
    {
        //TODO: Check if is correct

        roomScene.FinishRacePlayer(_clientId);
        RoomSend.PlayerFinish(RoomId, _clientId);
    }

    public void PlayerGrab(Guid _grabber, int _tick)
    {
        roomScene.PlayerGrab(_grabber, _tick);        
    }

    public void PlayerLetGo(Guid _grabber, Guid _grabbed)
    {
        roomScene.PlayerLetGo(_grabber, _grabbed);
    }

    public void PlayerPush(Guid _pusher, int _tick)
    {
        roomScene.PlayerPush(_pusher, _tick);
    }

    public void EndGame()
    {
        Console.WriteLine($"Game has finished on Room[{RoomId}]");
        RoomState = RoomState.closing;
        RoomSend.EndGame(RoomId);
    }

    public void Stop()
    {
        if(RoomState != RoomState.closing)
            RoomSend.Disconnected(RoomId);

        ThreadManager.RemoveThread(_roomThread);
        if (roomScene)
            roomScene.Stop();
        multicastUDP.Close();
        Clients.Clear();

        Console.WriteLine($"Room[{RoomId}] has been closed.");
    }

    public void Reset()
    {
        if (roomScene)
            roomScene.Stop();
        _roomThread.Clear();
        multicastUDP.Close();

        foreach (Client client in Clients.Values)
            client.Disconnect();
        Clients.Clear();
        UsedSpawnIds.Clear();

        RoomState = RoomState.dormant;

        Console.WriteLine($"Room[{RoomId}] is dorment.");
    }
    #endregion

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

    public class MulticastUDP
    {
        private Room _room;
        public IPAddress Ip { get; private set; }
        public int Port { get; private set; }

        private IPAddress _localIPaddress;
        public UdpClient RoomUdp;
        private IPEndPoint _remoteEndPoint;
        private IPEndPoint _localEndPoint;

        public MulticastUDP(Room _room, IPAddress _multicastIP, int _multicastPort)
        {
            this._room = _room;
            Ip = _multicastIP;
            Port = _multicastPort;
        }

        public void StartListening()
        {
            _localIPaddress = IPAddress.Any;

            // Create endpoints
            _remoteEndPoint = new IPEndPoint(Ip, Port);
            _localEndPoint = new IPEndPoint(_localIPaddress, Port);
            // Create and configure UdpClient
            RoomUdp = new UdpClient();
            // The following two lines allow multiple clients on the same PC
            RoomUdp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            RoomUdp.ExclusiveAddressUse = false;
            // Bind, Join
            RoomUdp.Client.Bind(_localEndPoint);
            RoomUdp.JoinMulticastGroup(Ip);

            // Start listening for incoming data
            RoomUdp.BeginReceive(new AsyncCallback(ReceiveCallback), null);

            Console.WriteLine($"{_room.RoomId}] now listenning on {Ip}:{Port}");
        }

        private void ReceiveCallback(IAsyncResult _result)
        {
            // Get received data
            IPEndPoint _clientEndPoint = new IPEndPoint(IPAddress.Any, Port);
            byte[] data = RoomUdp.EndReceive(_result, ref _clientEndPoint);
            // Restart listening for udp data packages
            RoomUdp.BeginReceive(new AsyncCallback(ReceiveCallback), null);

            if (data.Length < 4)
                return;

            //Handle Data
            using (Packet packet = new Packet(data))
            {
                int packetLength = packet.ReadInt();
                byte[] packetBytes = packet.ReadBytes(packetLength);

                _room._roomThread.ExecuteOnMainThread(() =>
                {
                    // Handle Message Data
                    using (Packet message = new Packet(packetBytes))
                    {
                        int packetId = message.ReadInt();

                        Guid clientId = Guid.Empty;
                        try
                        {
                            clientId = message.ReadGuid();
                        }
                        catch { };

                        if (clientId == Guid.Empty || clientId == _room.RoomId)
                            return;

                        if (_room.Clients[clientId].udp.endPoint == null)
                        {
                            // If this is a new connection
                            _room.Clients[clientId].udp.Connect(_clientEndPoint);
                            return;
                        }

                        //verify if the endpoint corresponds to the endpoint that sent the data
                        //this is for security reasons otherwise hackers could inpersonate other clients by send a clientId that does not corresponds to them
                        //without the string conversion even if the endpoint matched it returned false
                        if (_room.Clients[clientId] != null) // && Clients[clientId].udp.endPoint.ToString() == _clientEndPoint.ToString())
                        {
                            RoomHandle.packetHandlers[packetId](_room.RoomId, clientId, message);
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
            catch
            {
                //Console.WriteLine($"Error multicasting UDP data: {ex}");
            }
        }

        public void SendUDPData(IPEndPoint _clientEndPoint, Packet _packet)
        {
            try
            {
                if (_clientEndPoint != null)
                {
                    RoomUdp.Send(_packet.ToArray(), _packet.Length(), _clientEndPoint);
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"Error sending UDP data: {ex}");
            }
        }

        public void Close()
        {
            RoomUdp.DropMulticastGroup(Ip);
            RoomUdp.Close();
            RoomUdp.Dispose();
        }
    }
}

public enum RoomState { dormant, looking, full, playing, closing }