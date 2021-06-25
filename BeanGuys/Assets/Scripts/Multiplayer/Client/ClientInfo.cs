﻿using System;
using UnityEngine;

/// <summary>
/// Contains all information about the client relative to the game.
/// </summary>
public class ClientInfo : MonoBehaviour
{
    public static ClientInfo instance;

    public Guid Id;
    public string Username;

    public Guid RoomId;
    public int SpawnId;


    private void Awake()
    {
        instance = this;
    }

    public void Clear()
    {
        Id = Guid.Empty;
        RoomId = Guid.Empty;
        SpawnId = 0;
    }
}