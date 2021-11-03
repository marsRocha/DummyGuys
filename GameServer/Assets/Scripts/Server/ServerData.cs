using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

public class ServerData
{
    public static int TICKRATE { get; private set; }
    public static int MAX_ROOM_PLAYERS { get; private set; }
    public static int MAX_ROOMS { get; private set; }
    public static bool PLAYER_INTERACTION { get; private set; }
    public static string IP { get; private set; }
    public static int PORT { get; private set; }
    public static int ROOM_MIN_PORT { get; private set; }
    public static bool DEBUG { get; private set; }
    public static bool DORMENT_ROOMS { get; private set; }
    public static bool LAG_COMPENSATION { get; private set; }

    /// <summary>Gets data and sets it to corresponding variables</summary>
    public static void GetData()
    {
        string[] properties = ReadProperties();

        // Set properties
        TICKRATE = int.Parse(properties[0]);
        MAX_ROOM_PLAYERS = int.Parse(properties[2]);
        MAX_ROOMS = int.Parse(properties[3]);
        PLAYER_INTERACTION = bool.Parse(properties[4]);
        IP = properties[5];
        PORT = int.Parse(properties[6]);
        ROOM_MIN_PORT = int.Parse(properties[7]);
        DEBUG = bool.Parse(properties[8]);
        DORMENT_ROOMS = bool.Parse(properties[9]);
        LAG_COMPENSATION = bool.Parse(properties[10]);

        Application.targetFrameRate = TICKRATE;
        QualitySettings.vSyncCount = int.Parse(properties[1]);
    }

    /// <summary>Reads variable used to the condifuration of the server</summary>
    /// <returns>properties' array</returns>
    private static string[] ReadProperties()
    {
        string path = Application.dataPath + "/../server.properties";
        if (!File.Exists(path))
        {
            Console.WriteLine("No properties file found.");
            Application.Quit();
            return null;
        }

        string[] content = File.ReadAllLines(path);

        string[] values = new string[11];

        for (int i = 0; i < content.Length; i++)
        {
            if (i < 2)
                continue;
            values[i - 2] = Regex.Match(content[i], @"[^=]*$").Value;
        }

        return values;
    }
}
