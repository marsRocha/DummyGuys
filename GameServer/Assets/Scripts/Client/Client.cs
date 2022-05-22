using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class Client
{
    public readonly Guid Id;
    public readonly string Username;
    public readonly int Color;

    public int ClientRoomId { get; private set; }
    public readonly int RoomID;
    public bool ready, finished;

    public Player Player { get; private set; }

    public static int dataBufferSize = 4036;

    public TCP tcp;
    public UDP udp;

    public Client(Guid _id, string _username, int _color, int _roomId)
    {
        Id = _id;
        Username = _username;
        Color = _color;
        RoomID = _roomId;

        tcp = new TCP(RoomID);
        udp = new UDP(RoomID);
    }

    public class TCP
    {
        public TcpClient socket;
        private NetworkStream stream;
        private Packet receivedData;
        private byte[] receiveBuffer;
        private int clientRoomId;
        private readonly int roomId;

        public TCP(int _roomId)
        {
            roomId = _roomId;
        }

        public void Connect(TcpClient clientSocket)
        {
            socket = clientSocket;
            socket.ReceiveBufferSize = dataBufferSize;
            socket.SendBufferSize = dataBufferSize;

            stream = socket.GetStream();
            receivedData = new Packet();
            receiveBuffer = new byte[dataBufferSize];

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
            catch
            {
                //Debug.Log($"Error sending data to player {playerRoomId} via TCP: {ex}");
            }
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                int byteLength = stream.EndRead(result);
                if (byteLength <= 0)
                {
                    Server.Rooms[roomId].RemovePlayer(clientRoomId);
                    return;
                }

                byte[] data = new byte[byteLength];
                Array.Copy(receiveBuffer, data, byteLength);

                receivedData.Reset(HandleData(data));
                //handle data
                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
            }
            catch
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
                packetLength = receivedData.GetInt();
                if (packetLength <= 0)
                    return true;
            }

            while (packetLength > 0 && packetLength <= receivedData.UnreadLength())
            {
                byte[] packetBytes = receivedData.GetBytes(packetLength);
                Server.MainThread.ExecuteOnMain(() =>
                {
                    using (Packet packet = new Packet(packetBytes))
                    {
                        int packetId = packet.GetInt();
                        RoomHandle.packetHandlers[packetId](roomId, clientRoomId, packet);
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

        public void Disconnect()
        {
            socket.Close();
            stream = null;
            receivedData = null;
            receiveBuffer = null;
            socket = null;
        }

        public void SetClientRoomId(int _playerRoomId)
        {
            clientRoomId = _playerRoomId;
        }
    }

    public class UDP
    {
        public IPEndPoint endPoint;
        private readonly int roomId;

        public UDP(int _roomId)
        {
            roomId = _roomId;
        }

        /// <summary>Initializes the newly connected client's UDP-related info.</summary>
        /// <param name="_endPoint">The IPEndPoint instance of the newly connected client.</param>
        public void Connect(IPEndPoint _endPoint)
        {
            endPoint = _endPoint;
        }

        /// <summary>Sends data to the client via UDP.</summary>
        /// <param name="_packet">The packet to send.</param>
        public void SendData(Packet _packet)
        {
            Server.Rooms[roomId].multicastUDP.SendUDPData(endPoint, _packet);
        }

        /// <summary>Cleans up the UDP connection.</summary>
        public void Disconnect()
        {
            endPoint = null;
        }
    }

    public void Ping()
    {
        if (Player != null)
        {
            lock (Player)
            {
                Player.Ping();
            }
        }
    }

    public void Disconnect()
    {
        tcp.Disconnect();
        udp.Disconnect();

        if (Player)
            Player.Deactivate();
    }

    public void SetPlayerRoomId(int _playerRoomId)
    {
        ClientRoomId = _playerRoomId;

        tcp.SetClientRoomId(_playerRoomId);
    }

    public void SetPlayer(Player _player)
    {
        Player = _player;
    }
}
