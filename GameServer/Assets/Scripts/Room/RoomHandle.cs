using System;
using System.Collections.Generic;
using UnityEngine;

public class RoomHandle
{
    public delegate void PacketHandler(Guid RoomId, Guid ClientId, Packet packet);
    public static Dictionary<int, PacketHandler> packetHandlers;

    public static void InitializeData()
    {
        packetHandlers = new Dictionary<int, PacketHandler>()
            {
                { (int)ClientPackets.playerReady, PlayerReady },
                { (int)ClientPackets.playerMovement, PlayerMovement },
                { (int)ClientPackets.playerRespawn, PlayerRespawn },
                { (int)ClientPackets.playerFinish, PlayerFinish },
                { (int)ClientPackets.playerGrab, PlayerGrab },
                { (int)ClientPackets.playerLetGo, PlayerLetGo },
                { (int)ClientPackets.playerPush, PlayerPush },
                { (int)ClientPackets.ping, Ping },
            };
    }

    public static void PlayerReady(Guid _roomId, Guid _clientId, Packet _packet)
    {
        Server.Rooms[_roomId].PlayerReady(_clientId);
    }
    
    public static void PlayerMovement(Guid _roomId, Guid _clientId, Packet _packet)
    {
        PlayerState state = new PlayerState
        {
            tick = _packet.ReadInt(),
            position = _packet.ReadVector3(),
            rotation = _packet.ReadQuaternion(),
            ragdoll = _packet.ReadBool(),
            animation = _packet.ReadInt()
        };

        //Add new input state received
        Server.Rooms[_roomId].Clients[_clientId].Player.ReceivedClientState(state);

        RoomSend.PlayerMovement(_roomId, _clientId, state);
    }

    public static void PlayerRespawn(Guid _roomId, Guid _clientId, Packet _packet)
    {
        Server.Rooms[_roomId].roomScene.PlayerRespawn(_clientId);
    }

    /// <summary> Handles request from player1 to grab player2. </summary>
    public static void PlayerGrab(Guid _roomId, Guid _clientId, Packet _packet)
    {
        Guid _playerGrabbed = _packet.ReadGuid();
        int _simulationFrame = _packet.ReadInt();

        Server.Rooms[_roomId].PlayerGrab(_clientId, _playerGrabbed,  _simulationFrame);
    }

    /// <summary> Handles request from player1 to let go of player2. </summary>
    public static void PlayerLetGo(Guid _roomId, Guid _clientId, Packet _packet)
    {
        Guid _playerFreed = _packet.ReadGuid();
        int _tick = _packet.ReadInt();

        Server.Rooms[_roomId].PlayerLetGo(_clientId, _playerFreed, _tick);
    }

    /// <summary> Handles request from player1 to puff player2. </summary>
    public static void PlayerPush (Guid _roomId, Guid _clientId, Packet _packet)
    {
        Guid _playerPushed = _packet.ReadGuid();
        int _tick = _packet.ReadInt();

        Server.Rooms[_roomId].PlayerPush(_clientId, _playerPushed, _tick);
    }

    public static void PlayerFinish(Guid _roomId, Guid _clientId, Packet _packet)
    {
        int _simulationFrame = _packet.ReadInt();

        Server.Rooms[_roomId].PlayerFinish(_clientId, _simulationFrame);
    }

    public static void Ping(Guid _roomId, Guid _clientId, Packet _packet)
    {
        RoomSend.Pong(_roomId, _clientId);

        Server.Rooms[_roomId].Clients[_clientId].Pong();
    }
}
