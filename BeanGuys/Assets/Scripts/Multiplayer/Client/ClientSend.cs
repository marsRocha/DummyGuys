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
    private static void SendTCPData(Packet packet)
    {
        packet.WriteLength();
        Client.instance.Server.SendData(packet);
    }

    /// <summary>Sends a packet to the server via UDP.</summary>
    /// <param name="_packet">The packet to send to the server.</param>
    private static void SendUDPData(Packet packet)
    {
        packet.WriteLength();
        Client.SendUDPData(packet);
    }

    /// <summary>Sends a packet to everyone in the Room via UDP Multicast.</summary>
    /// <param name="_packet">The packet to send to the Room.</param>
    private static void MulticastUDPData(Packet packet)
    {
        packet.WriteLength();
        Client.MulticastUDPData(packet);
    }
    #endregion

    //TODO: Debug
    public static void Test()
    {
        using (Packet packet = new Packet((int)ClientPackets.test))
        {
            packet.Write(ClientInfo.instance.Id);

            SendUDPData(packet);
        }
    }

    /// <summary>Lets the server know that the welcome message was received.</summary>
    public static void WelcomeReceived()
    {
        using (Packet packet = new Packet((int)ClientPackets.welcomeReceived))
        {
            packet.Write(ClientInfo.instance.Id);
            packet.Write(ClientInfo.instance.Username);

            SendTCPData(packet);
        }
    }

    /// <summary>Lets the Room know that the player is ready to start the game.</summary>
    public static void PlayerReady()
    {
        Debug.Log("Player sent ready");
        using (Packet packet = new Packet((int)ClientPackets.playerReady))
        {
            packet.Write(ClientInfo.instance.Id);

            SendUDPData(packet);
        }
    }

    /// <summary>Sends player inputs/state to the server.</summary>
    /// <param name="_state">Inputs/State of the player.</param>
    /// <param name="_position">Position of the player.</param>
    /// <param name="_rotation">Rotation of the player.</param>
    public static void PlayerMovement(ClientInputState _state, Vector3 _position, Quaternion _rotation)
    {
        using (Packet packet = new Packet((int)ClientPackets.playerMovement))
        {
            packet.Write(ClientInfo.instance.Id);

            packet.Write(_state.Tick);
            packet.Write(_state.SimulationFrame);

            packet.Write(_state.HorizontalAxis);
            packet.Write(_state.VerticalAxis);
            packet.Write(_state.Jump);
            packet.Write(_state.Dive);

            packet.Write(_state.LookingRotation);
            packet.Write(_position);
            packet.Write(_rotation);

            SendUDPData(packet);
        }
    }

    /// <summary>Sends player inputs/state to the server.</summary>
    /// <param name="_state">State of the player, namely position, rotation and tick.</param>
    public static void PlayerMovement(SimulationState _state)
    {
        using (Packet packet = new Packet((int)ClientPackets.playerMovement))
        {
            packet.Write(ClientInfo.instance.Id);

            packet.Write(_state.position);
            packet.Write(_state.rotation);
            packet.Write(_state.simulationFrame);

            MulticastUDPData(packet);
        }
    }
}
