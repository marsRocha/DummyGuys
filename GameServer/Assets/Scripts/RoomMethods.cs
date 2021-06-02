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
        StartRace();
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
    public int AddPlayer(Guid id, string username, string peerIP, string peerPort)
    {
        Console.WriteLine($"Player[{id}] has joined the Room[{Id}]");
        int spawnId = GetServerPos();
        ClientsInfo.Add(id, new ClientInfo(id, username, spawnId));
        UsedSpawnIds.Add(spawnId);

        using (Packet packet = new Packet((int)ServerPackets.playerJoined))
        {
            packet.Write(id);
            packet.Write(username);
            packet.Write(peerIP);
            packet.Write(peerPort);
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

    public void CorrectPlayer(Guid id, Vector3 Position)
    {
        using (Packet packet = new Packet((int)ServerPackets.playerCorrection))
        {
            packet.Write(id);

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
