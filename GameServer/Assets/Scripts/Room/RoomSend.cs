using System;

public class RoomSend
{
    #region Methods of sending data
    private static void SendTCPData(Guid _roomId, Guid _toClient, Packet _packet)
    {
        _packet.WriteLength();
        Server.Rooms[_roomId].Clients[_toClient].tcp.SendData(_packet);
    }

    private static void SendTCPDataToAll(Guid _roomId, Packet _packet)
    {
        _packet.WriteLength();

        foreach (Client client in Server.Rooms[_roomId].Clients.Values)
        {
            client.tcp.SendData(_packet);
        }
    }

    private static void SendUDPData(Guid _roomId, Guid _toClient, Packet _packet)
    {
        _packet.WriteLength();
        Server.Rooms[_roomId].Clients[_toClient].udp.SendData(_packet);
    }

    private static void SendUDPDataToAll(Guid _roomId, Packet _packet)
    {
        _packet.WriteLength();

        foreach (Client client in Server.Rooms[_roomId].Clients.Values)
        {
            client.udp.SendData(_packet);
        }
    }

    private static void SendUDPDataToAll(Guid _roomId, Guid _clientId, Packet _packet)
    {
        _packet.WriteLength();

        foreach (Client client in Server.Rooms[_roomId].Clients.Values)
        {
            if (client.Id == _clientId)
                continue;

            client.udp.SendData(_packet);
        }
    }

    public static void MulticastUDPData(Guid _roomId, Packet _packet)
    {
        _packet.WriteLength();
        Server.Rooms[_roomId].multicastUDP.MulticastUDPData(_packet);

        //SendUDPDataToAll(_roomId, _packet);
    }
    #endregion

    public static void StartGame(Guid _roomId)
    {
        using (Packet _packet = new Packet((int)ServerPackets.startGame))
        {
            _packet.Write(_roomId);

            SendTCPDataToAll(_roomId, _packet);
        }
    }

    public static void StartGameDebug(Guid _roomId)
    {
        using (Packet _packet = new Packet((int)ServerPackets.startGame))
        {
            _packet.Write(_roomId);

            MulticastUDPData(_roomId, _packet);
        }
    }

    public static void EndGame(Guid _roomId)
    {
        using (Packet _packet = new Packet((int)ServerPackets.endGame))
        {
            _packet.Write(_roomId);

            SendTCPDataToAll(_roomId, _packet);
        }
    }
    
    public static void Map(Guid _roomId, int _mapIndex)
    {
        using (Packet _packet = new Packet((int)ServerPackets.map))
        {
            _packet.Write(_roomId);

            _packet.Write(_mapIndex);

            SendTCPDataToAll(_roomId, _packet);
        }
    }

    public static void JoinedRoom(Guid _roomId, Guid _toClient, string _lobbyIP, int _lobbyPort, int _spawnId, int _tickrate)
    {
        using (Packet _packet = new Packet((int)ServerPackets.joinedRoom))
        {
            _packet.Write(_roomId);

            _packet.Write(_lobbyIP);
            _packet.Write(_lobbyPort);
            _packet.Write(_spawnId);

            // Room configurations
            _packet.Write(_tickrate);
            _packet.Write(ServerData.PLAYER_INTERACTION);

            SendTCPData(_roomId, _toClient, _packet);
        }
    }

    public static void PlayersInRoom(Guid _roomId, Guid _toClient)
    {
        foreach(Client c in Server.Rooms[_roomId].Clients.Values)
        {
            if (c.Id == _toClient)
                continue;

            using (Packet _packet = new Packet((int)ServerPackets.playerJoined))
            {
                _packet.Write(_roomId);

                _packet.Write(c.Id);
                _packet.Write(c.Username);
                _packet.Write(c.Color);
                _packet.Write(c.SpawnId);

                SendTCPData(_roomId, _toClient, _packet);
            }
        }
    }

