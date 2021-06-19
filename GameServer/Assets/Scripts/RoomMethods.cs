using System;
using UnityEngine;

public partial class Room
{
    #region Game Related
    public void StartGame()
    {
        using (Packet _packet = new Packet((int)ServerPackets.startGame))
        {
            _packet.Write(RoomId);

            MulticastUDPData(_packet);
        }
        RoomState = RoomState.playing;

        Debug.Log($"Game has started on Room[{RoomId}]");
        RoomScene.StartRace();
    }

    public void EndGame()
    {
        using (Packet _packet = new Packet((int)ServerPackets.endGame))
        {
            _packet.Write(RoomId);

            MulticastUDPData(_packet);
        }
        RoomState = RoomState.closing;

        Debug.Log($"Game has finished on Room[{RoomId}]. Closing room");
        CloseRoom();
    }

    public void Map(string _mapName)
    {
        using (Packet _packet = new Packet((int)ServerPackets.map))
        {
            _packet.Write(RoomId);

            _packet.Write(_mapName);

            MulticastUDPData(_packet);
        }
    }
    #endregion

    #region Player Related
    public int AddPlayer(Guid _clientId, string _username)
    {
        Console.WriteLine($"Player[{_clientId}] has joined the Room[{RoomId}]");
        int spawnId = GetServerPos();
        ClientsInfo.Add(_clientId, new ClientInfo(_clientId, _username, spawnId));
        UsedSpawnIds.Add(spawnId);

        using (Packet _packet = new Packet((int)ServerPackets.playerJoined))
        {
            _packet.Write(RoomId);

            _packet.Write(_clientId);
            _packet.Write(_username);
            _packet.Write(spawnId);

            MulticastUDPData(_packet);
        }

        if (ClientsInfo.Count >= Server.MaxPlayersPerLobby)
            RoomState = RoomState.full;

        return spawnId;
    }

    public void RemovePlayer(Guid _clientId)
    {
        Console.WriteLine($"Player[{_clientId}] has left the Room[{RoomId}]");
        UsedSpawnIds.Remove(ClientsInfo[_clientId].spawnId);
        ClientsInfo.Remove(_clientId);

        using (Packet _packet = new Packet((int)ServerPackets.playerLeft))
        {
            _packet.Write(RoomId);

            _packet.Write(_clientId);

            MulticastUDPData(_packet);
        }

        if (ClientsInfo.Count < Server.MaxPlayersPerLobby && RoomState == RoomState.full)
            RoomState = RoomState.looking;
    }

    public void CorrectPlayer(Guid _clientId, SimulationState _simulationState)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerCorrection))
        {
            _packet.Write(RoomId);

            _packet.Write(_clientId);
            _packet.Write(_simulationState.position);
            _packet.Write(_simulationState.rotation);
            _packet.Write(_simulationState.velocity);
            _packet.Write(_simulationState.simulationFrame);

            MulticastUDPData(_packet);
        }
    }

    public void PlayerRespawn(Guid _clientId, int _checkPointNum)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerRespawn))
        {
            _packet.Write(RoomId);

            _packet.Write(_clientId);
            _packet.Write(_checkPointNum);

            MulticastUDPData(_packet);
        }
    }

    public void PlayerFinish(Guid _clientId, float _gameClock)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerFinish))
        {
            _packet.Write(RoomId);

            _packet.Write(_clientId);
            _packet.Write(_gameClock);

            MulticastUDPData(_packet);
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
