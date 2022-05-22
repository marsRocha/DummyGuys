using System;

public class Peer
{
    public Guid Id { get; private set; }
    public string Username { get; private set; }
    public int Color { get; private set; }
    public int ClientRoomId { get; private set; }

    /// <summary>Constructer to Peer class.</summary>
    public Peer(Guid _clientId, int _clientRoomId, string _username, int _color)
    {
        Id = _clientId;
        Username = _username;
        Color = _color;
        ClientRoomId = _clientRoomId;
    }
}