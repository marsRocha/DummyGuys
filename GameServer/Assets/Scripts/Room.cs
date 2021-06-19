using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public partial class Room
{
    public Guid RoomId { get; set; }
    public RoomState RoomState { get; set; }
    public List<int> UsedSpawnIds { get; set; }
    public Dictionary<Guid, ClientInfo> ClientsInfo { get; set; }
    public IPAddress MulticastIP { get; set; }
    public int MulticastPort { get; set; }
    private IPAddress _localIPaddress { get; set; }

    public UdpClient RoomUdp { get; set; }
    private IPEndPoint _remoteEndPoint { get; set; }
    private IPEndPoint _localEndPoint { get; set; }

    private RoomThread roomThread;
    public RoomScene RoomScene { get; set; }

    public Room(Guid id, string multicastIP, int multicastPort)
    {
        RoomId = id;
        MulticastIP = IPAddress.Parse(multicastIP);
        MulticastPort = multicastPort + 1;

        RoomState = RoomState.looking;
        ClientsInfo = new Dictionary<Guid, ClientInfo>();
        UsedSpawnIds = new List<int>();

        _localIPaddress = IPAddress.Any;

        // Create endpoints
        _remoteEndPoint = new IPEndPoint(MulticastIP, MulticastPort);
        _localEndPoint = new IPEndPoint(_localIPaddress, MulticastPort);

        // Create and configure UdpClient
        RoomUdp = new UdpClient();
        // The following two lines allow multiple clients on the same PC
        RoomUdp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        RoomUdp.ExclusiveAddressUse = false;
        // Bind, Join
        RoomUdp.Client.Bind(_localEndPoint);
        //RoomUdp.JoinMulticastGroup(MulticastIP);

        // Start listening for incoming data
        RoomUdp.BeginReceive(new AsyncCallback(ReceivedCallback), null);

        Console.WriteLine($"New lobby created [{RoomId}]: listenning in {multicastIP}:{multicastPort}");
    }

    private void ReceivedCallback(IAsyncResult result)
    {
        // Get received data
        IPEndPoint clientEndPoint = new IPEndPoint(0, MulticastPort);
        byte[] data = RoomUdp.EndReceive(result, ref clientEndPoint);
        // Restart listening for udp data packages
        RoomUdp.BeginReceive(new AsyncCallback(ReceivedCallback), null);

        if (data.Length < 4)
            return;

        //Handle Data
        using (Packet packet = new Packet(data))
        {
            int packetLength = packet.ReadInt();
            byte[] packetBytes = packet.ReadBytes(packetLength);

            ThreadManager.ExecuteOnMainThread(() =>
            {
                using (Packet message = new Packet(packetBytes))
                {
                    int packetId = message.ReadInt();

                    Guid clientId = Guid.Empty;
                    try
                    {
                        clientId = message.ReadGuid();
                    }
                    catch { };

                    if (clientId == Guid.Empty)
                        return;
                    //Console.WriteLine("Got Message");
                    //verify if the endpoint corresponds to the endpoint that sent the data
                    //this is for security reasons otherwise hackers could inpersonate other clients by send a clientId that does not corresponds to them
                    //without the string conversion even if the endpoint matched it returned false
                    if (Server.Clients[clientId].RoomID == RoomId) //TODO: Do I really need to check for this?
                    {
                        Server.packetHandlers[packetId](clientId, message);
                    }
                }
            });
        }
    }

    private void MulticastUDPData(Packet packet)
    {
        packet.WriteLength();
        RoomUdp.Send(packet.ToArray(), packet.Length(), _remoteEndPoint);

        Console.WriteLine($"Multicast sent");
    }

    public void CloseRoom()
    {
        RoomUdp.DropMulticastGroup(MulticastIP); //TODO: does not work
        RoomUdp.Close();

        Console.WriteLine($"Room[{RoomId}] has been closed.");
    }
}

public enum RoomState { looking, full, playing, closing }