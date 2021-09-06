using TMPro;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager instance;

    public TMP_InputField ipText;
    public TMP_InputField portText;

    [Header("States")]
    public bool isOnline;
    public string ip;
    public int port;

    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
        /*isOnline = false;
        if (GameManager.instance.debug)
            GoOnline();*/
    }

    public void Connect()
    {
        ip = ipText.text;
        port = int.Parse(portText.text);
        GoOnline();
    }

    public void SetServer(string _ip)
    {
        Debug.Log("Server ip changed!");
        ip = _ip;
    }

    public void GoOnline()
    {
        Client.instance.GoOnline(ip, port);
        isOnline = true;
    }
    
    public void GoOffline()
    {
        Client.instance.Disconnect();
        isOnline = false;
    }
}
