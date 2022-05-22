using System.Collections.Generic;
using UnityEngine;

public class RoomHandle
{
    public delegate void PacketHandler(int RoomId, int ClientRoomId, Packet packet);
    public static Dictionary<int, PacketHandler> packetHandlers;

    public static void InitializeData()
    {
        packetHandlers = new Dictionary<int, PacketHandler>()
            {
                { (int)ClientPackets.playerReady, PlayerReady },
                { (int)ClientPackets.playerMovement, PlayerMovement },
                { (int)ClientPackets.playerRespawn, PlayerRespawn },
                { (int)ClientPackets.playerGrab, PlayerGrab },
                { (int)ClientPackets.playerLetGo, PlayerLetGo },
                { (int)ClientPackets.playerPush, PlayerPush },
                { (int)ClientPackets.ping, Ping },
            };
    }

    public static void PlayerReady(int _roomId, int _clientRoomId, Packet _packet)
    {
        Server.Rooms[_roomId].Clients[_clientRoomId].Ping();

        Server.Rooms[_roomId].PlayerReady(_clientRoomId);
    }
    
    public static void PlayerMovement(int _roomId, int _clientRoomId, Packet _packet)
    {
        Server.Rooms[_roomId].Clients[_clientRoomId].Ping();

        PlayerState state = new PlayerState
        {
            tick = _packet.GetInt(),
            position = _packet.GetVector3(),
            rotation = _packet.GetQuaternion(),
            ragdoll = _packet.GetBool(),
            animation = _packet.GetInt()
        };

        //Add new input state received
        Server.Rooms[_roomId].Clients[_clientRoomId].Player.ReceivedClientState(state);
    }

    public static void PlayerRespawn(int _roomId, int _clientRoomId, Packet _packet)
    {
        Server.Rooms[_roomId].Clients[_clientRoomId].Ping();

        Server.Rooms[_roomId].roomScene.PlayerRespawn(_clientRoomId);
    }

    /// <summary> Handles request from player1 to grab player2. </summary>
    public static void PlayerGrab(int _roomId, int _clientRoomId, Packet _packet)
    {
        Server.Rooms[_roomId].Clients[_clientRoomId].Ping();

        int _tick = _packet.GetInt();

        Server.Rooms[_roomId].PlayerGrab(_clientRoomId, _tick);
    }

    /// <summary> Handles request from player1 to let go of player2. </summary>
    public static void PlayerLetGo(int _roomId, int _clientRoomId, Packet _packet)
    {
        Server.Rooms[_roomId].Clients[_clientRoomId].Ping();

        int _playerFreed = _packet.GetInt();

        Server.Rooms[_roomId].PlayerLetGo(_clientRoomId, _playerFreed);
    }

    /// <summary> Handles request from player1 to puff player2. </summary>
    public static void PlayerPush (int _roomId, int _clientRoomId, Packet _packet)
    {
        Server.Rooms[_roomId].Clients[_clientRoomId].Ping();

        int _tick = _packet.GetInt();

        Server.Rooms[_roomId].PlayerPush(_clientRoomId, _tick);
    }

    public static void Ping(int _roomId, int _clientRoomId, Packet _packet)
    {
        //string dt = _packet.GetString();
        Server.Rooms[_roomId].Clients[_clientRoomId].Ping();

        RoomSend.Pong(_roomId, _clientRoomId);
    }
}
