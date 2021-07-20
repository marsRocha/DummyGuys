using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;

/// <summary>
/// Contains all methods controling the game's client networking functions.
/// </summary>
public class Client : MonoBehaviour
{
    public static Client instance;

    public static int dataBufferSize = 4096;

    public static Dictionary<Guid, Peer> peers = new Dictionary<Guid, Peer>();

    private static UdpClient _udpClient;

    //Server Info
    private static IPAddress _serverIPaddress;
    private static int _serverPort = 26950;
    public TCP Server { get; private set; }

    //Room Info
    private static IPAddress _roomIPaddress;
    private static int _roomPort;
    private static IPEndPoint _localEndPoint; // Client IP, Client Port
    private static IPEndPoint _roomMulticastEndPoint; // Room IP, Room Port - to send multicast messages
    private static IPEndPoint _remoteEndPoint; // Any IP, Room Port - to bind to and receive multicast from whomever sends them

    //Client Info
    private static IPAddress _localIPaddress;

    //TODO: set private
    public bool isConnected = false;

    // Ping
    public DateTime pingSent;
    public double ping;

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

    /// <summary>Starts Client networking.</summary>
    public void GoOnline(string _ip, int _port)
    {
        ClientInfo.instance.Id = Guid.NewGuid();

        _serverIPaddress = IPAddress.Parse(_ip);
        _serverPort = _port;

        _localIPaddress = IPAddress.Any;

        InitializeData();
        ConnectToServer();
    }

    /// <summary>Attempts to connect to the server.</summary>
    private void ConnectToServer()
    {
        Server = new TCP();

        Server.Connect(_serverIPaddress, _serverPort);
    }

    /// <summary>Receives incoming UDP data.</summary>
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

            // Handle Data
            using (Packet packet = new Packet(data))
            {
                HandleData(packet);
            }
        }
        catch (Exception ex)
        {
            if(instance.isConnected)
                Debug.Log($"Error receiving UDP Multicast data: {ex}");
        }
    }

    /// <summary>Prepares received data to be used by the appropriate packet handler methods.</summary>
    /// <param name="_data">The recieved data.</param>
    public static void HandleData(Packet _data)
    {
        int packetLength = _data.ReadInt();
        byte[] packetBytes = _data.ReadBytes(packetLength);

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

                // Turn way unidetified messages and messages sent my me
                if (id == Guid.Empty || ClientInfo.instance.Id == id)
                    return;

                //TODO: to do this i need to send roomid on player movement messages inbetween clients
                // Turn way messages that do not have the identifier of the room, if we are already in a room
                /*if (ClientInfo.instance.RoomId != Guid.Empty && ClientInfo.instance.RoomId != id)
                    return;*/

                ClientHandle.packetHandlers[packetId](id, packet);
            }
        });
    }

    /// <summary>Sends data to Room via UDP.</summary>
    /// <param name="_packet">The packet to send.</param>
    public static void SendUDPData(Packet _packet)
    {
        try
        {
            _udpClient.Send(_packet.ToArray(), _packet.Length(), new IPEndPoint(IPAddress.Loopback, _roomPort)); //TODO: CHANGE LOOPBACK TO SERVER IP
        }
        catch (Exception ex)
        {
            Debug.Log($"Error sending UDP data: {ex}");
        }
    }

    /// <summary>Sends data to Room via UDP Multicast.</summary>
    /// <param name="_packet">The packet to send.</param>
    public static void MulticastUDPData(Packet _packet)
    {
        try
        {
            _udpClient.Send(_packet.ToArray(), _packet.Length(), _roomMulticastEndPoint);
        }
        catch (Exception ex)
        {
            Debug.Log($"Error multicasting UDP data: {ex}");
        }
    }

    /// <summary>Join the games room's multicast</summary>
    /// <param name="_address">The room's address.</param>
    /// <param name="_port">The room's port.</param>
    public static void ListenToRoom(string _address, int _port)
    {
        _roomIPaddress = IPAddress.Parse(_address);
        _roomPort = _port;

        // Create endpoints
        _roomMulticastEndPoint = new IPEndPoint(_roomIPaddress, _roomPort);
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

    //TODO: TO BE REPLACED
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

            // Send introduction
            ClientSend.Introduction();
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

                        Guid id = Guid.Empty;
                        try
                        {
                            id = packet.ReadGuid();
                        }
                        catch { };

                        ClientHandle.packetHandlers[packetId](id, packet);
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

    /// <summary>Initializes all necessary client data.</summary>
    private void InitializeData()
    {
        ClientHandle.InitializeData();
    }

    /// <summary>Disconnects from the server and stops all network traffic.</summary>
    public void Disconnect()
    {
        if (isConnected)
        {
            isConnected = false;
            // Close connection to server
            Server.socket.Close();
            // Stop listening to incoming messages
            _udpClient.DropMulticastGroup(_roomIPaddress);
            _udpClient.Close();
            // Clear peer/connections dictionary
            peers = new Dictionary<Guid, Peer>();
            // Clear client information
            ClientInfo.instance.Clear();

            Debug.Log("Disconnected.");
        }
    }

    /// <summary>Calls disconnect method before the application is quit.</summary>
    private void OnApplicationQuit()
    {
        Disconnect();
    }
}
