using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Common;
using System.Net.Sockets;
using System.Net;
using System;
using Newtonsoft.Json;
using System.Text;
using System.Threading;

public class NetworkManager : MonoBehaviour
{
    [SerializeField]
    public Player player;
    private UdpClient client;
    private IPEndPoint endPoint, multicastEndPoint;
    private Dictionary<Guid, Player> _players;

    public bool isNull;

    //public Message message;

    private UnityMonoTaskHandler handler;

    public bool isConnected = false;

    private void Awake()
    {
        DontDestroyOnLoad(this);
    }
    // Start is called before the first frame update
    void Start()
    {
        _players = new Dictionary<Guid, Player>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            Connect();
        }
        if (isConnected)
        {
            if (player.Messages.Count > 0)
            {
                Debug.Log("playerMessages is " + player.Messages.ToString());
                HandleMessage(player.Messages.Dequeue());
                Debug.Log("After treatment: playerMessages is " + player.Messages.ToString());
            }
        }

        //Debug.Log("pla")
    }

    public void CreateServer()
    {
        System.Diagnostics.Process.Start(@"C:\Users\Marci\Desktop\CuteGame\CuteGame\Multiplayer\Multiplayer.sln");
    }

    public void Connect()
    {
        Debug.Log("Looking for game session");
        int port = 7777;
        client = new UdpClient();
        endPoint = new IPEndPoint(IPAddress.Loopback, port);
        client.Client.SendTimeout = 5000;
        client.Client.ReceiveTimeout = 5000;
        client.Connect(endPoint);

        TryToConnect("mars");
        if (isConnected)
        {
            Debug.Log("Connected");

            Player mock = player;
            _players.Add(mock.Id, mock);
            player = _players[mock.Id];
            Debug.Log("PLAYR IS :" + player);
            Debug.Log("PLAYRGAME IS :" + player.GameState);
            GameManager.instance.LoadWorldTemplate();
        }
        else
            Debug.Log("Failed to connect");
    }

    public void HandleMessage(Message message)
    {
        switch (message.MessageType)
        {
            case MessageType.PlayerJoined:
                Debug.Log("New player joined" + " ip:" + player.MulticastIP + " port:" + player.MulticastPort);
                AddPlayer(message);
                break;
            case MessageType.PlayerMovement:

                break;
        }
        player.Messages.Clear();
    }

    public void TryToConnect(string name)
    {
        player = new Player();
        player.UdpClient = client;
        player.Name = name;
        player.GameState = GameState.Connecting;
        player.Messages = new Queue<Message>();
        try
        {
            SendPlayerMessage();

            Player answerPlayer = ReceivePlayerMessage();
            Debug.Log("Received: " + answerPlayer);
            isConnected = true;
            player = answerPlayer;
        }
        catch (Exception) { }
    }

    public void SendPlayerMessage()
    {
        string playerJson = JsonConvert.SerializeObject(player);
        byte[] msg = Encoding.ASCII.GetBytes(playerJson);
        client.Send(msg, msg.Length);
    }

    public void SendPlayerMessageMulticast()
    {
        string playerJson = JsonConvert.SerializeObject(player);
        byte[] msg = Encoding.ASCII.GetBytes(playerJson);
        client.Send(msg, msg.Length);
    }

    private Player ReceivePlayerMessage()
    {
        byte[] answer = client.Receive(ref endPoint);
        string answerJson = Encoding.ASCII.GetString(answer);
        return JsonConvert.DeserializeObject<Player>(answerJson);
    }

    private Message ReceiveMessage()
    {
        byte[] answer = client.Receive(ref endPoint);
        string answerJson = Encoding.ASCII.GetString(answer);
        return JsonConvert.DeserializeObject<Message>(answerJson);
    }

    public void ChangeState(GameState state)
    {
        player.GameState = state;
    }

    public void ProcessMovement(Message m)
    {
        handler.Move(m, player.Id, _players);
    }

    public void SyncUp()
    {
        Debug.Log("Start Sinc up");
        player.GameState = GameState.Sync;
        SendPlayerMessage();

        Message answer = ReceiveMessage();
        AddPlayers(answer);
        //After all info is loaded
        ReadyToJoin();
    }

    public void ReadyToJoin()
    {
        InitiateListener();
        player.GameState = GameState.GameSync;
        Message message = new Message();
        message.MessageType = MessageType.PlayerJoined;
        message.Description = JsonConvert.SerializeObject(player);
        player.Messages.Enqueue(message);
        SendPlayerMessageMulticast();
    }

    private void AddPlayer(Message message)
    {
        Player player = JsonConvert.DeserializeObject<Player>(message.Description);
        if (!_players.ContainsKey(player.Id))
            _players.Add(player.Id, player);
        else
            _players[player.Id] = player;
        SpawnPlayer(player);
    }

    private void AddPlayers(Message message)
    {
        Debug.Log("MESSAGE DESCRIPTION:" + message.Description);
        Dictionary<Guid, Player> players = JsonConvert.DeserializeObject<Dictionary<Guid, Player>>(message.Description);
        Debug.Log("players cound: " +players.Count);
        Debug.Log("players values: " +players.Values);
        foreach (Player p in players.Values)
        {
            if (p != null && p.GameState == GameState.GameSync && !_players.ContainsKey(p.Id))
            {
                _players.Add(p.Id, p);
                SpawnPlayer(p);
            }
        }
    }

    private void SpawnPlayer(Player p)
    {
        Debug.Log("Spawning player");
        if (p.Id == player.Id)
            GameManager.instance.SpawnPlayer();
        else
            GameManager.instance.SpawnSlave();
    }

    private void InitiateListener()
    {
        Thread thread = new Thread(new ThreadStart(Listener));
        thread.Start();
    }

    private void Listener()
    {
        client.Close();
        client = new UdpClient();
        multicastEndPoint = new IPEndPoint(IPAddress.Any, player.MulticastPort);
        client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        client.Client.Bind(multicastEndPoint);
        client.JoinMulticastGroup(IPAddress.Parse(player.MulticastIP));
        Debug.Log("Joined Multicast");

        while (true)
        {
            try
            {
                byte[] msg = client.Receive(ref multicastEndPoint);
                string msgJson = Encoding.ASCII.GetString(msg);

                Message message = JsonConvert.DeserializeObject<Message>(msgJson);
                if (message != null)
                {
                    Debug.Log("NEW PLAYER MESSAGE ADDED");
                    player.Messages.Enqueue(message);
                }

            }
            catch (Exception e)
            {
                //Debug.Log(e.StackTrace);
            }
        }
    }
}