using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using UnityEngine;

public class Server
{
    public static string Address { get; private set; }
    public static int Port { get; private set; }

    public static bool isActive = false;

    public static MessageQueue MainThread;

    public static Dictionary<int, Room> Rooms;

    private static TCP tcp;

    //Multicast
    private static List<IPAddress> multicastAddressesInUse;
    private static int[] currentInUse;

    /// <summary>Starts the server.</summary>
    /// <param name="_ip">The ip address to start the server on.</param>
    /// <param name="_port">The port to start the server on.</param>
    public static void Start()
    {
        Stop();

        Address = ServerData.IP;
        Port = ServerData.PORT;

        MainThread = new MessageQueue();
        MessageQueuer.AddQueue(MainThread);

        InitializeData();
        GetMulticastAdresses();
        InitializeRooms();

        tcp = new TCP();
        tcp.Connect(Address, Port);

        isActive = true;
    }

    private static void InitializeData()
    {
        RoomHandle.InitializeData();
        Console.WriteLine("Packets initialized.");
    }

    private static void InitializeRooms()
    {
        Rooms = new Dictionary<int, Room>();

        for(int i = 0; i < ServerData.MAX_ROOMS; i++)
        {
            Rooms.Add(-(i + 1), new Room(-(i + 1), GetNextAdress(), ServerData.ROOM_MIN_PORT + Rooms.Count));
        }
        Console.WriteLine("Room(s) Initialized.");
    }

    // "Matchmaking"
    public static int SearchForRoom()
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
            if (room.RoomState == RoomState.dormant)
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

        return foundRoom != null ? foundRoom.RoomId : 0; // 0 means it didn't found any room
    }

    public static void AddClientToRoom(Client _client, int _roomId)
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
            tcp.Stop();

            MainThread.ExecuteOnMain(() =>
            {
                foreach (Room room in Rooms.Values)
                    room.Stop();
            });

            Rooms.Clear();

            isActive = false;
        }
    }

    private class TCP
    {
        private TcpListener tcpListener;

        public void Connect(string _ip, int _port)
        {
            Stop();

            tcpListener = new TcpListener(IPAddress.Parse(_ip), Port);
            tcpListener.Start();
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(ConnectCallback), null);

            Console.WriteLine($"Server successfuly started, listening on {Port}.");
        }

        /// <summary>Listens for new connections.</summary>
        private void ConnectCallback(IAsyncResult _result)
        {
            TcpClient client = tcpListener.EndAcceptTcpClient(_result);
            // Once it connects we want to still keep on listening for more clients so we call it again
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(ConnectCallback), null);
            Console.WriteLine($"Incoming connection from {client.Client.RemoteEndPoint}...");

            // Handle new connection
            NewConnection newConnection = new NewConnection(client);
        }

        public void Stop()
        {
            if(tcpListener != null)
                tcpListener.Stop();
        }
    }
}
