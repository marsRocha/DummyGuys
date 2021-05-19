using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInfo
{
    public Guid Id;
    public string Username;
    public int SpawnPos;

    public PlayerInfo(Guid _id, string _username, int _spawnPos)
    {
        Id = _id;
        Username = _username;
        SpawnPos = _spawnPos;
    }
}