    public static void NewPlayer(Guid _roomId, Guid _id, string _username, int _color, int _spawnId)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerJoined))
        {
            _packet.Write(_roomId); // 16

            _packet.Write(_id); // 16
            _packet.Write(_username); // 9
            _packet.Write(_color); // 4
            _packet.Write(_spawnId); // 4

            SendTCPDataToAll(_roomId, _packet);
        }
    }
    
    public static void PlayerLeft(Guid _roomId, Guid _clientId)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerLeft))
        {
            _packet.Write(_roomId);

            _packet.Write(_clientId);

            SendTCPDataToAll(_roomId, _packet);
        }
    }
    
    public static void CorrectPlayer(Guid _roomId, Guid _clientId, PlayerState _playerState)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerCorrection))
        {
            _packet.Write(_roomId);

            _packet.Write(_clientId);

            _packet.Write(_playerState.tick);
            _packet.Write(_playerState.position);
            _packet.Write(_playerState.rotation);
            _packet.Write(_playerState.ragdoll);
            _packet.Write(_playerState.animation);

            SendUDPData(_roomId, _clientId, _packet);
        }
    }

    public static void PlayerMovement(Guid _roomId, Guid _clientId, PlayerState _playerState)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerMovement))
        {
            _packet.Write(_clientId);

            _packet.Write(_playerState.tick);
            _packet.Write(_playerState.position);
            _packet.Write(_playerState.rotation);
            _packet.Write(_playerState.ragdoll);
            _packet.Write(_playerState.animation);

            SendUDPDataToAll(_roomId, _clientId, _packet);
        }
    }

    public static void PlayerRespawn(Guid _roomId, Guid _clientId, int _checkPointNum)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerRespawn))
        {
            _packet.Write(_roomId);

            _packet.Write(_clientId);
            _packet.Write(_checkPointNum);

            SendTCPData(_roomId, _clientId, _packet);
        }
    }

    public static void PlayerFinish(Guid _roomId, Guid _clientId)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerFinish))
        {
            _packet.Write(_roomId);

            _packet.Write(_clientId);

            SendTCPDataToAll(_roomId, _packet);
        }
    }

    public static void PlayerGrab(Guid _roomId, Guid _grabber, Guid _grabbed)
    {
        // Send message to the grabber
        using (Packet _packet = new Packet((int)ServerPackets.playerGrab))
        {
            _packet.Write(_roomId);

            _packet.Write(_grabber);
            _packet.Write(_grabbed);

            SendUDPData(_roomId, _grabber, _packet);
        }

        // Send message to the grabbed
        using (Packet _packet = new Packet((int)ServerPackets.playerGrab))
        {
            _packet.Write(_roomId);

            _packet.Write(_grabber);
            _packet.Write(_grabbed);

            SendUDPData(_roomId, _grabbed, _packet);
        }
    }

    public static void PlayerLetGo(Guid _roomId, Guid _grabber, Guid _grabbed)
    {
        // Send message to the grabber
        using (Packet _packet = new Packet((int)ServerPackets.playerLetGo))
        {
            _packet.Write(_roomId);

            _packet.Write(_grabber);
            _packet.Write(_grabbed);

            SendUDPData(_roomId, _grabber, _packet);
        }

        // Send message to the grabbed
        using (Packet _packet = new Packet((int)ServerPackets.playerLetGo))
        {
            _packet.Write(_roomId);

            _packet.Write(_grabber);
            _packet.Write(_grabbed);

            SendUDPData(_roomId, _grabbed, _packet);
        }
    }

    public static void PlayerPush(Guid _roomId, Guid _pusher, Guid _pushed)
    {
        // Send message to the pusher
        using (Packet _packet = new Packet((int)ServerPackets.playerPush))
        {
            _packet.Write(_roomId);

            _packet.Write(_pusher);
            _packet.Write(_pushed);

            SendUDPData(_roomId, _pusher, _packet);
        }

        // Send message to the pushed
        using (Packet _packet = new Packet((int)ServerPackets.playerPush))
        {
            _packet.Write(_roomId);

            _packet.Write(_pusher);
            _packet.Write(_pushed);

            SendUDPData(_roomId, _pushed, _packet);
        }
    }

    public static void ServerTick(Guid _roomId, int _tick, float _clock)
    {
        using (Packet _packet = new Packet((int)ServerPackets.serverTick))
        {
            _packet.Write(_roomId);

            _packet.Write(_tick);
            _packet.Write(_clock);

            MulticastUDPData(_roomId, _packet);
        }
    }

    public static void Disconnected(Guid _roomId)
    {
        using (Packet _packet = new Packet((int)ServerPackets.disconnected))
        {
            _packet.Write(_roomId);

            SendTCPDataToAll(_roomId, _packet);
        }
    }

    public static void Pong(Guid _roomId, Guid _toClient)
    {
        using (Packet _packet = new Packet((int)ServerPackets.pong))
        {
            SendUDPData( _roomId, _toClient, _packet);
        }
    }
}
