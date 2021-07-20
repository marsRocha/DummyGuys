﻿using UnityEngine;

public class ServerController : MonoBehaviour
{
    public static ServerController instance;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Debug.Log("Instance already exists, destroying object.");
            Destroy(this);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;

        Server.Start(26950);
    }

    private void OnApplicationQuit()
    {
        Debug.Log("Server shutting down!");
        Server.Stop();
    }
}
