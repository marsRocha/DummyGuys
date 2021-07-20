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

    private MainThread _roomThread;
    public RoomScene roomScene { get; set; }

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

        // Initialize map scene
        LoadMap();

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

                    //verify if the endpoint corresponds to the endpoint that sent the data
                    //this is for security reasons otherwise hackers could inpersonate other clients by send a clientId that does not corresponds to them
                    //without the string conversion even if the endpoint matched it returned false
                    if (Clients[clientId] != null) //TODO: Do I really need to check for this? /////////////////////// CHECK FOR IPENPOINT ASWELL
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
            Debug.Log($"Error multicasting UDP data: {ex}");
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
            Debug.Log($"Error sending UDP data: {ex}");
        }
    }
    #endregion

    #region Methods
    public int AddPlayer(Client _client)
    {
        Debug.Log($"Player[{_client.Id}] has joined the Room[{RoomId}]");
        // Add player to the room clients
        Clients.Add(_client.Id, _client);
        // Get room id (user for spawning) and set it to the client
        int spawnId = GetServerPos();
        Debug.Log($"Player[{_client.Id}] has joined the Room[{RoomId}]");
        _client.SpawnId = spawnId;
        // Add room id to the used list
        UsedSpawnIds.Add(spawnId);

        // Inform the client that joined the room and it's information
        RoomSend.JoinedRoom(_client.RoomID, _client.Id, MulticastIP.ToString(), MulticastPort, spawnId);
        // Send the players already in the room, if any
        RoomSend.PlayersInRoom(RoomId, _client.Id);

        // Finally, tell everyone in the room that the player has joined
        RoomSend.NewPlayer(RoomId, _client.Id, _client.Username, _client.Color, _client.SpawnId);

        /*
        // Check if maximum player capacity has been reached
        if (Clients.Count >= Server.MaxPlayersPerLobby) // TODO: change this
        {
            Thread.Sleep(2000); //TODO: CHECK FOR A BETTER WAY (USED TO GIVE TIME TO THE LATEST PLAYER CONNECTED, SO IT CAN RECEIVE LOADMAP MESSAGE
            RoomState = RoomState.full;
        }*/

        // TODO FOR NOW
        roomScene.SpawnPlayers();
        RoomSend.Map(RoomId, "CourseTest");

        return spawnId;
    }

    public void RemovePlayer(Guid _clientId)
    {
        Console.WriteLine($"Player[{_clientId}] has left the Room[{RoomId}]");

        // Had to put on here because it's not called by a message thus not being locked, this is called by an event
        _roomThread.ExecuteOnMainThread(() =>
        {
            roomScene.players[_clientId].Deactivate();

            RoomSend.RemovePlayer(RoomId, _clientId);

            if (Clients.Count < Server.MaxPlayersPerLobby && RoomState == RoomState.full)
                RoomState = RoomState.looking;

            // TODO: REMOVE THIS FROM HERE
            Reset();
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
        Debug.Log($"Game has started on Room[{RoomId}]");
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

    public void EndGame()
    {
        Debug.Log($"Game has finished on Room[{RoomId}]. Closing room");
        RoomState = RoomState.closing;
        Thread.Sleep(3000);
        RoomSend.EndGame(RoomId);

        Stop();
    }

    public void Stop()
    {
        //TODO: does not work
        //RoomUdp.DropMulticastGroup(MulticastIP);
        RoomUdp.Close();
        RoomUdp.Dispose();

        ThreadManager.RemoveThread(_roomThread);

        roomScene.Stop();
        Debug.Log($"Room[{RoomId}] has been closed.");
        Server.Rooms.Remove(RoomId);
    }

    public void Reset()
    {
        _roomThread.Clear();

        Clients.Clear();
        UsedSpawnIds.Clear();
        roomScene.Reset();

        RoomState = RoomState.looking;

        Debug.Log($"Room[{RoomId}] has been reset.");
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

public enum RoomState { looking, full, playing, closing }