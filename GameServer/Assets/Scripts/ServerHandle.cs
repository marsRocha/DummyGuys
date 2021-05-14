using System;
using System.Net;
using UnityEngine;

public class ServerHandle
{
    public static void WelcomeReceived(Guid clientId, Packet packet)
    {
        Guid id = packet.ReadGuid();
        string username = packet.ReadString();
        Server.Clients[clientId].Username = username;
        //TODO: Set skin aswell

        Debug.Log($"{Server.Clients[clientId].tcp.socket.Client.RemoteEndPoint} ({username}) connected successfully and is now player {clientId}.");

        //Look for a room for player
        SearchForRoom(Server.Clients[clientId]);
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
                    ServerSend.JoinedRoom(client.Id, room.MulticastIP.ToString(), room.MulticastPort);
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
            ServerSend.JoinedRoom(client.Id, newRoom.MulticastIP.ToString(), newRoom.MulticastPort);
        }
    }

    public static void PlayerMovement(Guid clientId, Packet packet)
    {
        Vector3 position = packet.ReadVector3();
        Quaternion rotation = packet.ReadQuaternion();
        Vector3 velocity = packet.ReadVector3();
        Vector3 angular_velocity = packet.ReadVector3();
        int tick_number = packet.ReadInt();

        //Update player
    }

    public static void PlayerRespawn(Guid id, Packet packet)
    {
        Debug.Log("Got anim");

        int checkPointNum = packet.ReadInt();
        //GameManager.instance.PlayerRespawn(id, checkPointNum);
    }

    public static void PlayerFinish(Guid id, Packet packet)
    {
        float time = packet.ReadFloat();
        //GameManager.instance.PlayerFinish(id, time);
    }
}
