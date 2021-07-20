using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using UnityEngine;

public class Server
{
    public const int MaxPlayersPerLobby = 2;
    public const int MaxNumberOfRooms = 1;
    public static int Port { get; private set; }

    public static bool isActive = false;
    public static int tickrate = 30;

    public static MainThread MainThread;

    public static Dictionary<Guid, Room> Rooms;

    private static TcpListener tcpListener;

    //Multicast
    private static List<IPAddress> multicastAddressesInUse;
    public static int multicastPort = 7778;
    private static int[] currentInUse;

    public static void Start(int port)
    {
        Stop();

        Port = port;

        MainThread = new MainThread();
        ThreadManager.AddThread(MainThread);

        InitializeData();
        GetMulticastAdresses();
        InitializeRooms();

        tcpListener = new TcpListener(IPAddress.Any, Port);
        tcpListener.Start();
        tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

        isActive = true;

        Debug.Log($"Server successfuly started, listening on {Port}.");
    }

    /// <summary>
    /// Listens for new connections
    /// </summary>
    private static void TCPConnectCallback(IAsyncResult result)
    {
        TcpClient client = tcpListener.EndAcceptTcpClient(result);
        // Once it connects we want to still keep on listening for more clients so we call it again
        tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);
        Debug.Log($"Incoming connection from {client.Client.RemoteEndPoint}...");

        // Handle new connection
        NewConnection newConnection = new NewConnection(client);
    }

    private static void InitializeData()
    {
        RoomHandle.InitializeData();
        Debug.Log("Packets initialized.");
    }

    private static void InitializeRooms()
    {
        Rooms = new Dictionary<Guid, Room>();

        for(int i = 0; i < MaxNumberOfRooms; i++)
        {
            Guid newGuid = Guid.NewGuid();
            Rooms.Add(newGuid, new Room(newGuid, GetNextAdress(), multicastPort));
        }
        Debug.Log("Rooms initialized.");
    }

    // "Matchmaking"
    public static Guid SearchForRoom()
    {
        Room foundRoom = null;

        if (Rooms.Count > 0)
        {
            foreach (Room room in Rooms.Values)
            {
                if (room.RoomState == RoomState.looking)
                {
                    foundRoom = room;
                    break;
                }
            }
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
        Debug.Log("Multicast addresses in use:");
        foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (adapter.Supports(NetworkInterfaceComponent.IPv4))
            {
                foreach (IPAddressInformation multi in adapter.GetIPProperties().MulticastAddresses)
                {
                    if (multi.Address.AddressFamily != AddressFamily.InterNetworkV6)
                    {
                        Debug.Log("    " + multi.Address);
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
