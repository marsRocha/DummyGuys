using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientSend : MonoBehaviour
{
    #region methods of sending info

    //TODO: SUBSTITUTE THIS METHOD AND SENDUDPDATA FOR RELIABLE UDP COMMUNICATION
    private static void SendTCPDataToServer(Packet packet)
    {
        packet.WriteLength();
        Client.instance.server.SendData(packet);
    }

    private static void SendUDPData(Packet packet)
    {
        packet.WriteLength();
        Client.SendUDPData(packet);
    }

    private static void MulticastUDPData(Packet packet)
    {
        packet.WriteLength();
        Client.MulticastUDPData(packet);
    }
    #endregion

    public static void Test()
    {
        using (Packet packet = new Packet((int)ClientPackets.test))
        {
            packet.Write(Client.instance.clientInfo.id);

            SendUDPData(packet);
        }
    }

    //Send to server when it receives a welcome message
    public static void WelcomeReceived()
    {
        using (Packet packet = new Packet((int)ClientPackets.welcomeReceived))
        {
            packet.Write(Client.instance.clientInfo.id);
            packet.Write(Client.instance.clientInfo.username);
            //packet.Write(Client.MyPort);

            SendTCPDataToServer(packet);
        }
    }

    #region GameInfo
    //To server
    public static void PlayerMovement(ClientInputState clientInputState, Vector3 position, Quaternion rotation)
    {
        using (Packet packet = new Packet((int)ClientPackets.playerMovement))
        {
            packet.Write(Client.instance.clientInfo.id);

            packet.Write(clientInputState.Tick); //Global clock
            packet.Write(clientInputState.SimulationFrame); //PlayerObj tick

            packet.Write(clientInputState.HorizontalAxis);
            packet.Write(clientInputState.VerticalAxis);
            packet.Write(clientInputState.Jump);
            packet.Write(clientInputState.Dive);

            packet.Write(clientInputState.LookingRotation);
            packet.Write(position);
            packet.Write(rotation);

            SendUDPData(packet);
        }
    }

    //To peers
    public static void PlayerMovement(Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angular_velocity, float tick_number)
    {
        using (Packet packet = new Packet((int)ClientPackets.playerMovement))
        {
            packet.Write(Client.instance.clientInfo.id);

            packet.Write(position);
            packet.Write(rotation);
            packet.Write(velocity);
            packet.Write(angular_velocity);
            packet.Write(tick_number);

            MulticastUDPData(packet);
        }
    }

    public static void PlayerAnim(int anim)
    {
        using (Packet packet = new Packet((int)ClientPackets.playerAnim))
        {
            packet.Write(Client.instance.clientInfo.id);
            packet.Write(anim);

            MulticastUDPData(packet);
        }
    }

    public static void PlayerRespawn(int checkpointNum)
    {
        using (Packet packet = new Packet((int)ClientPackets.playerRespawn))
        {
            packet.Write(Client.instance.clientInfo.id);
            packet.Write(checkpointNum);

            MulticastUDPData(packet);
        }
    }

    public static void PlayerFinish(float time)
    {
        using (Packet packet = new Packet((int)ClientPackets.playerFinish))
        {
            packet.Write(Client.instance.clientInfo.id);
            packet.Write(time);

            MulticastUDPData(packet);
        }
    }
    #endregion
}
