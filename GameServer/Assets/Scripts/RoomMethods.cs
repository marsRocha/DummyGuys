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
    public void AddPlayer(Guid id, string username, string peerIP, string peerPort)
    {
        Debug.Log($"Player[{id}] has joined the Room[{Id}]");
        PlayersInfo.Add(id, new PlayerInfo(id, username, 0));

        using (Packet packet = new Packet((int)ServerPackets.playerJoined))
        {
            packet.Write(id);
            packet.Write(username);
            packet.Write(peerIP);
            packet.Write(peerPort);

            MulticastUDPData(packet);
        }

        if (PlayersInfo.Count >= Server.MaxPlayersPerLobby)
            RoomState = RoomState.full;
    }

    public void RemovePlayer(Guid id)
    {
        Debug.Log($"Player[{id}] has left the Room[{Id}]");
        PlayersInfo.Remove(id);

        using (Packet packet = new Packet((int)ServerPackets.playerLeft))
        {
            packet.Write(id);

            MulticastUDPData(packet);
        }

        if (PlayersInfo.Count < Server.MaxPlayersPerLobby && RoomState == RoomState.full)
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
}
