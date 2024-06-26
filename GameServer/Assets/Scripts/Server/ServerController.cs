﻿using System;
using UnityEngine;

public class ServerController : MonoBehaviour
{
    public static ServerController instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Console.WriteLine("Instance already exists, destroying object.");
            Destroy(this);
        }

        ServerData.GetData();
    }

    // Start is called before the first frame update
    void Start()
    {
        // Initiate server
        Server.Start();
    }

    private void OnApplicationQuit()
    {
        Console.WriteLine("Server shutting down!");
        Server.Stop();
    }
}
