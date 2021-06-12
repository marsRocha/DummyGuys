using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System;

public class Peer
{
    public Guid Id { get; private set; }
    public string Username { get; private set; }
    public int SpawnId { get; private set; }

    public Peer(Guid _clientId, string _username, int _spawnId)
    {
        Id = _clientId;
        Username = _username;
        SpawnId = _spawnId;
    }

    public void Disconnect()
    {
        Debug.Log($"Player {Id} has disconnected.");
        GameManager.instance.RemovePlayer(Id);
    }
}