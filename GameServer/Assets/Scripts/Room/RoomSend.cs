using System;

public class RoomSend
{
    #region Methods of sending data
    private static void SendTCPData(int _roomId, int _toClient, Packet _packet)
    {
        _packet.WriteLength();
        Server.Rooms[_roomId].Clients[_toClient].tcp.SendData(_packet);
    }

    private static void SendTCPDataToAll(int _roomId, Packet _packet)
    {
        _packet.WriteLength();

        foreach (Client client in Server.Rooms[_roomId].Clients.Values)
        {
            client.tcp.SendData(_packet);
        }
    }

    private static void SendUDPData(int _roomId, int _toClient, Packet _packet)
    {
        _packet.WriteLength();
        Server.Rooms[_roomId].Clients[_toClient].udp.SendData(_packet);
    }

    private static void SendUDPDataToAll(int _roomId, Packet _packet)
    {
        _packet.WriteLength();

        foreach (Client client in Server.Rooms[_roomId].Clients.Values)
        {
            client.udp.SendData(_packet);
        }
    }

    private static void SendUDPDataToAll(int _roomId, int _clientId, Packet _packet)
    {
        _packet.WriteLength();

        foreach (Client client in Server.Rooms[_roomId].Clients.Values)
        {
            if (client.ClientRoomId == _clientId)
                continue;

            client.udp.SendData(_packet);
        }
    }

    public static void MulticastUDPData(int _roomId, Packet _packet)
    {
        _packet.WriteLength();
        Server.Rooms[_roomId].multicastUDP.MulticastUDPData(_packet);

        //SendUDPDataToAll(_roomId, _packet);
    }
    #endregion

    public static void StartGame(int _roomId)
    {
        using (Packet _packet = new Packet((int)ServerPackets.startGame))
        {
            _packet.Add(_roomId);

            SendTCPDataToAll(_roomId, _packet);
        }
    }

    public static void EndGame(int _roomId)
    {
        using (Packet _packet = new Packet((int)ServerPackets.endGame))
        {
            _packet.Add(_roomId);

            SendTCPDataToAll(_roomId, _packet);
        }
    }
    
    public static void Map(int _roomId, int _mapIndex)
    {
        using (Packet _packet = new Packet((int)ServerPackets.map))
        {
            _packet.Add(_roomId);

            _packet.Add(_mapIndex);

            SendTCPDataToAll(_roomId, _packet);
        }
    }

    public static void JoinedRoom(int _roomId, string _lobbyIP, int _lobbyPort, int _clientRoomId, int _tickrate)
    {
        using (Packet _packet = new Packet((int)ServerPackets.joinedRoom))
        {
            _packet.Add(_roomId);

            _packet.Add(_lobbyIP);
            _packet.Add(_lobbyPort);
            _packet.Add(_clientRoomId);

            // Room configurations
            _packet.Add(_tickrate);
            _packet.Add(ServerData.PLAYER_INTERACTION);

            SendTCPData(_roomId, _clientRoomId, _packet);
        }
    }

    public static void PlayersInRoom(int _roomId, int _toClient)
    {
        foreach(Client c in Server.Rooms[_roomId].Clients.Values)
        {
            if (c.ClientRoomId == _toClient)
                continue;

            using (Packet _packet = new Packet((int)ServerPackets.playerJoined))
            {
                _packet.Add(_roomId);

                _packet.Add(c.Id);
                _packet.Add(c.ClientRoomId);
                _packet.Add(c.Username);
                _packet.Add(c.Color);

                SendTCPData(_roomId, _toClient, _packet);
            }
        }
    }

