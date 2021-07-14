using System;
using UnityEngine;

/// <summary>
/// Contains all information about the client relative to the game.
/// </summary>
public class ClientInfo : MonoBehaviour
{
    public static ClientInfo instance;

    public Guid Id;
    public string Username;
    public int Color;

    public Guid RoomId;
    public int SpawnId;


    private void Awake()
    {
        instance = this;
    }

    public void SetClientName(string name)
    {
        Debug.Log("Username changed!");
        instance.Username = name;
    }

    public void SetClientColor(int _color)
    {
        Debug.Log("Color changed!");
        instance.Color = _color;
        GameObject.Find("SceneManager").GetComponent<MenuSceneManager>().SetColorPlayerObjs();
    }

    public void Clear()
    {
        Id = Guid.Empty;
        RoomId = Guid.Empty;
        SpawnId = 0;
        Color = 0;
    }
}
