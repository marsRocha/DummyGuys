using System;
using System.Collections.Generic;
using UnityEngine;

public class ServerHandle
{
    public delegate void PacketHandler(Guid ClientId, Packet packet);
    public static Dictionary<int, PacketHandler> packetHandlers;

    public static void InitializeData()
    {
        packetHandlers = new Dictionary<int, PacketHandler>()
            {
                { (int)ClientPackets.welcomeReceived, WelcomeReceived },
                { (int)ClientPackets.ping, Ping },
                { (int)ClientPackets.test, Test },
            };
    }

    public static void WelcomeReceived(Guid _clientId, Packet _packet)
    {
        Guid id = _packet.ReadGuid();
        string username = _packet.ReadString();
        Server.Clients[_clientId].Username = username;
        //TODO: Set skin aswell

        //Connect UDP
        //Server.Clients[clientId].udp.Connect((IPEndPoint)Server.Clients[clientId].tcp.socket.Client.RemoteEndPoint);

        Debug.Log($"{Server.Clients[_clientId].tcp.socket.Client.RemoteEndPoint} ({username}) connected successfully and is now player {_clientId}.");

        //Look for a room for player
        SearchForRoom(Server.Clients[_clientId]);
    }

    public static void SearchForRoom(Client _client)
    {
        Room foundRoom = null;

        if (Server.Rooms.Count > 0)
        {
            foreach (Room room in Server.Rooms.Values)
            {
                if (room.ClientsInfo.Count < Server.MaxPlayersPerLobby && room.RoomState == RoomState.looking)
                {
                    foundRoom = room;
                    break;
                }
            }
        }
        else
        {
            //Create new room
            Guid newGuid = Guid.NewGuid();
            Server.Rooms.Add(newGuid, new Room(newGuid, Server.GetNextAdress(), Server.multicastPort));
            foundRoom = Server.Rooms[newGuid];
        }

        //Add player to room and return their spawn id
        int spawnId = foundRoom.AddPlayer(_client.Id, _client.Username);

        _client.RoomID = foundRoom.RoomId;
        //Let the client know the rooms multicast info
        ServerSend.JoinedRoom(_client.Id, _client.RoomID, foundRoom.MulticastIP.ToString(), foundRoom.MulticastPort, spawnId);

        //Send all players inside room to new player
        foreach (ClientInfo p in foundRoom.ClientsInfo.Values)
        {
            if(p.id != _client.Id)
                ServerSend.PlayerInfo(_client.Id, foundRoom.RoomId, p);
        }
    }

    public static void Ping(Guid _clientId, Packet _packet)
    {
        ServerSend.Pong(_clientId);
    }

    public static void Test(Guid _clientId, Packet packet)
    {
        Console.WriteLine($"Got message from {_clientId}");
    }
}
