using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;

public class Peer
{
    public Guid Id { get; private set; }
    public string Username { get; private set; }

    public static int dataBufferSize = 4036;
    public TCP tcp;
    public UDP udp;

    public Peer(Guid clientId, string username)
    {
        Id = clientId;
        Username = username;
        tcp = new TCP(Id);
        udp = new UDP(Id);
    }

    public void SetIdentification(Guid id, string username)
    {
        Id = id;
        Username = username;
    }

    public class TCP
    {
        public TcpClient socket;
        private NetworkStream stream;
        private Packet receivedData;
        private byte[] receiveBuffer;
        private readonly Guid id;

        public TCP(Guid clientId)
        {
            id = clientId;
        }

        //Used when we reach out to a peer in order to connect
        //waits for response with connect callback
        public void Connect(string ip, int port)
        {
            socket = new TcpClient
            {
                ReceiveBufferSize = dataBufferSize,
                SendBufferSize = dataBufferSize
            };

            receiveBuffer = new byte[dataBufferSize];
            socket.BeginConnect(ip, port, ConnectCallback, socket);

            ClientSend.Introduction(id);
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

        //Used when peer reaches out to us 
        //try to connect and send a message with him
        //no need to sen intro the other peer was the one who established the first connection, which means he has my data sent from the server
        public void Connect(TcpClient clientSocket)
        {
            socket = clientSocket;
            socket.ReceiveBufferSize = dataBufferSize;
            socket.SendBufferSize = dataBufferSize;

            stream = socket.GetStream();
            receivedData = new Packet();
            receiveBuffer = new byte[dataBufferSize];

            stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);

            //Send acknoledge of connection
            ClientSend.WelcomePeer(id);
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
                    Client.peers[id].Disconnect();
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
                        Client.packetHandlers[packetId](id, packet);
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
        public UDP(Guid clientId)
        {
            id = clientId;
        }

        public void Connect(IPEndPoint _endPoint)
        {
            endPoint = _endPoint;

            //send packet to initiate connection and open port so client can receive messages
            using (Packet packet = new Packet())
            {
                SendData(packet);
            }
            Debug.Log("Connection packet sent");
        }

        public void SendData(Packet packet)
        {
            Client.SendUDPData(endPoint, packet);
        }

        public void HandleData(Packet data)
        {
            int packetLength = data.ReadInt();
            byte[] packetBytes = data.ReadBytes(packetLength);

            ThreadManager.ExecuteOnMainThread(() =>
            {
                using (Packet packet = new Packet(packetBytes))
                {
                    int packetId = packet.ReadInt();
                    Client.packetHandlers[packetId]( id, packet);
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
        Debug.Log($"Player {Id} has disconnected.");
        GameManager.instance.Disconnect(Id);

        tcp.Disconnect();
        udp.Disconnect();
    }
}