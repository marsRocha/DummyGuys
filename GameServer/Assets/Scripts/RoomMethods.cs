using System;
using UnityEngine;

public partial class Room
{
    #region Game Related
    public void StartGame()
    {
        using (Packet packet = new Packet((int)ServerPackets.startGame))
        {
            packet.Write(Id);

            MulticastUDPData(packet);
        }
        RoomState = RoomState.playing;

        Debug.Log($"Game has started on Room[{Id}]");
        RoomScene.StartRace();
    }

    public void EndGame()
    {
        using (Packet packet = new Packet((int)ServerPackets.endGame))
        {
            packet.Write(Id);

            MulticastUDPData(packet);
        }
        RoomState = RoomState.closing;

        Debug.Log($"Game has finished on Room[{Id}]. Closing room");
        CloseRoom();
    }

    public void Map(string _mapName)
    {
        using (Packet packet = new Packet((int)ServerPackets.map))
        {
            packet.Write(Id);
            packet.Write(_mapName);

            MulticastUDPData(packet);
        }
    }
    #endregion

    #region Player Related
    public int AddPlayer(Guid _clientId, string _username)
    {
        Console.WriteLine($"Player[{_clientId}] has joined the Room[{Id}]");
        int spawnId = GetServerPos();
        ClientsInfo.Add(_clientId, new ClientInfo(_clientId, _username, spawnId));
        UsedSpawnIds.Add(spawnId);

        using (Packet packet = new Packet((int)ServerPackets.playerJoined))
        {
            packet.Write(Id);
            packet.Write(_clientId);
            packet.Write(_username);
            packet.Write(spawnId);

            MulticastUDPData(packet);
        }

        if (ClientsInfo.Count >= Server.MaxPlayersPerLobby)
            RoomState = RoomState.full;

        return spawnId;
    }

    public void RemovePlayer(Guid id)
    {
        Console.WriteLine($"Player[{id}] has left the Room[{Id}]");
        UsedSpawnIds.Remove(ClientsInfo[id].spawnId);
        ClientsInfo.Remove(id);

        using (Packet packet = new Packet((int)ServerPackets.playerLeft))
        {
            packet.Write(id);

            MulticastUDPData(packet);
        }

        if (ClientsInfo.Count < Server.MaxPlayersPerLobby && RoomState == RoomState.full)
            RoomState = RoomState.looking;
    }

    public void CorrectPlayer(Guid id, SimulationState _simulationState)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerCorrection))
        {
            _packet.Write(id);

            _packet.Write(_simulationState.position);
            _packet.Write(_simulationState.velocity);
            _packet.Write(_simulationState.simulationFrame);

            MulticastUDPData(_packet);
        }
    }

    public void PlayerRespawn(Guid _id, float _game_clock)
    {
        using (Packet packet = new Packet((int)ServerPackets.playerRespawn))
        {
            packet.Write(_id);
            packet.Write(_game_clock);

            MulticastUDPData(packet);
        }
    }

    public void PlayerFinish(Guid _id, float _game_clock)
    {
        using (Packet packet = new Packet((int)ServerPackets.playerFinish))
        {
            packet.Write(_id);
            packet.Write(_game_clock);

            MulticastUDPData(packet);
        }
    }
    #endregion

    private int GetServerPos()
    {
        System.Random r = new System.Random();
        int rInt = r.Next(0, 60);

        while (UsedSpawnIds.Contains(rInt))
        {
            rInt = r.Next(0, 60);
        }

        return rInt;
    }
}
