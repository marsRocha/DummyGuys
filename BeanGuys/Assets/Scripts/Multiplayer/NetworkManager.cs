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
        if (GameManager.instance.debug)
            GoOnline();
    }

    public void SetServer(string ip)
    {
        Debug.Log("Server ip changed!");
        connectTo = ip;
    }

    public void SetClientName(string name)
    {
        Debug.Log("Username changed!");
        ClientInfo.instance.Username = name;
    }

    public void GoOnline()
    {
        Client.instance.GoOnline(connectTo, 26950);
        isOnline = true;
    }
    
    public void GoOffline()
    {
        Client.instance.Disconnect();
        isOnline = false;
    }
}
