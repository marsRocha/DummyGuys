using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

public class TestData
{
    public static string IP { get; private set; }
    public static int PORT { get; private set; }

    /// <summary>Gets data and sets it to corresponding variables</summary>
    public static void GetData()
    {
        string[] properties = ReadProperties();

        // Set properties
        IP = properties[0];
        PORT = int.Parse(properties[1]);
    }

    /// <summary>Reads variable used to the condifuration of the client</summary>
    /// <returns>properties' array</returns>
    private static string[] ReadProperties()
    {
        string path = Application.dataPath + "/../client.properties";
        if (!File.Exists(path))
        {
            Console.WriteLine("No properties file found.");
            Application.Quit();
            return null;
        }

        string[] content = File.ReadAllLines(path);

        string[] values = new string[4];

        for (int i = 0; i < content.Length; i++)
        {
            if (i < 2)
                continue;
            values[i - 2] = Regex.Match(content[i], @"[^=]*$").Value;
        }

        return values;
    }
}
