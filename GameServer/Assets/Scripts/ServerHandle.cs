using System;
using System.Net;
using UnityEngine;

public class ServerHandle
{
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

    public static void SearchForRoom(Client _clientId)
    {
        if (Server.Rooms.Count > 0)
        {
            foreach (Room room in Server.Rooms.Values)
            {
                if (room.ClientsInfo.Count < Server.MaxPlayersPerLobby && room.RoomState == RoomState.looking)
                {
                    //Add player to room and return their spawn id
                    int spawnId = room.AddPlayer(_clientId.Id, _clientId.Username, ((IPEndPoint)_clientId.tcp.socket.Client.RemoteEndPoint).Address.ToString(),
                        ((IPEndPoint)_clientId.tcp.socket.Client.RemoteEndPoint).Port.ToString());

                    _clientId.RoomID = room.Id;
                    //Let the client know the rooms multicast info
                    ServerSend.JoinedRoom(_clientId.Id, _clientId.RoomID, room.MulticastIP.ToString(), room.MulticastPort, spawnId);
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
            int spawnId = newRoom.AddPlayer(_clientId.Id, _clientId.Username, ((IPEndPoint)_clientId.tcp.socket.Client.RemoteEndPoint).Address.ToString(),
                 ((IPEndPoint)_clientId.tcp.socket.Client.RemoteEndPoint).Port.ToString());

            _clientId.RoomID = newRoom.Id;
            //Let the client know the rooms multicast info
            ServerSend.JoinedRoom(_clientId.Id, _clientId.RoomID, newRoom.MulticastIP.ToString(), newRoom.MulticastPort, spawnId);
        }
    }

    public static void PlayerMovement(Guid _clientId, Packet _packet)
    {
        Debug.Log("Movement message");
        ClientInputState state = new ClientInputState();

        state.simulationFrame = _packet.ReadInt();

        state.HorizontalAxis = _packet.ReadInt();
        state.VerticalAxis = _packet.ReadInt();
        state.jump = _packet.ReadBool();
        state.dive = _packet.ReadBool();

        state.position = _packet.ReadVector3();
        state.rotation = _packet.ReadQuaternion();
        state.velocity = _packet.ReadVector3();
        state.angular_velocity = _packet.ReadVector3();

        //Check if player does exist
        if (!Server.Clients[_clientId].player)
            return;

        Debug.Log("got here");
        //Add new input state received
        Server.Clients[_clientId].player.AddInput(state);
        Debug.Log("got here 22222");
    }

    public static void PlayerRespawn(Guid _clientId, Packet _packet)
    {
        Debug.Log("Got anim");

        int checkPointNum = _packet.ReadInt();
        //GameManager.instance.PlayerRespawn(id, checkPointNum);
    }

    public static void PlayerFinish(Guid _clientId, Packet _packet)
    {
        float time = _packet.ReadFloat();
        //GameManager.instance.PlayerFinish(id, time);
    }

    public static void Test(Guid _clientId, Packet packet)
    {
        Console.WriteLine($"Got message from {_clientId}");
    }
}
