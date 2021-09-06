using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

public class ServerController : MonoBehaviour
{
    public static ServerController instance;

    public string ip;
    public int port;

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
    }

    // Start is called before the first frame update
    void Start()
    {
        string[] properties = ReadProperties();

        // Set properties
        Server.TICKRATE = int.Parse(properties[0]);
        Application.targetFrameRate = Server.TICKRATE;
        QualitySettings.vSyncCount = int.Parse(properties[1]);
        Server.MAX_PLAYERS_PER_ROOM = int.Parse(properties[2]);
        Server.MAX_NUMBER_ROOMS = int.Parse(properties[3]);
        Server.PLAYER_INTERACTION = bool.Parse(properties[4]);
        ip = properties[5];
        port = int.Parse(properties[6]);
        Server.ROOM_MIN_PORT = int.Parse(properties[7]);

        // Initiate server
        Server.Start(ip, port);
    }

    private void OnApplicationQuit()
    {
        Console.WriteLine("Server shutting down!");
        Server.Stop();
    }

    // Server properties
    private string[] ReadProperties()
    {
        string path = Application.dataPath + "/../server.properties";
        if (!File.Exists(path))
        {
            Console.WriteLine("No properties file found.");
            Application.Quit();
            return null;
        }

        string[] content = File.ReadAllLines(path);

        string[] values = new string[8];

        for (int i = 0; i < 10; i++)
        {
            if (i < 2)
                continue;

            values[i - 2] = Regex.Match(content[i], @"[^=]*$").Value;
        }

        return values;
    }
}
