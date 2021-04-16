using System;
using System.Collections.Generic;
using System.Net;
using System.Numerics;
using System.Text;

namespace Multiplayer
{
    class ServerHandle
    {
        public static void WelcomeReceived(int idFromClient, Packet packet)
        {
            int clientIdCheck = packet.ReadInt();
            string username = packet.ReadString();

            Console.WriteLine($"{Server.Clients[idFromClient].tcp.socket.Client.RemoteEndPoint} ({username}) connected successfully and is now player {idFromClient}.");
            if(idFromClient != clientIdCheck)
            {
                Console.WriteLine($"Player \"{username}\" (ID: {idFromClient}) has assumed the wrong client ID ({clientIdCheck}).");
                //TODO: disconect player?
            }

            //Look for a room for player
            SearchForRoom(Server.Clients[idFromClient]);

            //////////
            /*if (idFromClient > 1)
            {
                string peerPort = ((IPEndPoint)Server.Clients[idFromClient].tcp.socket.Client.RemoteEndPoint).Port.ToString();
                string peerIP = ((IPEndPoint)Server.Clients[idFromClient].tcp.socket.Client.RemoteEndPoint).Address.ToString();
                ServerSend.Peer(idFromClient, username, peerIP, peerPort, 1);
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
