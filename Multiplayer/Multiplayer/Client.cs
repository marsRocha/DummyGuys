using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Numerics;

namespace Multiplayer
{
    public class Client
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public Guid RoomID { get; set; }

        public static int dataBufferSize = 4036;

        public TCP tcp;
        public UDP udp;

        public Client(int clientId)
        {
            Id = clientId;
            tcp = new TCP(Id);
            udp = new UDP(Id);
        }

        public class TCP
        {
            public TcpClient socket;
            private NetworkStream stream;
            private Packet receivedData;
            private byte[] receiveBuffer;
            private readonly int id;

            public TCP(int clientId)
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

                //Welcome message
                ServerSend.Welcome(id);
            }

            public void SendData(Packet packet)
            {
                try
                {
                    if(socket != null)
                    {
                        stream.BeginWrite(packet.ToArray(), 0, packet.Length(), null, null);
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Error sending data to player {id} via TCP: {ex}");
                }
            }

            private void ReceiveCallback(IAsyncResult result)
            {
                try
                {
                    int byteLength = stream.EndRead(result);
                    if(byteLength <= 0)
                    {
                        Server.Clients[id].Disconnect();
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
                    Console.WriteLine($"Error receiving TCP data: {ex}");
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
                    ThreadManager.ExecuteOnMainThread(() =>
                    {
                        using (Packet packet = new Packet(packetBytes))
                        {
                            int packetId = packet.ReadInt();
                            Server.packetHandlers[packetId](id, packet);
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
            private int id;
            public UDP(int clientId)
            {
                id = clientId;
            }

            public void Connect(IPEndPoint _endPoint)
            {
                endPoint = _endPoint;
            }

            public void SendData(Packet packet)
            {
                Server.SendUDPData(endPoint, packet);
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
                        Server.packetHandlers[packetId](id, packet);
                    }
                });
            }

            public void Disconnect()
            {
                endPoint = null;
            }
        }
    
        /*public void SendIntoGame(string _playerName)
        {
            player = new Player(id, _playerName, Vector3.Zero);

            foreach(Client c in Server.clients.Values)
            {
                if(c.player != null)
                {
                    if (c.id != id)
                        ServerSend.SpawnPlayer(id, c.player);
                }
            }

            foreach(Client c in Server.clients.Values)
            {
                if(c.player != null)
                {
                    ServerSend.SpawnPlayer(c.player.id, player);
                }
            }
        }*/

        private void Disconnect()
        {
            Console.WriteLine($"{tcp.socket.Client.RemoteEndPoint} has disconnected.");
            
            Server.Rooms[RoomID].RemovePlayer(Id);
            tcp.Disconnect();
            udp.Disconnect();
            Server.Clients.Remove(Id);
        }
    }
}
