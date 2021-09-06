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
    public List<int> UsedSpawnIds { get; set; }
    public Dictionary<Guid, Client> Clients { get; set; }
    public IPAddress MulticastIP { get; set; }
    public int MulticastPort { get; set; }
    private IPAddress _localIPaddress;

    public UdpClient RoomUdp { get; set; }
    private IPEndPoint _remoteEndPoint;
    private IPEndPoint _localEndPoint;

    protected MainThread _roomThread;
    public RoomScene roomScene { get; set; }

    private readonly int tickrate = 30;

    public Room(Guid _id, string _multicastIP, int _multicastPort)
    {
        RoomId = _id;
        MulticastIP = IPAddress.Parse(_multicastIP);
        MulticastPort = _multicastPort;

        _roomThread = new MainThread();
        ThreadManager.AddThread(_roomThread);

        RoomState = RoomState.dorment;
        Clients = new Dictionary<Guid, Client>();
        UsedSpawnIds = new List<int>();
    }

    public void Awaken()
    {
        ConnectUDP();
        RoomState = RoomState.looking;
    }

    #region Communication
    private void ConnectUDP()
    {
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
        RoomUdp.JoinMulticastGroup(MulticastIP);

        // Start listening for incoming data
        RoomUdp.BeginReceive(new AsyncCallback(ReceiveCallback), null);

        Console.WriteLine($"{RoomId}] now listenning on {MulticastIP}:{MulticastPort}");
    }

    private void ReceiveCallback(IAsyncResult _result)
    {
        // Get received data
        IPEndPoint _clientEndPoint = new IPEndPoint(IPAddress.Any, MulticastPort);
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

            _roomThread.ExecuteOnMainThread(() =>
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

                    if (clientId == Guid.Empty || clientId  == RoomId)
                        return;

                    if (Clients[clientId].udp.endPoint == null)
                    {
                        // If this is a new connection
                        Clients[clientId].udp.Connect(_clientEndPoint);
                        return;
                    }

                    //verify if the endpoint corresponds to the endpoint that sent the data
                    //this is for security reasons otherwise hackers could inpersonate other clients by send a clientId that does not corresponds to them
                    //without the string conversion even if the endpoint matched it returned false
                    if (Clients[clientId] != null) // && Clients[clientId].udp.endPoint.ToString() == _clientEndPoint.ToString())
                    {
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
                RoomUdp.Send(_packet.ToArray(), _packet.Length(), _clientEndPoint);
            }
        }
        catch (Exception ex)
        {
            Debug.Log($"Error sending UDP data: {ex}");
        }
    }
    #endregion

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
        RoomSend.JoinedRoom(_client.RoomID, _client.Id, MulticastIP.ToString(), MulticastPort, _client.SpawnId, tickrate);
        // Send the players already in the room, if any
        RoomSend.PlayersInRoom(RoomId, _client.Id);

        // Finally, tell everyone in the room that the player has joined
        RoomSend.NewPlayer(RoomId, _client.Id, _client.Username, _client.Color, _client.SpawnId);

        // Check if maximum player capacity has been reached
        if (Clients.Count >= 2) //Server.MaxPlayersPerLobby) // TODO: change this
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
            else if(Clients.Count < Server.MAX_PLAYERS_PER_ROOM && RoomState != RoomState.playing)
            {
                RoomState = RoomState.looking;
            }

            RoomSend.PlayerLeft(RoomId, _clientId);
        });
    }

    public void LoadMap()
    {
        //Add new room scene
        PhysicsSceneManager.AddSimulation(RoomId, "CourseTest");
    }

    public void InitializeMap()
    {
        roomScene.Initialize(RoomId);

        // Used to give time for the players connected
        Thread.Sleep(2000);
        RoomSend.Map(RoomId, "CourseTest");
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
        roomScene.StartRace();
        RoomState = RoomState.playing;

        RoomSend.StartGame(RoomId);
    }

    public void PlayerFinish(Guid _clientId, int _simulationFrame)
    {
        //TODO: Check if is correct

        roomScene.FinishRacePlayer(_clientId);
        RoomSend.PlayerFinish(RoomId, _clientId);
    }

    public void PlayerGrab(Guid _grabber, Guid _grabbed, int _tick)
    {
        //roomScene.PlayerGrab(_from, _to, _tick);
        RoomSend.PlayerGrab(RoomId, _grabber, _grabbed);
    }

    public void PlayerLetGo(Guid _grabber, Guid _grabbed, int _tick)
    {
        RoomSend.PlayerLetGo(RoomId, _grabber, _grabbed);
    }

    public void PlayerPush(Guid _pusher, Guid _pushed, int _tick)
    {
        RoomSend.PlayerPush(RoomId, _pusher, _pushed);
    }

    public void EndGame()
    {
        Console.WriteLine($"Game has finished on Room[{RoomId}]. Closing room");
        RoomState = RoomState.closing;
        Thread.Sleep(3000);
        RoomSend.EndGame(RoomId);

        Reset();
    }

    public void Stop()
    {
        if(RoomState != RoomState.closing)
            RoomSend.Disconnected(RoomId);

        RoomUdp.DropMulticastGroup(MulticastIP);
        RoomUdp.Close();
        RoomUdp.Dispose();

        Clients.Clear();

        ThreadManager.RemoveThread(_roomThread);

        roomScene.Stop();
        Console.WriteLine($"Room[{RoomId}] has been closed.");
        Server.Rooms.Remove(RoomId);
    }

    public void Reset()
    {
        RoomUdp.DropMulticastGroup(MulticastIP);
        RoomUdp.Close();
        RoomUdp.Dispose();

        _roomThread.Clear();

        Clients.Clear();
        UsedSpawnIds.Clear();

        if(roomScene)
            roomScene.Stop();

        RoomState = RoomState.dorment;

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
}

public enum RoomState { dorment, looking, full, playing, closing }