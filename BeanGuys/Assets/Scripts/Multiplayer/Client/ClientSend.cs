using System;
using UnityEngine;

/// <summary>
/// Contains all methods to send messages to both Server and Room.
/// </summary>
public class ClientSend : MonoBehaviour
{
    #region methods of sending info
    //TODO: SUBSTITUTE THIS METHOD AND SENDUDPDATA FOR RELIABLE UDP COMMUNICATION
    /// <summary>Sends a packet to the server via TCP.</summary>
    /// <param name="_packet">The packet to send to the sever.</param>
    private static void SendTCPData(Packet _packet)
    {
        _packet.WriteLength();
        Client.instance.Server.SendData(_packet);
    }

    /// <summary>Sends a packet to the server via UDP.</summary>
    /// <param name="_packet">The packet to send to the server.</param>
    private static void SendUDPData(Packet _packet)
    {
        _packet.WriteLength();
        Client.SendUDPData(_packet);
    }

    /// <summary>Sends a packet to everyone in the Room via UDP Multicast.</summary>
    /// <param name="_packet">The packet to send to the Room.</param>
    private static void MulticastUDPData(Packet _packet)
    {
        _packet.WriteLength();
        Client.MulticastUDPData(_packet);
    }
    #endregion

    //TODO: Debug
    public static void Test()
    {
        using (Packet _packet = new Packet((int)ClientPackets.test))
        {
            _packet.Write(ClientInfo.instance.Id);

            SendUDPData(_packet);
        }
    }

    /// <summary>Lets the server know that the welcome message was received.</summary>
    public static void WelcomeReceived()
    {
        using (Packet _packet = new Packet((int)ClientPackets.welcomeReceived))
        {
            _packet.Write(ClientInfo.instance.Id);
            _packet.Write(ClientInfo.instance.Username);

            SendTCPData(_packet);
        }
    }

    public static void Ping()
    {
        using (Packet _packet = new Packet((int)ClientPackets.ping))
        {
            SendTCPData(_packet);
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
            _packet.Write(ClientInfo.instance.Id);

            SendUDPData(_packet);
        }
    }

    /// <summary>Sends player inputs to the server.</summary>
    /// <param name="_state">Inputs/State of the player.</param>
    public static void PlayerInput(ClientInputState _state)
    {
        using (Packet _packet = new Packet((int)ClientPackets.playerMovement))
        {
            _packet.Write(ClientInfo.instance.Id);

            _packet.Write(_state.Tick);

            _packet.Write(_state.HorizontalAxis);
            _packet.Write(_state.VerticalAxis);
            _packet.Write(_state.Jump);
            _packet.Write(_state.Dive);

            SendUDPData(_packet);
        }
    }



    /// <summary>Sends player inputs/state to the server.</summary>
    /// <param name="_state">Inputs/State of the player.</param>
    /// <param name="_position">Position of the player.</param>
    /// <param name="_rotation">Rotation of the player.</param>
    public static void PlayerMovement(ClientInputState _state, Vector3 _position, Quaternion _rotation, bool _ragdoll)
    {
        using (Packet _packet = new Packet((int)ClientPackets.playerMovement))
        {
            _packet.Write(ClientInfo.instance.Id);

            _packet.Write(_state.Tick);
            _packet.Write(_state.SimulationFrame);

            _packet.Write(_state.HorizontalAxis);
            _packet.Write(_state.VerticalAxis);
            _packet.Write(_state.Jump);
            _packet.Write(_state.Dive);
            _packet.Write(_state.LookingRotation);

            _packet.Write(_position);
            _packet.Write(_rotation);
            _packet.Write(_ragdoll);

            SendUDPData(_packet);
        }
    }

    /// <summary>Sends player inputs/state to the server.</summary>
    /// <param name="_state">State of the player, namely position, rotation and tick.</param>
    public static void PlayerMovement(PlayerState _state)
    {
        using (Packet _packet = new Packet((int)ClientPackets.playerMovement))
        {
            _packet.Write(ClientInfo.instance.Id);

            _packet.Write(_state.tick);
            _packet.Write(_state.position);
            _packet.Write(_state.rotation);
            _packet.Write(_state.ragdoll);
            _packet.Write(_state.animation);

            MulticastUDPData(_packet);
        }
    }
}
