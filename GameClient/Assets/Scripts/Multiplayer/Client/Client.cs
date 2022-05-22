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

    public static Dictionary<int, Peer> peers = new Dictionary<int, Peer>();

    //Server Info
    public IPAddress serverIp { get; private set; }
    public int serverPort { get; private set; }

    public TCP tcp { get; private set; }
    public UDP udp { get; private set; }
    public Multicast multicast { get; private set; }

    //Room Info
    public IPAddress roomIp { get; private set; }
    public int roomPort { get; private set; }
    private static IPEndPoint _roomMulticastEndPoint; // Room IP, Room Port - to send multicast messages
    private static IPEndPoint _remoteEndPoint; // Any IP, Room Port - to bind to and receive multicast from whomever sends them

    //Client Info
    private static IPAddress _localIPaddress;

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
    public void Connect(string _ip, int _port)
    {
        ClientInfo.instance.Id = Guid.NewGuid();

        serverIp = IPAddress.Parse(_ip);
        serverPort = _port;

        tcp = new TCP();
        udp = new UDP();
        multicast = new Multicast();

        InitializeData();

        tcp.Connect();
    }

    public class TCP
    {
        public TcpClient socket;
        
        public int dataBufferSize;
        private NetworkStream stream;
        private Packet receivedData;
        private byte[] receiveBuffer;

        /// <summary>Attempts to connect to the server via TCP.</summary>
        public void Connect()
        {
            dataBufferSize = 4096;
            socket = new TcpClient
            {
                ReceiveBufferSize = dataBufferSize,
                SendBufferSize = dataBufferSize
            };

            receiveBuffer = new byte[dataBufferSize];
            socket.BeginConnect(instance.serverIp, instance.serverPort, ConnectCallback, socket);
        }

        /// <summary>Initializes the newly connected client's TCP-related info.</summary>
        private void ConnectCallback(IAsyncResult result)
        {
            socket.EndConnect(result);
            if (!socket.Connected)
                return;

            receivedData = new Packet();

            stream = socket.GetStream();
            stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
        }

        /// <summary>Sends data to the client via TCP.</summary>
        /// <param name="_packet">The packet to send.</param>
        public void SendData(Packet _packet)
        {
            try
            {
                if (socket != null)
                {
                    stream.BeginWrite(_packet.ToArray(), 0, _packet.Length(), null, null);
                }
            }
            catch (Exception _ex)
            {
                Debug.Log($"Error sending data to server via TCP: {_ex}");
                Disconnect();
            }
        }

        /// <summary>Process incoming data from the stream.</summary>
        private void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                int byteLength = stream.EndRead(result);
                if (byteLength <= 0)
                {
                    GameManager.instance.Disconnected();
                    return;
                }

                byte[] data = new byte[byteLength];
                Array.Copy(receiveBuffer, data, byteLength);

                // Handle data
                // Reset receivedData if all data was handled
                receivedData.Reset(HandleData(data));
                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
            }
            catch (Exception)
            {
                Disconnect();
            }
        }

        /// <summary>Prepares received data to be used by the appropriate packet handler methods.</summary>
        /// <param name="_data">The recieved data.</param>
        private bool HandleData(byte[] _data)
        {
            int packetLength = 0;

            receivedData.SetBytes(_data);
            if (receivedData.UnreadLength() >= 4)
            {
                packetLength = receivedData.GetInt();
                if (packetLength <= 0)
                    return true;
            }

            while (packetLength > 0 && packetLength <= receivedData.UnreadLength())
            {
                byte[] packetBytes = receivedData.GetBytes(packetLength);
                MessageQueuer.ExecuteOnMain(() =>
                {
                    using (Packet packet = new Packet(packetBytes))
                    {
                        int packetId = packet.GetInt();

                        int id = 0;
                        try
                        {
                            id = packet.GetInt();
                        }
                        catch { };

                        ClientHandle.packetHandlers[packetId](id, packet);
                    }
                });

                packetLength = 0;
                if (receivedData.UnreadLength() >= 4)
                {
                    packetLength = receivedData.GetInt();
                    if (packetLength <= 0)
                        return true;
                }
            }

            if (packetLength <= 1)
                return true;
            return false;
        }

        /// <summary>Disconnects from the server and cleans up the TCP connection.</summary>
        private void Disconnect()
        {
            instance.Disconnect();

            stream = null;
            receiveBuffer = null;
            receivedData = null;
            socket = null;
        }
    }

    public class UDP
    {
        public UdpClient socket;
        public IPEndPoint endPoint;

        /// <summary>Attempts to connect to the server via UDP.</summary>
        /// <param name="_localPort">The port number to bind the UDP socket to.</param>
        public void Connect(int _localPort)
        {
            endPoint = new IPEndPoint(instance.serverIp, instance.roomPort);

            socket = new UdpClient(_localPort);

            socket.Connect(endPoint);
            socket.BeginReceive(ReceiveCallback, null);

            // Send first udp message to room, in order for it to get the udp IPEndPoint
            using (Packet _packet = new Packet((int)ClientPackets.introduction))
            {
                _packet.Add(ClientInfo.instance.ClientRoomId);

                SendData(_packet);
            }
        }

        /// <summary>Sends data to the client via UDP.</summary>
        /// <param name="_packet">The packet to send.</param>
        public void SendData(Packet _packet)
        {
            try
            {
                if (socket != null)
                {
                    socket.BeginSend(_packet.ToArray(), _packet.Length(), null, null);
                }
            }
            catch (Exception _ex)
            {
                Debug.Log($"Error sending data to server via UDP: {_ex}");
            }

        }

        /// <summary>Receives incoming UDP data.</summary>
        private void ReceiveCallback(IAsyncResult _result)
        {
            try
            {
                byte[] _data = socket.EndReceive(_result, ref endPoint);
                socket.BeginReceive(ReceiveCallback, null);

                if (_data.Length < 4)
                {
                    instance.Disconnect();
                    return;
                }

                HandleData(_data);
            }
            catch
            {
                Disconnect();
            }
        }

        /// <summary>Prepares received data to be used by the appropriate packet handler methods.</summary>
        /// <param name="_data">The recieved data.</param>
        private void HandleData(byte[] _data)
        {
            using (Packet _packet = new Packet(_data))
            {
                int _packetLength = _packet.GetInt();
                _data = _packet.GetBytes(_packetLength);
            }

            MessageQueuer.ExecuteOnMain(() =>
            {
                // Translate received data into packet and treat it
                using (Packet packet = new Packet(_data))
                {
                    int packetId = packet.GetInt();

                    if (packetId == (int)ServerPackets.pong)
                    {
                        ClientHandle.Pong(0, packet);
                        return;
                    }

                    int id = 0;
                    try
                    {
                        id = packet.GetInt();
                    }
                    catch { };

                    if (id == 0)
                        return;

                    ClientHandle.packetHandlers[packetId](id, packet);
                }
            });
        }

        /// <summary>Disconnects from the server and cleans up the UDP connection.</summary>
        private void Disconnect()
        {
            instance.Disconnect();

            endPoint = null;
            socket = null;
        }
    }

    public class Multicast
    {
        public UdpClient socket;
        public IPEndPoint endPoint;

        /// <summary>Attempts to connect to the Room via Multicast Udp.</summary>
        public void Connect()
        {
            _localIPaddress = IPAddress.Any;

            // Create endpoints
            _roomMulticastEndPoint = new IPEndPoint(instance.roomIp, instance.roomPort);
            _remoteEndPoint = new IPEndPoint(_localIPaddress, instance.roomPort);

            // Create and configure UdpClient
            socket = new UdpClient();

            if (TestData.LOCAL)
            {
                // The following three lines allow multiple clients on the same PC
                socket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                socket.ExclusiveAddressUse = false;
            } 
            else socket.MulticastLoopback = false;
            // Bind, Join
            socket.Client.Bind(_remoteEndPoint);
            socket.JoinMulticastGroup(instance.roomIp);

            // Start listening for incoming data
            socket.BeginReceive(new AsyncCallback(ReceiveCallback), null);
        }

        /// <summary>Sends data to the client via UDP.</summary>
        /// <param name="_packet">The packet to send.</param>
        public void SendData(Packet _packet)
        {
            try
            {
                socket.Send(_packet.ToArray(), _packet.Length(), _roomMulticastEndPoint);
            }
            catch (Exception ex)
            {
                Debug.Log($"Error multicasting UDP data: {ex}");
            }

        }

        /// <summary>Receives incoming UDP data.</summary>
        private void ReceiveCallback(IAsyncResult _result)
        {
            try
            {
                IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, instance.roomPort);
                byte[] data = socket.EndReceive(_result, ref clientEndPoint);
                // Restart listening for udp data packages
                socket.BeginReceive(new AsyncCallback(ReceiveCallback), null);

                if (data.Length < 4)
                {
                    Disconnect();
                    return;
                }

                // Handle Data
                using (Packet packet = new Packet(data))
                {
                    HandleData(packet);
                }
            }
            catch (Exception ex)
            {
                if (instance.isConnected)
                    Debug.Log($"Error receiving UDP Multicast data: {ex}");
            }
        }

        /// <summary>Prepares received data to be used by the appropriate packet handler methods.</summary>
        /// <param name="_data">The recieved data.</param>
        private void HandleData(Packet _data)
        {
            int packetLength = _data.GetInt();
            byte[] packetBytes = _data.GetBytes(packetLength);

            MessageQueuer.ExecuteOnMain(() =>
            {
                using (Packet packet = new Packet(packetBytes))
                {
                    int packetId = packet.GetInt();

                    int id = 0;
                    try
                    {
                        id = packet.GetInt();
                    }
                    catch { };

                    // Turn way unidetified messages and messages sent my me
                    if (id == 0 || id == ClientInfo.instance.ClientRoomId)
                        return;

                    ClientHandle.packetHandlers[packetId](id, packet);
                }
            });
        }

        /// <summary>Disconnects from the server and cleans up the UDP connection.</summary>
        private void Disconnect()
        {
            instance.Disconnect();

            endPoint = null;
            socket = null;
        }
    }

    /// <summary>Connects udp and multicast to server's room.</summary>
    public static void ConnectToRoom(string _address, int _port)
    {
        instance.roomIp = IPAddress.Parse(_address);
        instance.roomPort = _port;

        instance.udp.Connect(((IPEndPoint)instance.tcp.socket.Client.LocalEndPoint).Port);
        instance.multicast.Connect();
    }

    /// <summary>Initializes client packet data.</summary>
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
            // Close connection to room
            udp.socket.Close();
            multicast.socket.DropMulticastGroup(roomIp);
            multicast.socket.Close();
            // Close connection to server
            tcp.socket.Close();
            // Clear peer/connections dictionary
            peers.Clear();
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
