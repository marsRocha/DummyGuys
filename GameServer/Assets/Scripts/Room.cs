using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public partial class Room
{
    public Guid Id { get; set; }
    public RoomState RoomState { get; set; }
    public Dictionary<Guid, Player> Players { get; set; }
    public IPAddress MulticastIP { get; set; }
    public int MulticastPort { get; set; }
    private IPAddress _localIPaddress { get; set; }

    public UdpClient RoomUdp { get; set; }
    private IPEndPoint _remoteEndPoint { get; set; }
    private IPEndPoint _localEndPoint { get; set; }


    public Room(Guid id, string multicastIP, int multicastPort)
    {
        Id = id;
        MulticastIP = IPAddress.Parse(multicastIP);
        MulticastPort = multicastPort;

        RoomState = RoomState.looking;
        Players = new Dictionary<Guid, Player>();

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
        RoomUdp.JoinMulticastGroup(MulticastIP);

        // Start listening for incoming data
        RoomUdp.BeginReceive(new AsyncCallback(ReceivedCallback), null);

        Debug.Log($"New lobby created [{Id}]: listenning in {multicastIP}:{multicastPort}");
    }

    private void ReceivedCallback(IAsyncResult result)
    {
        // Get received data
        IPEndPoint clientEndPoint = new IPEndPoint(0, MulticastPort);
        byte[] data = RoomUdp.EndReceive(result, ref clientEndPoint);

        if (data.Length < 4)
            return;

        //Handle Data
        using (Packet packet = new Packet(data))
        {
            /*Guid clientId = packet.ReadGuid();

            if (clientId == null)
                return;*/
            //Debug.Log("Got Message");
            //verifiy if the endpoint corresponds to the endpoint that sent the data
            //this is for security reasons otherwise hackers could inpersonate other clients by send a clientId that does not corresponds to them
            //without the string conversion even if the endpoint matched it returned false
            /*if (Server.Clients[clientId].udp.endPoint.Equals(clientEndPoint) && Server.Clients[clientId].RoomID == Id) //TODO: Do I really need to check for this?
            {
                //Handle Data
                int packetLength = packet.ReadInt();
                byte[] packetBytes = packet.ReadBytes(packetLength);

                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using (Packet message = new Packet(packetBytes))
                    {
                        int packetId = message.ReadInt();
                        Server.packetHandlers[packetId](clientId, message);
                    }
                });
            }*/
        }

        // Restart listening for udp data packages
        RoomUdp.BeginReceive(new AsyncCallback(ReceivedCallback), null);
    }

    private void MulticastUDPData(Packet packet)
    {
        packet.WriteLength();
        RoomUdp.Send(packet.ToArray(), packet.Length(), _remoteEndPoint);

        Debug.Log($"Multicast sent");
    }

    public void CloseRoom()
    {
        RoomUdp.DropMulticastGroup(MulticastIP); //does not work
        RoomUdp.Close();

        Debug.Log($"Room[{Id}] has been closed.");
    }
}

public enum RoomState { looking, full, playing }
