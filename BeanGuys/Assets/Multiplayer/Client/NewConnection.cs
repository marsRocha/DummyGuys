using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;

public class NewConnection
{
    public Guid Id { get; private set; }
    public string Username { get; private set; }

    public static int dataBufferSize = 4036;
    public TCP tcp;

    public NewConnection(Guid clientId)
    {
        Id = clientId;
        Username = Username;
        tcp = new TCP(Id);
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

        private void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                int byteLength = stream.EndRead(result);
                if (byteLength <= 0)
                {
                    //Client.peers[id].Disconnect();
                    return;
                }

                byte[] data = new byte[byteLength];
                Array.Copy(receiveBuffer, data, byteLength);

                receivedData.Reset(HandleData(data));
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

                        //Check if it is an introduction packet(9)
                        if(packetId == 9)
                        {
                            Guid peerID = packet.ReadGuid();
                            string username = packet.ReadString();

                            //Add new peer since now we know their information
                            Client.peers.Add(peerID, new Peer(peerID, username));
                            Client.peers[peerID].tcp.Connect(Client.newConnections[id].tcp.socket);

                            GameManager.instance.UpdatePlayerCount();
                            Debug.Log($"Peer[{peerID}] introduction finished! {username} has joined the game!");
                        }
                        else
                        {
                            Debug.Log($"Error receiving introduction data: Packet id does not match");
                        }
                        //Remove this connection, no longer needed
                        Client.newConnections.Remove(id);
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
}