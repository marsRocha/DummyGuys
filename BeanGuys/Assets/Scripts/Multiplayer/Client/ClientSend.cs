using System;
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

        Debug.Log(_packet.GetByteLength());

        Analytics.bandwidthUp += _packet.GetByteLength();
        Analytics.packetsUp++;
    }
    #endregion

    /// <summary>Lets the server know that the welcome message was received.</summary>
    public static void Introduction()
    {
        using (Packet _packet = new Packet((int)ClientPackets.introduction))
        {
            _packet.Write(ClientInfo.instance.Id.ToString("N"));
            _packet.Write(ClientInfo.instance.Username);
            _packet.Write(ClientInfo.instance.Color);

            SendTCPData(_packet);
        }
    }

    public static void Ping()
    {
        using (Packet _packet = new Packet((int)ClientPackets.ping))
        {
            _packet.Write(ClientInfo.instance.Id.ToString("N"));

            SendUDPData(_packet);
        }
        // We send the client ping packet and set pingSent to now
        Client.instance.pingSent = DateTime.UtcNow;
    }

    /// <summary>Lets the Room know that the player is ready to start the game.</summary>
    public static void PlayerReady()
    {
        Debug.Log("Player sent ready");
        using (Packet _packet = new Packet((int)ClientPackets.playerReady))
        {
            _packet.Write(ClientInfo.instance.Id.ToString("N"));

            SendTCPData(_packet);
        }
    }

    /// <summary>Create & Send player state to the server.</summary>
    /// <param name="_state">State of the player, namely position, rotation and tick.</param>
    public static void PlayerMovement(PlayerState _state)
    {
        using (Packet _packet = new Packet((int)ClientPackets.playerMovement))
        {
            _packet.Write(ClientInfo.instance.Id.ToString("N")); // GUID

            _packet.Write(_state.tick); // int
            _packet.Write(_state.position); // Vector3
            _packet.Write(_state.rotation); // Quaternion
            _packet.Write(_state.ragdoll); // Bool
            _packet.Write(_state.animation); // int

            MulticastUDPData(_packet);
        }
    }

    /// <summary>Sends player respawn resquest to the server.</summary>
    public static void PlayerRespawn()
    {
        using (Packet _packet = new Packet((int)ClientPackets.playerRespawn))
        {
            _packet.Write(ClientInfo.instance.Id.ToString("N"));

            SendTCPData(_packet);
        }
    }

    /// <summary>Sends player grab resquest to the server.</summary>
    public static void PlayerGrab( int _tick)
    {
        using (Packet _packet = new Packet((int)ClientPackets.playerGrab))
        {
            _packet.Write(ClientInfo.instance.Id.ToString("N"));
            _packet.Write(_tick);

            SendTCPData(_packet);
        }
    }

    /// <summary>Sends player let go of grab resquest to the server.</summary>
    public static void PlayerLetGo(Guid _playerFreed)
    {
        using (Packet _packet = new Packet((int)ClientPackets.playerLetGo))
        {
            _packet.Write(ClientInfo.instance.Id.ToString("N"));
            _packet.Write(_playerFreed);

            SendTCPData(_packet);
        }
    }

    /// <summary>Sends player grab resquest to the server.</summary>
    public static void PlayerPush(int _tick)
    {
        using (Packet _packet = new Packet((int)ClientPackets.playerPush))
        {
            _packet.Write(ClientInfo.instance.Id.ToString("N"));
            _packet.Write(_tick);

            SendTCPData(_packet);
        }
    }
}
