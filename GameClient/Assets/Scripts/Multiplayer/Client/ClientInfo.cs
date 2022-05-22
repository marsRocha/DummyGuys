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

    public int RoomId;
    public int ClientRoomId;


    private void Awake()
    {
        instance = this;
    }

    public void SetClientName(string name)
    {
        instance.Username = name;
    }

    public void SetClientColor(int _color)
    {
        instance.Color = _color;
        GameObject.Find("SceneManager").GetComponent<MenuSceneManager>().SetColorPlayerObjs();
    }

    public void Clear()
    {
        Id = Guid.Empty;
        RoomId = 0;
        ClientRoomId = 0;
        Color = 0;
    }
}
