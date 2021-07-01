using System;

public class RoomSend
{
    public static void StartGame(Guid _roomId)
    {
        using (Packet _packet = new Packet((int)ServerPackets.startGame))
        {
            _packet.Write(_roomId);

            Server.Rooms[_roomId].MulticastUDPData(_packet);
        }
    }
    
    public static void EndGame(Guid _roomId)
    {
        using (Packet _packet = new Packet((int)ServerPackets.endGame))
        {
            _packet.Write(_roomId);

            Server.Rooms[_roomId].MulticastUDPData(_packet);
        }
    }
    
    public static void Map(Guid _roomId, string _mapName)
    {
        using (Packet _packet = new Packet((int)ServerPackets.map))
        {
            _packet.Write(_roomId);

            _packet.Write(_mapName);

            Server.Rooms[_roomId].MulticastUDPData(_packet);
        }
    }
    
    public static void NewPlayer(Guid _roomId, ClientInfo _client)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerJoined))
        {
            _packet.Write(_roomId);

            _packet.Write(_client.id);
            _packet.Write(_client.username);
            _packet.Write(_client.spawnId);

            Server.Rooms[_roomId].MulticastUDPData(_packet);
        }
    }
    
    public static void RemovePlayer(Guid _roomId, Guid _clientId)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerLeft))
        {
            _packet.Write(_roomId);

            _packet.Write(_clientId);

            Server.Rooms[_roomId].MulticastUDPData(_packet);
        }
    }
    
    public static void CorrectPlayer(Guid _roomId, Guid _clientId, SimulationState _simulationState)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerCorrection))
        {
            _packet.Write(_roomId);

            _packet.Write(_clientId);

            _packet.Write(_simulationState.simulationFrame);
            _packet.Write(_simulationState.position);
            _packet.Write(_simulationState.rotation);
            _packet.Write(_simulationState.velocity);
            _packet.Write(_simulationState.ragdoll);

            Server.Rooms[_roomId].MulticastUDPData(_packet);
        }
    }

    public static void PlayerRespawn(Guid _roomId, Guid _clientId, int _checkPointNum)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerRespawn))
        {
            _packet.Write(_roomId);

            _packet.Write(_clientId);
            _packet.Write(_checkPointNum);

            Server.Rooms[_roomId].MulticastUDPData(_packet);
        }
    }

    public static void PlayerFinish(Guid _roomId, Guid _clientId)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerFinish))
        {
            _packet.Write(_roomId);

            _packet.Write(_clientId);

            Server.Rooms[_roomId].MulticastUDPData(_packet);
        }
    }

    public static void ServerTick(Guid _roomId, int tick)
    {
        using (Packet _packet = new Packet((int)ServerPackets.serverTick))
        {
            _packet.Write(_roomId);

            _packet.Write(tick);

            Server.Rooms[_roomId].MulticastUDPData(_packet);
        }
    }
}
