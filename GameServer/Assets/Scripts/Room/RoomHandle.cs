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
            };
    }

    public static void PlayerReady(Guid _roomId, Guid _clientId, Packet _packet)
    {
        Server.Rooms[_roomId].PlayerReady(_clientId);
    }
    
    public static void PlayerMovement(Guid _roomId, Guid _clientId, Packet _packet)
    {
        ClientInputState state = new ClientInputState();

        state.Tick = _packet.ReadInt();
        state.SimulationFrame = _packet.ReadInt();

        state.HorizontalAxis = _packet.ReadFloat();
        state.VerticalAxis = _packet.ReadFloat();
        state.Jump = _packet.ReadBool();
        state.Dive = _packet.ReadBool();
        state.LookingRotation = _packet.ReadQuaternion();

        state.position = _packet.ReadVector3();
        state.rotation = _packet.ReadQuaternion();
        state.ragdoll = _packet.ReadBool();

        //Check if player does exist
        if (!Server.Clients[_clientId].player)
            return;

        //Add new input state received
        Server.Clients[_clientId].player.ReceivedClientState(state);
    }

    public static void PlayerRespawn(Guid _roomId, Guid _clientId, Packet _packet)
    {
        int checkPointNum = _packet.ReadInt();
        //GameManager.instance.PlayerRespawn(id, checkPointNum);
    }

    public static void PlayerFinish(Guid _roomId, Guid _clientId, Packet _packet)
    {
        int _simulationFrame = _packet.ReadInt();

        Server.Rooms[_roomId].PlayerFinish(_clientId, _simulationFrame);
    }
}
