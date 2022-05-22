using System;
using System.Collections.Generic;
using UnityEngine;

public class MapController : MonoBehaviour
{
    public static MapController instance;

#pragma warning disable 0649
    // Components
    [SerializeField]
    private PlayerCamera cam;
    [SerializeField]
    private ParticleSystem confetti;
    [SerializeField]
    private UIManager uiManager;
    [SerializeField]
    private Countdown startCountDown;
    [SerializeField]
    private Timer returnCountDown;
    [SerializeField]
    private Transform[] checkPoints;
    private Vector3[] spawns;
    [SerializeField]
    public GameLogic gameLogic;
#pragma warning restore 0649

    public Dictionary<int, RemotePlayerManager> players { get; private set; }
    public Player localPlayer { get; private set; }

    public bool isRunning { get; private set; } = false;
    private bool disconnected;

    private int qualifiedPlayers;
    private int totalPlayers;
    private bool qualified;

    #region Singleton
    private void Awake()
    {

        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            instance = this;
        }
    }
    #endregion

    public void Initialize()
    {
        gameLogic = new GameLogic();

        spawns = new Vector3[60];
        for (int i = 0; i <= 3; i++)
        {
            for(int j = 0; j <= 14; j++)
                spawns[(14 * i) + j] = new Vector3(-18.06f + 2.58f * j, 0, -2.58f * i);
        }

        players = new Dictionary<int, RemotePlayerManager>();

        isRunning = true;
        qualified = false;

        qualifiedPlayers = 0;
        totalPlayers = 0;
        SpawnPlayers();

        uiManager.Initialize();
    }

    public void InitializeDebug()
    {
        gameLogic = new GameLogic();

        spawns = new Vector3[60];
        for (int i = 0; i <= 3; i++)
        {
            for (int j = 0; j <= 14; j++)
                spawns[(15 * i) + j] = new Vector3(-18.06f + 2.58f * j, 0.5f, -2.58f * i);
        }

        players = new Dictionary<int, RemotePlayerManager>();
        SpawnPlayers();

        isRunning = true;
        qualified = false;

        qualifiedPlayers = 0;
        totalPlayers = players.Count + 1;
    }

    public void StartCountDown()
    {
        startCountDown.StartCountdown();
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            cam.StopFollowMouse();
            uiManager.OpenMenu();
        }

        if (isRunning)
        {
            gameLogic.SetClock(gameLogic.Clock + Time.deltaTime);
        }
        else
        {
            if(disconnected == true)
            {
                cam.StopFollowMouse();
                uiManager.DisconnectedMenu();
                disconnected = false;
            }
        }
    }

    private void FixedUpdate()
    {
        // Used only when player's object is deactivated due to crossing finish line,
        // in doing this we need to run the physics on another script since Playermanager no longer exists
        if (qualified)
        {
            Physics.Simulate(Time.fixedDeltaTime);
        }
    }

    // Called by countdown clock
    public void StartRace()
    {
        isRunning = true;
        localPlayer.StartPlayer();
    }

    public void StopRace()
    {
        isRunning = false;
        localPlayer.StopPlayer();
        disconnected = true;
    }

    #region Spawn Players
    public void SpawnPlayers()
    {
        SpawnLocalPlayer();
        //Spawn peers
        GameManager.instance.SpawnRemotePlayers();
    }

    public void SpawnLocalPlayer()
    {
        GameObject p = Instantiate((GameObject)Resources.Load("LocalPlayer"), spawns[ClientInfo.instance.ClientRoomId - 1], Quaternion.identity);
        p.transform.GetChild(0).GetChild(1).GetComponent<SkinnedMeshRenderer>().material = PlayerColor.instance.materials[ClientInfo.instance.Color];

        localPlayer = p.GetComponent<Player>();
        localPlayer.Initialize(ClientInfo.instance.Username, gameLogic.playerInteraction);

        cam.enabled = true;
        cam.SetFollowTargets( p.transform, p.GetComponent<PlayerController>().pelvis);
    }

    public void SpawnRemotePlayer(int _clientRoomId, string _username, int _color)
    {
        GameObject p = Instantiate((GameObject)Resources.Load("RemotePlayer"), spawns[_clientRoomId - 1], Quaternion.identity);
        p.transform.GetChild(0).GetChild(1).GetComponent<SkinnedMeshRenderer>().material = PlayerColor.instance.materials[_color];

        players.Add(_clientRoomId, p.GetComponent<RemotePlayerManager>());
        players[_clientRoomId].Initialize(_clientRoomId, _username);
    }
    #endregion

    #region Respawn Player
    public void LocalPlayerRespawn(int _checkPointNum)
    {
        Vector3 newPos = GetRespawnPosition(ClientInfo.instance.ClientRoomId, _checkPointNum);
        localPlayer.ReceivedRespawn(newPos, Quaternion.identity);
    }

    private Vector3 GetRespawnPosition(int id, int checkPointNum)
    {
        if (checkPointNum == 0)
            return spawns[id - 1];
        else
            return checkPoints[checkPointNum - 1].position;
    }
    #endregion

    public void PlayerLeft(int _clientId)
    {
        GameObject pObj = players[_clientId].gameObject;
        players.Remove(_clientId);
        Destroy(pObj);

        totalPlayers = players.Count; // update number of players remaining

        uiManager.UpdateQualifiedNum(qualifiedPlayers, totalPlayers); // updates ui
    }

    public void PlayerFinish(int _clientId)
    {
        if (_clientId == ClientInfo.instance.ClientRoomId)
        {
            qualified = true;
            confetti.Play();
            localPlayer.gameObject.SetActive(false);
            UpdateQualified();
            uiManager.OnQualified(qualifiedPlayers, totalPlayers, ClientInfo.instance.Username);

            // Enter spectate mode
            cam.StartSpectating();
        }
        else
        {
            players[_clientId].gameObject.SetActive(false);
            UpdateQualified();
            uiManager.OnQualified(qualifiedPlayers, totalPlayers, players[_clientId].Username);
        }
    }

    public void UpdateQualified()
    {
        qualifiedPlayers++;
    }

    public void EndRace()
    {
        Debug.Log("Race finished.");
        cam.StopFollowMouse();
        if (!qualified)
            uiManager.UnQualified();
        else 
            uiManager.Qualified();

        if (!GameManager.instance.debug)
        {
            returnCountDown.StartCountdown();
        }
    }

    public Transform GetPlayerTransform(int _index)
    {
        Transform player = null;
        int i = 0;
        foreach(RemotePlayerManager p in players.Values)
        {
            if (i >= _index)
            {
                player = p.transform;
                break;
            }

            i++;
        }

        return player;
    }
}

