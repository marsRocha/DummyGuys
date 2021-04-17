using System;
using System.Collections.Generic;
using System.Net;
using System.Numerics;
using System.Text;

namespace Multiplayer
{
    class ServerHandle
    {
        public static void WelcomeReceived(Guid idFromClient, Packet packet)
        {
            Guid id = packet.ReadGuid();
            string username = packet.ReadString();
            Server.Clients[idFromClient].Username = username;
            //TODO: Set skin aswell

            Console.WriteLine($"{Server.Clients[idFromClient].tcp.socket.Client.RemoteEndPoint} ({username}) connected successfully and is now player {idFromClient}.");

            //Look for a room for player
            SearchForRoom(Server.Clients[idFromClient]);

            //////////
            /*if (Server.Clients.Count > 1)
            {
                string peerPort = ((IPEndPoint)Server.Clients[idFromClient].tcp.socket.Client.RemoteEndPoint).Port.ToString();
                string peerIP = ((IPEndPoint)Server.Clients[idFromClient].tcp.socket.Client.RemoteEndPoint).Address.ToString();
                foreach(Client client in Server.Clients.Values)
                {
                    ServerSend.Peer(client.Id, Server.Clients[idFromClient].Id, Server.Clients[idFromClient].Username, peerIP, peerPort);
                    break;
                }
                Console.WriteLine("Sent peer");
            }*/
        }

        public static void SearchForRoom(Client client)
        {
            if (Server.Rooms.Count > 0)
            {
                foreach (Room room in Server.Rooms.Values)
                {
                    if (room.Players.Count < Server.MaxPlayersPerLobby && room.RoomState == RoomState.looking)
                    {
                        //Add player to it
                        room.AddPlayer(client.Id, client.Username, ((IPEndPoint)client.tcp.socket.Client.RemoteEndPoint).Address.ToString(),
                            ((IPEndPoint)client.tcp.socket.Client.RemoteEndPoint).Port.ToString());

                        client.RoomID = room.Id;
                        //Let the client know the rooms multicast info
                        ServerSend.JoinedRoom(client.Id, room.MulticastIP, room.MulticastPort);
                    }
                }
            }
            else
            {
                //Create new room
                Guid newGuid = Guid.NewGuid();
                Server.Rooms.Add(newGuid, new Room(newGuid, Server.GetNextAdress(), Server.multicastPort));
                Room newRoom = Server.Rooms[newGuid];

                //Add player to it
                newRoom.AddPlayer(client.Id, client.Username, ((IPEndPoint)client.tcp.socket.Client.RemoteEndPoint).Address.ToString(),
                     ((IPEndPoint)client.tcp.socket.Client.RemoteEndPoint).Port.ToString());

                client.RoomID = newRoom.Id;
                //Let the client know the rooms multicast info
                ServerSend.JoinedRoom(client.Id, newRoom.MulticastIP, newRoom.MulticastPort);
            }
        }
    }
}
