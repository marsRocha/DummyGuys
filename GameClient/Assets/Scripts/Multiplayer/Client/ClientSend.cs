using System;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Contains all methods to send messages to both Server and Room.
/// </summary>
public class ClientSend : MonoBehaviour
{
    #region Methods of sending data
    /// <summary>Sends a packet to the server via TCP.</summary>
    /// <param name="_packet">The packet to send to the sever.</param>
    private static void SendTCPData(Packet _packet)
    {
        if (Client.instance.tcp.socket == null)
            return;

        _packet.WriteLength();
        Client.instance.tcp.SendData(_packet);

        Analytics.bandwidthUp += _packet.GetByteLength();
        Analytics.packetsUp++;
    }

    /// <summary>Sends a packet to the Room via UDP.</summary>
    /// <param name="_packet">The packet to send to the server.</param>
    private static void SendUDPData(Packet _packet)
    {
        if (Client.instance.udp.socket == null)
            return;

        _packet.WriteLength();
        Client.instance.udp.SendData(_packet);

        Analytics.bandwidthUp += _packet.GetByteLength();
        Analytics.packetsUp++;
    }

    /// <summary>Sends a packet to everyone in the Room via UDP Multicast.</summary>
    /// <param name="_packet">The packet to send to the Room.</param>
    private static void MulticastUDPData(Packet _packet)
    {
        if (Client.instance.multicast.socket == null)
            return;

        _packet.WriteLength();
        Client.instance.multicast.SendData(_packet);

        //Debug.Log(_packet.GetByteLength());

        Analytics.bandwidthUp += _packet.GetByteLength();
        Analytics.packetsUp++;
    }
    #endregion

    /// <summary>Lets the server know that the welcome message was received.</summary>
    public static void Introduction()
    {
        using (Packet _packet = new Packet((int)ClientPackets.introduction))
        {
            _packet.Add(ClientInfo.instance.Id.ToString("N"));
            _packet.Add(ClientInfo.instance.Username);
            _packet.Add(ClientInfo.instance.Color);

            SendTCPData(_packet);
        }
    }

    /// <summary>Lets the Room know that the player is ready to start the game.</summary>
    public static void PlayerReady()
    {
        using (Packet _packet = new Packet((int)ClientPackets.playerReady))
        {
            _packet.Add(ClientInfo.instance.ClientRoomId);

            SendTCPData(_packet);
        }
    }

    /// <summary>Create & Send player state to the server.</summary>
    /// <param name="_state">State of the player, namely position, rotation and tick.</param>
    public static void PlayerMovement(PlayerState _state)
    {
        using (Packet _packet = new Packet((int)ClientPackets.playerMovement))
        {
            _packet.Add(ClientInfo.instance.ClientRoomId); // int

            _packet.Add(_state.tick); // int
            _packet.Add(_state.position); // Vector3
            _packet.Add(_state.rotation); // Quaternion
            _packet.Add(_state.ragdoll); // Bool
            _packet.Add(_state.animation); // int

            MulticastUDPData(_packet);
        }
    }

    /// <summary>Sends player respawn resquest to the server.</summary>
    public static void PlayerRespawn()
    {
        using (Packet _packet = new Packet((int)ClientPackets.playerRespawn))
        {
            _packet.Add(ClientInfo.instance.ClientRoomId);

            SendTCPData(_packet);
        }
    }

    /// <summary>Sends player grab resquest to the server.</summary>
    public static void PlayerGrab( int _tick)
    {
        using (Packet _packet = new Packet((int)ClientPackets.playerGrab))
        {
            _packet.Add(ClientInfo.instance.ClientRoomId);
            _packet.Add(_tick);

            SendTCPData(_packet);
        }
    }

    /// <summary>Sends player let go of grab resquest to the server.</summary>
    public static void PlayerLetGo(int _playerFreed)
    {
        using (Packet _packet = new Packet((int)ClientPackets.playerLetGo))
        {
            _packet.Add(ClientInfo.instance.ClientRoomId);
            _packet.Add(_playerFreed);

            SendTCPData(_packet);
        }
    }

    /// <summary>Sends player grab resquest to the server.</summary>
    public static void PlayerPush(int _tick)
    {
        using (Packet _packet = new Packet((int)ClientPackets.playerPush))
        {
            _packet.Add(ClientInfo.instance.ClientRoomId);
            _packet.Add(_tick);

            SendTCPData(_packet);
        }
    }

    public static void Ping()
    {
        using (Packet _packet = new Packet((int)ClientPackets.ping))
        {
            _packet.Add(ClientInfo.instance.ClientRoomId);
            //_packet.Add(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"));

            SendUDPData(_packet);
        }
        // We send the client ping packet and set pingSent to now
        Client.instance.pingSent = DateTime.UtcNow;
        if (TestData.PING)
            Console.WriteLine("Ping sent");
    }
}
