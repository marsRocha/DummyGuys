using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Client
{
    public class Client
    {
        public delegate void PacketHandler(Packet packet);
        public static Dictionary<int, PacketHandler> packetHandlers;

        public static int dataBufferSize = 4096;

        // TCP
        static TCP server;
        // UDP
        public static UDP udp;
        static UdpClient multicastClient;
        // Multicast
        static int _roomPort = 7778;
        static IPAddress _multicastIPaddress = IPAddress.Parse("233.0.0.2");
        static IPAddress _localIPaddress;
        static IPEndPoint _localEndPoint;
        static IPEndPoint _remoteEndPoint;

        public static void Start()
        {
            packetHandlers = new Dictionary<int, PacketHandler>()
            {
                { (int)Packets.test, MessageReceived },
                { (int)Packets.welcome, WelcomeReceived },
            };

            _localIPaddress = IPAddress.Any;

            server = new TCP();
            udp = new UDP();
            server.Connect(IPAddress.Loopback, 7777);

            // Create endpoints
            _remoteEndPoint = new IPEndPoint(_multicastIPaddress, _roomPort);
            _localEndPoint = new IPEndPoint(_localIPaddress, _roomPort);
        }

        #region Udp

        public static void MulticastConnect()
        {
            // Create and configure UdpClient
            multicastClient = new UdpClient();
            // The following three lines allow multiple clients on the same PC
            multicastClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            multicastClient.ExclusiveAddressUse = false;
            // Bind, Join
            multicastClient.Client.Bind(_localEndPoint);
            multicastClient.JoinMulticastGroup(_multicastIPaddress);

            // Start listening for incoming data
            multicastClient.BeginReceive(new AsyncCallback(ReceivedCallback), null);
        }

        private static void ReceivedCallback(IAsyncResult ar)
        {
            // Get received data
            IPEndPoint sender = new IPEndPoint(IPAddress.Any, _roomPort);
            Byte[] data = multicastClient.EndReceive(ar, ref sender);
            // Restart listening for udp data packages
            multicastClient.BeginReceive(new AsyncCallback(ReceivedCallback), null);

            if (data.Length < 4)
                return;

            //Handle Data
            using (Packet packet = new Packet(data))
            {
                int packetLength = packet.ReadInt();
                byte[] packetBytes = packet.ReadBytes(packetLength);

                using (Packet message = new Packet(packetBytes))
                {
                    int packetId = message.ReadInt();

                    packetHandlers[packetId](message);
                }
            }
        }

        public static void SendUDPMulticast()
        {
            using (Packet packet = new Packet((int)Packets.test))
            {
                packet.Write("Multicast from client.");

                packet.WriteLength();
                multicastClient.Send(packet.ToArray(), packet.Length(), _remoteEndPoint);
            }
            Console.WriteLine($"Multicast sent");
        }

        public class UDP
        {
            public UdpClient socket;
            public IPEndPoint endPoint;

            public UDP()
            {
                endPoint = new IPEndPoint(IPAddress.Loopback, _roomPort);
            }

            public void Connect(int _localPort)
            {
                socket = new UdpClient(_localPort);

                socket.Connect(endPoint);
                socket.BeginReceive(ReceiveCallback, null);
                
                SendData();
            }

            public void SendData()
            {
                using (Packet packet = new Packet((int)Packets.test))
                {
                    packet.Write("Udp from client.");

                    packet.WriteLength();
                    socket.Send(packet.ToArray(), packet.Length());
                }
                Console.WriteLine($"Udp sent");
            }


            private void ReceiveCallback(IAsyncResult _result)
            {
                try
                {
                    byte[] _data = socket.EndReceive(_result, ref endPoint);
                    socket.BeginReceive(ReceiveCallback, null);

                    if (_data.Length < 4)
                    {
                        Disconnect();
                        return;
                    }

                    HandleData(_data);
                }
                catch
                {
                    Disconnect();
                }
            }

            private void HandleData(byte[] _data)
            {
                using (Packet _packet = new Packet(_data))
                {
                    int _packetLength = _packet.ReadInt();
                    _data = _packet.ReadBytes(_packetLength);
                }

                using (Packet _packet = new Packet(_data))
                {
                    int _packetId = _packet.ReadInt();
                    packetHandlers[_packetId](_packet);
                }
            }

            private void Disconnect()
            {
                endPoint = null;
                socket = null;
            }
        }

        #endregion

        #region TCP
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
                    Console.WriteLine($"Error sending data to server via TCP: {ex}");
                }
            }

            private void ReceiveCallback(IAsyncResult result)
            {
                try
                {
                    int byteLength = stream.EndRead(result);
                    if (byteLength <= 0)
                    {
                        Disconnect();
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

                    using (Packet packet = new Packet(packetBytes))
                    {
                        int packetId = packet.ReadInt();
                        packetHandlers[packetId](packet);
                    }

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
                stream = null;
                receiveBuffer = null;
                receivedData = null;
                socket = null;
            }
        }

        public static void SendTcp()
        {
            using (Packet packet = new Packet((int)Packets.test))
            {
                packet.Write("Tcp from client.");

                packet.WriteLength();
                server.SendData(packet);
            }
            Console.WriteLine($"TCP sent");
        }

        public static void SendTcpConnect()
        {
            using (Packet packet = new Packet((int)Packets.connect))
            {
                packet.Write("Tcp connect from client.");

                packet.WriteLength();
                server.SendData(packet);
            }
            Console.WriteLine($"TCP connect sent");
        }
        #endregion

        #region Messages
        public static void WelcomeReceived(Packet packet)
        {
            // Now that we have the client's id, connect UDP
            udp.Connect(((IPEndPoint)server.socket.Client.LocalEndPoint).Port);

            MulticastConnect();
        }

        public static void MessageReceived(Packet packet)
        {
            string message = packet.ReadString();

            Console.WriteLine($"Got message: {message}");
        }
        #endregion
    }
}
