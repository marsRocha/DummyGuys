using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;
using System.Security.Cryptography;
using System.Net.Http.Headers;
using System.Threading;

public class Client : MonoBehaviour
{
    public static Client instance;
    public static int dataBufferSize = 4096;
    public static int MaxPlayers { get; private set; } = 10;

    public delegate void PacketHandler(Guid id, Packet packet);
    public static Dictionary<int, PacketHandler> packetHandlers;
    public static Dictionary<Guid, NewConnection> newConnections = new Dictionary<Guid, NewConnection>();
    public static Dictionary<Guid, Peer> peers = new Dictionary<Guid, Peer>();

    private static TcpListener tcpListener;
    private static UdpClient _udpClient;

    //Server Info
    private static IPAddress _serverIPaddress;
    private static int _serverPort = 26950;
    public TCP server { get; private set; }

    //Room Info
    private static IPAddress _roomIPaddress;
    private static int _roomPort;
    private static IPEndPoint _localEndPoint; // Client IP, Client Port
    private static IPEndPoint _roomMulticastEndPoint; // Room IP, Room Port - to send multicast messages
    private static IPEndPoint _remoteEndPoint; // Any IP, Room Port - to bind to and receive multicast from whomever sends them

    //Client Info
    private static IPAddress _localIPaddress;
    private static int _localPort;

    public ClientInfo clientInfo;
    private bool isConnected = false;

    public int clientExeID;

