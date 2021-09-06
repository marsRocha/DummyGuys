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
    }

    /// <summary>Sends a packet to the Room via UDP.</summary>
    /// <param name="_packet">The packet to send to the server.</param>
    private static void SendUDPData(Packet _packet)
    {
        if (Client.instance.udp.socket == null)
            return;

        _packet.WriteLength();
        Client.instance.udp.SendData(_packet);
    }

    /// <summary>Sends a packet to everyone in the Room via UDP Multicast.</summary>
    /// <param name="_packet">The packet to send to the Room.</param>
    private static void MulticastUDPData(Packet _packet)
    {
        if (Client.instance.multicast.socket == null)
            return;

        //_packet.WriteLength();
        //Client.instance.multicast.SendData(_packet);
        SendUDPData(_packet);
    }
    #endregion

    /// <summary>Lets the server know that the welcome message was received.</summary>
    public static void Introduction()
    {
        using (Packet _packet = new Packet((int)ClientPackets.introduction))
        {
            _packet.Write(ClientInfo.instance.Id);
            _packet.Write(ClientInfo.instance.Username);
            _packet.Write(ClientInfo.instance.Color);

            SendTCPData(_packet);
        }

        Analytics.bandwidthUp += 37;
        Analytics.packetsUp++;
    }

    public static void Ping()
    {
        using (Packet _packet = new Packet((int)ClientPackets.ping))
        {
            _packet.Write(ClientInfo.instance.Id);

            SendUDPData(_packet);
        }
        // We send the client ping packet and set pingSent to now
        Client.instance.pingSent = DateTime.UtcNow;

        Analytics.bandwidthUp += 8;
        Analytics.packetsUp++;
    }

    /// <summary>Lets the Room know that the player is ready to start the game.</summary>
    public static void PlayerReady()
    {
        Debug.Log("Player sent ready");
        using (Packet _packet = new Packet((int)ClientPackets.playerReady))
        {
            _packet.Write(ClientInfo.instance.Id);

            SendTCPData(_packet);
        }

        Analytics.bandwidthUp += 24;
        Analytics.packetsUp++;
    }

    /// <summary>Sends player state to the server.</summary>
    /// <param name="_state">State of the player, namely position, rotation and tick.</param>
    public static void PlayerMovement(PlayerState _state)
    {
        using (Packet _packet = new Packet((int)ClientPackets.playerMovement))
        {
            _packet.Write(ClientInfo.instance.Id); // GUID

            _packet.Write(_state.tick); // int
            _packet.Write(_state.position); // Vector3
            _packet.Write(_state.rotation); // Quaternion
            _packet.Write(_state.ragdoll); // Bool
            _packet.Write(_state.animation); // int

            MulticastUDPData(_packet);
        }

        Analytics.bandwidthUp += 45;
        Analytics.packetsUp++;
    }

    /// <summary>Sends player respawn resquest to the server.</summary>
    public static void PlayerRespawn()
    {
        using (Packet _packet = new Packet((int)ClientPackets.playerRespawn))
        {
            _packet.Write(ClientInfo.instance.Id);

            SendTCPData(_packet);
        }

        Analytics.bandwidthUp += 24;
        Analytics.packetsUp++;
    }

    /// <summary>Sends player grab resquest to the server.</summary>
    public static void PlayerGrab(Guid _playerGrabbed, int _tick)
    {
        using (Packet _packet = new Packet((int)ClientPackets.playerGrab))
        {
            _packet.Write(ClientInfo.instance.Id);
            _packet.Write(_playerGrabbed);
            _packet.Write(_tick);

            MulticastUDPData(_packet);
        }

        Analytics.bandwidthUp += 24;
        Analytics.packetsUp++;
    }

    /// <summary>Sends player let go of grab resquest to the server.</summary>
    public static void PlayerLetGo(Guid _playerFreed, int _tick)
    {
        using (Packet _packet = new Packet((int)ClientPackets.playerLetGo))
        {
            _packet.Write(ClientInfo.instance.Id);
            _packet.Write(_playerFreed);
            _packet.Write(_tick);

            MulticastUDPData(_packet);
        }

        Analytics.bandwidthUp += 24;
        Analytics.packetsUp++;
    }

    /// <summary>Sends player grab resquest to the server.</summary>
    public static void PlayerPush(Guid _playerPushed, int _tick)
    {
        using (Packet _packet = new Packet((int)ClientPackets.playerPush))
        {
            _packet.Write(ClientInfo.instance.Id);
            _packet.Write(_playerPushed);
            _packet.Write(_tick);

            MulticastUDPData(_packet);
        }

        Analytics.bandwidthUp += 24;
        Analytics.packetsUp++;
    }
}
