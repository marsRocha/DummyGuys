using System;
using System.Collections.Generic;
using System.Net;
using System.Numerics;
using System.Text;

namespace Multiplayer
{
    class ServerHandle
    {
        public static void WelcomeReceived(Guid clientId, Packet packet)
        {
            Guid id = packet.ReadGuid();
            string username = packet.ReadString();
            Server.Clients[clientId].Username = username;
            //TODO: Set skin aswell

            //Connect UDP
            //Server.Clients[clientId].udp.Connect((IPEndPoint)Server.Clients[clientId].tcp.socket.Client.RemoteEndPoint);

            Console.WriteLine($"{Server.Clients[clientId].tcp.socket.Client.RemoteEndPoint} ({username}) connected successfully and is now player {clientId}.");

            //Look for a room for player
            SearchForRoom(Server.Clients[clientId]);
        }

        public static void SearchForRoom(Client client)
        {
            if (Server.Rooms.Count > 0)
            {
                foreach (Room room in Server.Rooms.Values)
                {
                    if (room.ClientsInfo.Count < Server.MaxPlayersPerLobby && room.RoomState == RoomState.looking)
                    {
                        //Add player to room and return their spawn id
                        int spawnId = room.AddPlayer(client.Id, client.Username, ((IPEndPoint)client.tcp.socket.Client.RemoteEndPoint).Address.ToString(),
                            ((IPEndPoint)client.tcp.socket.Client.RemoteEndPoint).Port.ToString());

                        client.RoomID = room.Id;
                        //Let the client know the rooms multicast info
                        ServerSend.JoinedRoom(client.Id, room.MulticastIP.ToString(), room.MulticastPort, spawnId);
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
                int spawnId = newRoom.AddPlayer(client.Id, client.Username, ((IPEndPoint)client.tcp.socket.Client.RemoteEndPoint).Address.ToString(),
                     ((IPEndPoint)client.tcp.socket.Client.RemoteEndPoint).Port.ToString());

                client.RoomID = newRoom.Id;
                //Let the client know the rooms multicast info
                ServerSend.JoinedRoom(client.Id, newRoom.MulticastIP.ToString(), newRoom.MulticastPort, spawnId);
            }
        }

        public static void PlayerMovement(Guid clientId, Packet packet)
        {
            Vector3 position = packet.ReadVector3();
            Quaternion rotation = packet.ReadQuaternion();
            Vector3 velocity = packet.ReadVector3();
            Vector3 angular_velocity = packet.ReadVector3();
            float tick_number = packet.ReadFloat();
        
            //Update player
        }        
        
        public static void StarGame(Guid clientId, Packet packet)
        {
            Console.WriteLine("Start game");
        }

        public static void PlayerRespawn(Guid id, Packet packet)
        {
            Console.WriteLine("Got anim");

            int checkPointNum = packet.ReadInt();
            //GameManager.instance.PlayerRespawn(id, checkPointNum);
        }

        public static void PlayerFinish(Guid id, Packet packet)
        {
            float time = packet.ReadFloat();
            //GameManager.instance.PlayerFinish(id, time);
        }

        public static void Test(Guid id, Packet packet)
        {
            Console.WriteLine($"Got message from {id}");
        }
    }
}
