// Server only uses this class to clients that are not in a room
public class ServerSend
{
    private static void SendTCPData(NewConnection _connection, Packet _packet)
    {
        _packet.WriteLength();
        _connection.SendData(_packet);
    }

    public static void Welcome(NewConnection _toClient)
    {
        using (Packet packet = new Packet((int)ServerPackets.welcome))
        {
            SendTCPData(_toClient, packet);
        }
    }
}
