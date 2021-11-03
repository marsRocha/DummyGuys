using TMPro;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager instance;
#pragma warning disable 0649
    [SerializeField]
    private bool clientTest;

    [SerializeField]
    private TMP_InputField ipText;
    [SerializeField]
    private TMP_InputField portText;

    [Header("States")]
    [SerializeField]
    private string ip;
    [SerializeField]
    private int port;
#pragma warning restore 0649

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        if (clientTest)
            ConnectTest();
    }

    public void Connect()
    {
        ip = ipText.text;
        port = int.Parse(portText.text);

        Client.instance.Connect(ip, port);
    }

    private void ConnectTest()
    {
        TestData.GetData();

        ip = TestData.IP;
        port = TestData.PORT;

        Client.instance.Connect(ip, port);
    }
}