    public static void NewPlayer(int _roomId, Guid _clientId, int _clientRoomId, string _username, int _color)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerJoined))
        {
            _packet.Add(_roomId);

            _packet.Add(_clientId.ToString("N"));
            _packet.Add(_clientRoomId);
            _packet.Add(_username);
            _packet.Add(_color);

            SendTCPDataToAll(_roomId, _packet);
        }
    }
    
    public static void PlayerLeft(int _roomId, int _clientId)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerLeft))
        {
            _packet.Add(_roomId);

            _packet.Add(_clientId);

            SendTCPDataToAll(_roomId, _packet);
        }
    }
    
    public static void CorrectPlayer(int _roomId, int _clientRoomId, PlayerState _playerState)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerCorrection))
        {
            _packet.Add(_roomId);

            _packet.Add(_clientRoomId);

            _packet.Add(_playerState.tick);
            _packet.Add(_playerState.position);
            _packet.Add(_playerState.rotation);
            _packet.Add(_playerState.ragdoll);
            _packet.Add(_playerState.animation);

            SendUDPData(_roomId, _clientRoomId, _packet);
        }
    }

    public static void PlayerRespawn(int _roomId, int _clientRoomId, int _checkPointNum)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerRespawn))
        {
            _packet.Add(_roomId);

            _packet.Add(_clientRoomId);
            _packet.Add(_checkPointNum);

            SendTCPData(_roomId, _clientRoomId, _packet);
        }
    }

    public static void PlayerGrab(int _roomId, int _grabber, int _grabbed)
    {
        // Send message to the grabber
        using (Packet _packet = new Packet((int)ServerPackets.playerGrab))
        {
            _packet.Add(_roomId);

            _packet.Add(_grabber);
            _packet.Add(_grabbed);

            SendUDPData(_roomId, _grabber, _packet);
        }

        // Send message to the grabbed
        using (Packet _packet = new Packet((int)ServerPackets.playerGrab))
        {
            _packet.Add(_roomId);

            _packet.Add(_grabber);
            _packet.Add(_grabbed);

            SendUDPData(_roomId, _grabbed, _packet);
        }
    }

    public static void PlayerLetGo(int _roomId, int _grabber, int _grabbed)
    {
        // Send message to the grabber
        using (Packet _packet = new Packet((int)ServerPackets.playerLetGo))
        {
            _packet.Add(_roomId);

            _packet.Add(_grabber);
            _packet.Add(_grabbed);

            SendUDPData(_roomId, _grabber, _packet);
        }

        // Send message to the grabbed
        using (Packet _packet = new Packet((int)ServerPackets.playerLetGo))
        {
            _packet.Add(_roomId);

            _packet.Add(_grabber);
            _packet.Add(_grabbed);

            SendUDPData(_roomId, _grabbed, _packet);
        }
    }

    public static void PlayerPush(int _roomId, int _pusher, int _pushed)
    {
        // Send message to the pusher
        using (Packet _packet = new Packet((int)ServerPackets.playerPush))
        {
            _packet.Add(_roomId);

            _packet.Add(_pusher);
            _packet.Add(_pushed);

            SendUDPData(_roomId, _pusher, _packet);
        }

        // Send message to the pushed
        using (Packet _packet = new Packet((int)ServerPackets.playerPush))
        {
            _packet.Add(_roomId);

            _packet.Add(_pusher);
            _packet.Add(_pushed);

            SendUDPData(_roomId, _pushed, _packet);
        }
    }

    public static void PlayerFinish(int _roomId, int _clientRoomId)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerFinish))
        {
            _packet.Add(_roomId);

            _packet.Add(_clientRoomId);

            SendTCPDataToAll(_roomId, _packet);
        }
    }

    public static void ServerTick(int _roomId, int _tick)
    {
        using (Packet _packet = new Packet((int)ServerPackets.serverTick))
        {
            _packet.Add(_roomId);

            _packet.Add(_tick);

            MulticastUDPData(_roomId, _packet);
        }
    }

    public static void ServerClock(int _roomId, float _clock)
    {
        using (Packet _packet = new Packet((int)ServerPackets.serverClock))
        {
            _packet.Add(_roomId);

            _packet.Add(_clock);

            MulticastUDPData(_roomId, _packet);
        }
    }

    public static void Disconnected(int _roomId)
    {
        using (Packet _packet = new Packet((int)ServerPackets.disconnected))
        {
            _packet.Add(_roomId);

            SendTCPDataToAll(_roomId, _packet);
        }
    }

    public static void Pong(int _roomId, int _toClient)
    {
        using (Packet _packet = new Packet((int)ServerPackets.pong))
        {
            //_packet.Add(_dt);
            SendUDPData(_roomId, _toClient, _packet);
        }
    }
}
