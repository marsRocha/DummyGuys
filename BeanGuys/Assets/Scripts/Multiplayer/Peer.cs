using UnityEngine;
using System;

public class Peer
{
    public Guid Id { get; private set; }
    public string Username { get; private set; }
    public int Color { get; private set; }
    public int SpawnId { get; private set; }

    public Peer(Guid _clientId, string _username, int _color, int _spawnId)
    {
        Id = _clientId;
        Username = _username;
        Color = _color;
        SpawnId = _spawnId;
    }

    /// <summary>Disconnects player from client's game.</summary>
    public void Disconnect()
    {
        Debug.Log($"Player {Id} has disconnected.");
        GameManager.instance.RemovePlayer(Id);
    }
}