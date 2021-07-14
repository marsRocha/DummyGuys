using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class Client
{
    public readonly Guid Id;
    public readonly string Username;
    public readonly int Color;

    public int SpawnId;
    public readonly Guid RoomID;
    public bool ready, finished;


    public Player Player { get; private set; }


    public static int dataBufferSize = 4036;

    public TCP tcp;
    public UDP udp;

    public Client(Guid _id, string _username, int _color, Guid _roomId)
    {
        Id = _id;
        Username = _username;
        Color = _color;
        RoomID = _roomId;

        tcp = new TCP(Id, RoomID);
        udp = new UDP(Id, RoomID);
    }

    public class TCP
    {
        public TcpClient socket;
        private NetworkStream stream;
        private Packet receivedData;
        private byte[] receiveBuffer;
        private readonly Guid id;
        private readonly Guid roomId;

        public TCP(Guid _clientId, Guid _roomId)
        {
            id = _clientId;
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
            catch (Exception ex)
            {
                Debug.Log($"Error sending data to player {id} via TCP: {ex}");
            }
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                int byteLength = stream.EndRead(result);
                if (byteLength <= 0)
                {
                    Server.Rooms[roomId].Clients[id].Disconnect();
                    return;
                }

                byte[] data = new byte[byteLength];
                Array.Copy(receiveBuffer, data, byteLength);

                receivedData.Reset(HandleData(data));
                //handle data
                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
            }
            catch (Exception ex)
            {
                Debug.Log($"Error receiving TCP data: {ex}");
                // TODO: disconnect
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
                Server.MainThread.ExecuteOnMainThread(() =>
                {
                    using (Packet packet = new Packet(packetBytes))
                    {
                        int packetId = packet.ReadInt();
                        RoomHandle.packetHandlers[packetId](roomId, id, packet);
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

        public void Disconnect()
        {
            socket.Close();
            stream = null;
            receivedData = null;
            receiveBuffer = null;
            socket = null;
        }
    }

    public class UDP
    {
        public IPEndPoint endPoint;
        private readonly Guid id;
        private readonly Guid roomId;

        public UDP(Guid _clientId, Guid _roomId)
        {
            id = _clientId;
            roomId = _roomId;
        }

        public void Connect(IPEndPoint _endPoint)
        {
            endPoint = _endPoint;
        }

        public void SendData(Packet packet)
        {
            //Server.SendUDPData(endPoint, packet);
        }

        public void HandleData(Packet data)
        {
            int packetLength = data.ReadInt();
            byte[] packetBytes = data.ReadBytes(packetLength);

            Server.MainThread.ExecuteOnMainThread(() =>
            {
                using (Packet packet = new Packet(packetBytes))
                {
                    int packetId = packet.ReadInt();
                    RoomHandle.packetHandlers[packetId](roomId, id, packet);
                }
            });
        }

        public void Disconnect()
        {
            endPoint = null;
        }
    }

    private void Disconnect()
    {
        Debug.Log($"{tcp.socket.Client.RemoteEndPoint} has disconnected.");

        Server.Rooms[RoomID].RemovePlayer(Id);
        tcp.Disconnect();
        udp.Disconnect();
        Server.Rooms[RoomID].Clients.Remove(Id);
    }

    public void SetPlayer(Player _player)
    {
        Player = _player;
    }
}