    #region Singleton
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(this);
        }
    }
    #endregion

    public void GoOnline(string _ip, int _port)
    {
        _serverIPaddress = IPAddress.Parse(_ip);
        _serverPort = _port;

        _localIPaddress = IPAddress.Any;
        _localPort = 5000 + clientExeID; // Depending on the id set on inspector, set a different ip and port to listen to peers

        InitializeData();
        ConnectToServer();

        //Listen to peers
        tcpListener = new TcpListener(_localIPaddress, _localPort);
        tcpListener.Start();
        tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

        // Create local endpoint
        _localEndPoint = new IPEndPoint(_localIPaddress, _localPort);

        Debug.Log($"Client started listening[TCP only] on {_localPort}");
    }

    private void ConnectToServer()
    {
        server = new TCP();

        isConnected = true;
        server.Connect(_serverIPaddress, _serverPort);
    }

    #region Callbacks
    //Incoming Peer requests to connect
    private static void TCPConnectCallback(IAsyncResult result)
    {
        TcpClient client = tcpListener.EndAcceptTcpClient(result);
        //Once it connects we want to still keep on listening for more clients so we call it again
        tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);
        Debug.Log($"Incoming connection from {client.Client.RemoteEndPoint}...");

        //Add to new connections until it's id is received
        Guid pId = Guid.NewGuid();
        newConnections.Add(pId, new NewConnection(pId));
        newConnections[pId].Connect(client);
    }

    public static void HandleData(Packet data)
    {
        int packetLength = data.ReadInt();
        byte[] packetBytes = data.ReadBytes(packetLength);

        ThreadManager.ExecuteOnMainThread(() =>
        {
            using (Packet packet = new Packet(packetBytes))
            {
                int packetId = packet.ReadInt();

                Guid id = Guid.Empty;
                try
                {
                    id = packet.ReadGuid();
                }
                catch { };

                if (id == Guid.Empty || Client.instance.clientInfo.id == id)
                    return;

                packetHandlers[packetId](id, packet);
            }
        });
    }

    #endregion

    public static void SendUDPData(IPEndPoint peerEndPoint, Packet packet)
    {
        try
        {
            if (peerEndPoint != null)
            {
                _udpClient.Send(packet.ToArray(), packet.Length(), peerEndPoint);
            }
        }
        catch (Exception ex)
        {
            Debug.Log($"Error sending UDP data: {ex}");
        }
    }

    public static void SendUDPData(Packet packet)
    {
        try
        {
            _udpClient.Send(packet.ToArray(), packet.Length(), new IPEndPoint(IPAddress.Loopback, _roomPort)); //TODO: CHANGE LOOPBACK TO SERVER IP
        }
        catch (Exception ex)
        {
            Debug.Log($"Error sending UDP data: {ex}");
        }
    }

    public static void MulticastUDPData(Packet packet)
    {
        try
        {
            _udpClient.Send(packet.ToArray(), packet.Length(), _roomMulticastEndPoint);
        }
        catch (Exception ex)
        {
            Debug.Log($"Error multicasting UDP data: {ex}");
        }
    }

    public void ConnectToPeer(Guid id, string username, int spawnId, string ip, int port)
    {
        //store peer info
        peers.Add(id, new Peer(id, username, spawnId));
        peers[id].tcp.Connect(ip, port);
        Debug.Log($"Tried to connect to peer {id}");
    }

    public static void ListenToRoom(string roomAddress, int roomPort)
    {
        _roomIPaddress = IPAddress.Parse(roomAddress);
        _roomPort = roomPort;

        // Create endpoints
        _roomMulticastEndPoint = new IPEndPoint(_roomIPaddress, roomPort);
        _remoteEndPoint = new IPEndPoint(_localIPaddress, _roomPort);

        // Create and configure UdpClient
        _udpClient = new UdpClient();
        // The following three lines allow multiple clients on the same PC
        _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _udpClient.ExclusiveAddressUse = false;
        // Bind, Join
        _udpClient.Client.Bind(_remoteEndPoint);
        _udpClient.JoinMulticastGroup(_roomIPaddress);

        // Start listening for incoming data
        _udpClient.BeginReceive(new AsyncCallback(ReceivedCallback), null);
    }

    private static void ReceivedCallback(IAsyncResult result)
    {
        try
        {
            IPEndPoint clientEndPoint = new IPEndPoint(0, _roomPort);
            byte[] data = _udpClient.EndReceive(result, ref clientEndPoint);
            // Restart listening for udp data packages
            _udpClient.BeginReceive(new AsyncCallback(ReceivedCallback), null);

            if (data.Length < 4)
                return;

            //Handle Data
            using (Packet packet = new Packet(data))
            {
                HandleData(packet);
            }
        }
        catch (Exception ex)
        {
            Debug.Log($"Error receiving UDP Multicast data: {ex}");
        }
    }

    public static string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                Debug.Log($"Local ip:{ip}");
                return ip.ToString();
            }
        }
        throw new Exception("Local IP Address Not Found!");
    }

    public class TCP
    {
        public TcpClient socket;

        private NetworkStream stream;
        private Packet receivedData;
        private byte[] receiveBuffer;

        public void Connect(IPAddress _ip, int _port)
        {
            socket = new TcpClient
            {
                ReceiveBufferSize = dataBufferSize,
                SendBufferSize = dataBufferSize
            };

            receiveBuffer = new byte[dataBufferSize];
            socket.BeginConnect(_ip, _port, ConnectCallback, socket);
        }

        private void ConnectCallback(IAsyncResult result)
        {
            socket.EndConnect(result);

            if (!socket.Connected)
            {
                return;
            }

            stream = socket.GetStream();

            receivedData = new Packet();

            stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
        }

        public void SendData(Packet packet)
        {
            try
            {
                if (socket != null)
                {
                    stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null);
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"Error sending data to server via TCP: {ex}");
            }
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                int byteLength = stream.EndRead(result);
                if (byteLength <= 0)
                {
                    instance.Disconnect();
                    return;
                }

                byte[] data = new byte[byteLength];
                Array.Copy(receiveBuffer, data, byteLength);

                //handle data
                receivedData.Reset(HandleData(data));
                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
            }
            catch (Exception)
            {
                Disconnect();
            }
        }

        private bool HandleData(byte[] data)
        {
            int packetLength = 0;

            receivedData.SetBytes(data);
            if (receivedData.UnreadLength() >= 4)
            {
                packetLength = receivedData.ReadInt();
                if (packetLength <= 0)
                    return true;
            }

            while (packetLength > 0 && packetLength <= receivedData.UnreadLength())
            {
                byte[] packetBytes = receivedData.ReadBytes(packetLength);
                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using (Packet packet = new Packet(packetBytes))
                    {
                        int packetId = packet.ReadInt();
                        packetHandlers[packetId](Guid.Empty, packet);
                    }
                });

                packetLength = 0;
                if (receivedData.UnreadLength() >= 4)
                {
                    packetLength = receivedData.ReadInt();
                    if (packetLength <= 0)
                        return true;
                }
            }

            if (packetLength <= 1)
                return true;
            return false;
        }

        private void Disconnect()
        {
            instance.Disconnect();

            stream = null;
            receiveBuffer = null;
            receivedData = null;
            socket = null;
        }
    }

    private void InitializeData()
    {
        packetHandlers = new Dictionary<int, PacketHandler>()
        {   
            //SERVER SENT
            { (int) ServerPackets.welcome, ClientHandle.WelcomeServer },
            { (int) ServerPackets.joinedRoom, ClientHandle.JoinedRoom },
            { (int) ServerPackets.playerJoined, ClientHandle.PlayerJoined },
            { (int) ServerPackets.playerLeft, ClientHandle.PlayerLeft },
            { (int) ServerPackets.map, ClientHandle.Map },
            { (int) ServerPackets.startGame, ClientHandle.StartGame },
            //CLIENT SENT
            { (int) ClientPackets.welcome, ClientHandle.WelcomePeer },
            { (int) ClientPackets.playerMovement, ClientHandle.PlayerMovement },
            { (int) ClientPackets.playerAnim, ClientHandle.PlayerAnim },
            { (int) ClientPackets.playerRespawn, ClientHandle.PlayerRespawn },
            { (int) ClientPackets.playerFinish, ClientHandle.PlayerFinish },
            { (int) ClientPackets.map, ClientHandle.Map },
            { (int) ClientPackets.startGame, ClientHandle.StartGame },
        };
        //TEMPORARY FOR P2P USE ONLY
    }

    private void OnApplicationQuit()
    {
        Disconnect();
    }

    public void Disconnect()
    {
        if (isConnected)
        {
            isConnected = false;
            //Close connection to server
            server.socket.Close();
            //Stop listening to incoming messages
            tcpListener.Stop();
            _udpClient.DropMulticastGroup(_roomIPaddress);
            _udpClient.Close();
            //Clear peer/connections dictionary
            peers = new Dictionary<Guid, Peer>();
            newConnections = new Dictionary<Guid, NewConnection>();
            Debug.Log("Disconnected.");
        }
    }
}
