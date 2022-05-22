using UnityEngine;
using System.Net.Sockets;
using System;

public class NewConnection
{
    private static int dataBufferSize = 4036;

    private TcpClient socket;
    private NetworkStream stream;
    private Packet receivedData;
    private byte[] receiveBuffer;

    public NewConnection(TcpClient _client)
    {
        socket = _client;
        Connect();
    }

    private void Connect()
    {
        socket.ReceiveBufferSize = dataBufferSize;
        socket.SendBufferSize = dataBufferSize;

        stream = socket.GetStream();
        receivedData = new Packet();
        receiveBuffer = new byte[dataBufferSize];

        stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);

        // Let client know connection was reached
        ServerSend.AcceptConnection(this);
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
            Debug.Log($"Error sending welcome packet to new connection: {ex}");
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
                // Check if packet sent is the correct one and has all the right information
                using (Packet _packet = new Packet(packetBytes))
                {
                    int packetId = _packet.GetInt();

                    if (packetId == (int)ClientPackets.introduction)
                    {
                        Guid _id = _packet.GetGuid();
                        string _username = _packet.GetString();
                        int _color = _packet.GetInt();

                        // Look for a room for player
                        int roomId = Server.SearchForRoom();

                        if (roomId == 0) // Found no room
                        {
                            //Console.WriteLine($"No rooms available, disconnecting new connection.");
                            Disconnect();
                        }

                        // Create client to be added to room
                        Client newClient = new Client(_id, _username, _color, roomId);
                        // Connect client to the newConnection socket
                        newClient.tcp.Connect(socket);

                        // Add to room 
                        Server.AddClientToRoom(newClient, roomId);
                    }
                    else
                    {
                        //Console.WriteLine($"Error receiving introduction data: Packet id does not match");
                        Disconnect();
                    }
                }
                Close();
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

    public void Close()
    {
        stream = null;
        receivedData = null;
        receiveBuffer = null;
        socket = null;
    }

    public void Disconnect()
    {
        ServerSend.RefuseConnection(this);
        socket.Close();
        stream = null;
        receivedData = null;
        receiveBuffer = null;
        socket = null;
    }
}