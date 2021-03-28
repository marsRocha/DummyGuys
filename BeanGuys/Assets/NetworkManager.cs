using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager instance;

    [Header("States")]
    public bool isOnline;
    public string connectTo = "127.0.0.1";

    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
        isOnline = false;
        Client.instance.GoOnline(connectTo, 26950);
        isOnline = true;
    }
}
