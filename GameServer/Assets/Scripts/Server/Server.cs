using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

public class Server
{
    public static int MAX_PLAYERS_PER_ROOM = 2;
    public static int MAX_NUMBER_ROOMS = 1;
    public static int TICKRATE = 30;
    public static int ROOM_MIN_PORT = 7778;
    public static bool PLAYER_INTERACTION = true;

    public static int Port { get; private set; }

    public static bool isActive = false;

    public static MainThread MainThread;

    public static Dictionary<Guid, Room> Rooms;

    private static TcpListener tcpListener;

    //Multicast
    private static List<IPAddress> multicastAddressesInUse;
    private static int[] currentInUse;

    /// <summary>Starts the server.</summary>
    /// <param name="_ip">The ip address to start the server on.</param>
    /// <param name="_port">The port to start the server on.</param>
    public static void Start(string _ip, int _port)
    {
        Stop();

        Port = _port;

        MainThread = new MainThread();
        ThreadManager.AddThread(MainThread);

        InitializeData();
        GetMulticastAdresses();
        InitializeRooms();

        tcpListener = new TcpListener(IPAddress.Parse(_ip), Port);
        tcpListener.Start();
        tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

        isActive = true;

        Console.WriteLine($"Server successfuly started, listening on {Port}.");
    }

    /// <summary>Listens for new connections.</summary>
    private static void TCPConnectCallback(IAsyncResult _result)
    {
        TcpClient client = tcpListener.EndAcceptTcpClient(_result);
        // Once it connects we want to still keep on listening for more clients so we call it again
        tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);
        Console.WriteLine($"Incoming connection from {client.Client.RemoteEndPoint}...");

        // Handle new connection
        NewConnection newConnection = new NewConnection(client);
    }

    private static void InitializeData()
    {
        RoomHandle.InitializeData();
        Console.WriteLine("Packets initialized.");
    }

    private static void InitializeRooms()
    {
        Rooms = new Dictionary<Guid, Room>();

        for(int i = 0; i < MAX_NUMBER_ROOMS; i++)
        {
            Guid newGuid = Guid.NewGuid();
            Rooms.Add(newGuid, new Room(newGuid, GetNextAdress(), ROOM_MIN_PORT + Rooms.Count));
        }
        Console.WriteLine("Rooms initialized.");
    }

    // "Matchmaking"
    public static Guid SearchForRoom()
    {
        Room foundRoom = null;

        // Check available rooms looking for players
        foreach (Room room in Rooms.Values)
        {
            if (room.RoomState == RoomState.looking)
            {
                foundRoom = room;
                break;
            }
        }
        if (foundRoom != null)
            return foundRoom.RoomId;

        // Check available dorment rooms
        foreach (Room room in Rooms.Values)
        {
            if (room.RoomState == RoomState.dorment)
            {
                foundRoom = room;
                break;
            }
        }
        if (foundRoom != null)
        {
            foundRoom.Awaken();
            return foundRoom.RoomId;
        }

        return foundRoom != null ? foundRoom.RoomId : Guid.Empty;
    }

    public static void AddClientToRoom(Client _client, Guid _roomId)
    {
        Rooms[_roomId].AddPlayer(_client);
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

    public static void Stop()
    {
        if (isActive)
        {
            tcpListener.Stop();

            MainThread.ExecuteOnMainThread(() =>
            {
                foreach (Room room in Rooms.Values)
                    room.Stop();
            });

            isActive = false;
        }
    }
}
