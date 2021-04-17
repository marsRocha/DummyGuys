using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;
using System.Security.Cryptography;
using System.Net.Http.Headers;

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
    private static UdpClient udpClient;

    //Server Info
    private string serverIP = "127.0.0.1";
    public int serverPort = 26950;
    public TCP server { get; private set; }

    //TODO: public for now, afterwards it will be the server to give the client this information
    public static IPAddress _roomMulticastIPaddress;
    private static IPEndPoint _roomMulticastEndPoint;
    private static IPEndPoint _lolcalEndPoint;

    //Client Info
    public static IPAddress _localIPaddress;
    public static int MyPort;
    public Guid myId;
    public string username;
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

    public void GoOnline(string ip, int port)
    {
        serverIP = ip;
        serverPort = port;

        _localIPaddress = IPAddress.Parse("127.0.0.1");// IPAddress.Any;
        //Depending on the id set on inspector, set a different ip and port to listen to peers
        MyPort = 5000 + clientExeID;

        InitializeData();
        ConnectToServer();

        //Listen to peers
        tcpListener = new TcpListener(_localIPaddress, MyPort);
        tcpListener.Start();
        tcpListener.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

        // Create endpoint
        _lolcalEndPoint = new IPEndPoint(_localIPaddress, MyPort);

        // Create and configure UdpClient
        udpClient = new UdpClient();
        // The following two lines allow multiple clients on the same PC
        udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        udpClient.ExclusiveAddressUse = false;
        // Bind, Join
        udpClient.Client.Bind(_lolcalEndPoint);
        // Start listening for incoming data
        udpClient.BeginReceive(UDPReceiveCallback, null);

        Debug.Log($"Client started listening on {MyPort}");
    }

    private void ConnectToServer()
    {
        server = new TCP();

        isConnected = true;
        server.Connect(serverIP, serverPort);
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
        newConnections[pId].tcp.Connect(client);
    }

    private static void UDPReceiveCallback(IAsyncResult result)
    {
        try
        {
            Debug.Log("got mail");
            //IPEndPoint clientEndPoint = new IPEndPoint(_localIPaddress, MyPort);
            IPEndPoint clientEndPoint = new IPEndPoint(0, 0);
            byte[] data = udpClient.EndReceive(result, ref clientEndPoint);
            udpClient.BeginReceive(UDPReceiveCallback, null);

            if (data.Length < 4)
            {
                Debug.Log("Connection packet received.");
                return;
            }

            using (Packet packet = new Packet(data))
            {
                Guid clientId = packet.ReadGuid();

                if (clientId == null)
                    return;

                if (peers[clientId].udp.endPoint == null)
                {
                    peers[clientId].udp.Connect(clientEndPoint);
                    return;
                }

                //Debug.Log($"client Endpoint:{clientEndPoint}, udpEndPoint:{peers[1].udp.endPoint}");
                //verifiy if the endpoint corresponds to the endpoint that sent the data
                //this is for security reasons otherwise hackers could inpersonate other clients by send a clientId that does not corresponds to them
                //without the string conversion even if the endpoint matched it returned false
                if (peers[clientId].udp.endPoint.Equals(clientEndPoint))
                {
                    //Debug.Log($"Handle data, peerID:{clientId}");

                    //peers[clientId].udp.HandleData(data);
                    peers[clientId].udp.HandleData(packet);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Log($"Error receiving UDP data: {ex}");
        }
    }

    #endregion

    public static void SendUDPData(IPEndPoint peerEndPoint, Packet packet)
    {
        try
        {
            //Send my id so who receives it knows who sent it
            //packet.InsertGuid(instance.myId);

            if (peerEndPoint != null)
            {
                udpClient.BeginSend(packet.ToArray(), packet.Length(), peerEndPoint, null, null);
            }
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
            packet.InsertGuid(instance.myId);
            udpClient.Send(packet.ToArray(), packet.Length(), _roomMulticastEndPoint);
        }
        catch (Exception ex)
        {
            Debug.Log($"Error multicasting UDP data: {ex}");
        }
    }

    public void ConnectToPeer(Guid id, string username, string ip, int port)
    {
        //store peer info
        peers.Add(id, new Peer(id, username));
        peers[id].tcp.Connect(ip, port);
        Debug.Log($"Tried to connect to peer {id}");
    }

    public static void ListenToRoom(string roomAddress, int roomPort)
    {
        _roomMulticastIPaddress = IPAddress.Parse(roomAddress);

        // Create endpoints
        _roomMulticastEndPoint = new IPEndPoint(_roomMulticastIPaddress, roomPort);

        udpClient.JoinMulticastGroup(_roomMulticastIPaddress, _localIPaddress);
        udpClient.MulticastLoopback = true;
        udpClient.Client.MulticastLoopback = true;
    }

    public class TCP
    {
        public TcpClient socket;

        private NetworkStream stream;
        private Packet receivedData;
        private byte[] receiveBuffer;

        public void Connect(string ip, int port)
        {
            socket = new TcpClient
            {
                ReceiveBufferSize = dataBufferSize,
                SendBufferSize = dataBufferSize
            };

            receiveBuffer = new byte[dataBufferSize];
            socket.BeginConnect(ip, port, ConnectCallback, socket);
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
            { (int) ServerPackets.welcome, ClientHandle.WelcomeServer },
            { (int) ServerPackets.peer, ClientHandle.PeerList },
            { (int) ServerPackets.joinedRoom, ClientHandle.JoinedRoom },
            { (int) ClientPackets.welcome, ClientHandle.WelcomePeer },
            { (int) ClientPackets.playerMovement, ClientHandle.PlayerMovement },
            { (int) ClientPackets.playerAnim, ClientHandle.PlayerAnim },
            { (int) ClientPackets.playerRespawn, ClientHandle.PlayerRespawn },
            { (int) ClientPackets.playerFinish, ClientHandle.PlayerFinish },
            { (int) ClientPackets.startGame, ClientHandle.StartGame }, //TODO: REMOVE
        };
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
            udpClient.Close();
            //Clear peer/connections dictionary
            peers = new Dictionary<Guid, Peer>();
            newConnections = new Dictionary<Guid, NewConnection>();
            Debug.Log("Disconnected.");
        }
    }
}
