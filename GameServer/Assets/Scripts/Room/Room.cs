using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class Room
{
    public Guid RoomId { get; set; }
    public RoomState RoomState { get; set; }
    public List<int> UsedSpawnIds { get; set; }
    public Dictionary<Guid, ClientInfo> ClientsInfo { get; set; }
    public IPAddress MulticastIP { get; set; }
    public int MulticastPort { get; set; }
    private IPAddress _localIPaddress { get; set; }

    public UdpClient RoomUdp { get; set; }
    private IPEndPoint _remoteEndPoint { get; set; }
    private IPEndPoint _localEndPoint { get; set; }

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
        ClientsInfo = new Dictionary<Guid, ClientInfo>();
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

        // TODO: FORNOW
        //Wait x seconds to give time for more players to join in
        //Thread.Sleep(2000);
        //After waiting x time, we now load the map
        //LoadMap();
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
                    if (Server.Clients[clientId].RoomID == RoomId) //TODO: Do I really need to check for this?
                    {
                        RoomHandle.packetHandlers[packetId](RoomId, clientId, message);
                    }
                }
            });
        }
    }

    public void MulticastUDPData(Packet packet)
    {
        packet.WriteLength();
        RoomUdp.Send(packet.ToArray(), packet.Length(), _remoteEndPoint);

        Console.WriteLine($"Multicast sent");
    }
    #endregion

    #region Methods
    public int AddPlayer(Guid _clientId, string _username)
    {
        Console.WriteLine($"Player[{_clientId}] has joined the Room[{RoomId}]");
        int spawnId = GetServerPos();

        ClientInfo newClient = new ClientInfo(_clientId, _username, spawnId);
        ClientsInfo.Add(_clientId, newClient);
        UsedSpawnIds.Add(spawnId);

        RoomSend.NewPlayer(RoomId, newClient);

        if (ClientsInfo.Count >= Server.MaxPlayersPerLobby)
        {
            Thread.Sleep(2000); //TODO: CHECK FOR A BETTER WAY (USED TO GIVE TIME TO THE LATEST PLAYER CONNECTED, SO IT CAN RECEIVE LOADMAP MESSAGE
            RoomState = RoomState.full;
            LoadMap();
        }

        return spawnId;
    }

    public void RemovePlayer(Guid _clientId)
    {
        Console.WriteLine($"Player[{_clientId}] has left the Room[{RoomId}]");
        UsedSpawnIds.Remove(ClientsInfo[_clientId].spawnId);
        ClientsInfo.Remove(_clientId);

        RoomSend.RemovePlayer(RoomId, _clientId);

        if (ClientsInfo.Count < Server.MaxPlayersPerLobby && RoomState == RoomState.full)
            RoomState = RoomState.looking;
    }

    public void LoadMap()
    {
        //Add new room scene
        PhysicsSceneManager.AddSimulation(RoomId, "CourseTest");
    }

    public void InitializeMap(Scene _scene)
    {
        roomScene.Initialize(RoomId, _scene);

        RoomSend.Map(RoomId, "CourseTest");
    }

    public void PlayerReady(Guid _clientId)
    {
        Debug.Log($"Player {_clientId} is ready.");
        ClientsInfo[_clientId].ready = true;

        StartGame();

        /*int count = 0;
        foreach(ClientInfo c in ClientsInfo.Values)
        {
            if (c.ready) count++;
        }

        if (count == ClientsInfo.Count)
            StartGame();*/
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
        ThreadManager.RemoveThread(_roomThread);
        _roomThread = new MainThread();
        ThreadManager.AddThread(_roomThread);

        ClientsInfo.Clear();
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