// Server only uses this class to clients that are not in a room
public class ServerSend
{
    private static void SendTCPData(NewConnection _connection, Packet _packet)
    {
        _packet.WriteLength();
        _connection.SendData(_packet);
    }

    /// <summary> Sends message accepting new connection. </summary>
    /// <param name="_toClient"></param>
    public static void AcceptConnection(NewConnection _toClient)
    {
        using (Packet packet = new Packet((int)ServerPackets.accept))
        {
            SendTCPData(_toClient, packet);
        }
    }

    /// <summary> Sends message refusing new connection. </summary>
    /// <param name="_toClient"></param>
    public static void RefuseConnection(NewConnection _toClient)
    {
        using (Packet packet = new Packet((int)ServerPackets.refuse))
        {
            SendTCPData(_toClient, packet);
        }
    }
}
