using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapController : MonoBehaviour
{
    public static MapController instance;

    public UIManager uiManager;
    public CountDown countDown;

    public Dictionary<Guid, RemotePlayerManager> players { get; private set; }
    public PlayerManager localPlayer { get; private set; }

    [Header("Components")]
    [HideInInspector]
    public Vector3[] spawns;
    public Transform[] checkPoints;

    //Controls game
    public float Game_Clock;
    public const int TICK_PER_MS = 30;
    public bool isRunning { get; private set; } = false;


    [Header("Components")]
    public PlayerCamera camera;
    public ParticleSystem confetti;

    public int playerCheckPoint { get; private set; } = 0;
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
        DontDestroyOnLoad(this.gameObject);
    }
    #endregion

    public void Initialize()
    {
        spawns = new Vector3[60];
        for (int i = 0; i <= 3; i++)
        {
            for(int j = 0; j <= 14; j++)
                spawns[(14 * i) + j] = new Vector3(-18.06f + 2.58f * j, 0,-2.58f * i);
        }


        players = new Dictionary<Guid, RemotePlayerManager>();

        Game_Clock = 0;
        isRunning = true;
        qualified = false;

        qualifiedPlayers = 0;
        totalPlayers = 0;
        SpawnPlayers();

        uiManager.Initialize();
    }

    public void InitializeDebug()
    {
        spawns = new Vector3[60];
        for (int i = 0; i <= 3; i++)
        {
            for (int j = 0; j <= 14; j++)
                spawns[(14 * i) + j] = new Vector3(-18.06f + 2.58f * j, 0, -2.58f * i);
        }

        players = new Dictionary<Guid, RemotePlayerManager>();

        Game_Clock = 0;
        isRunning = true;
        qualified = false;

        qualifiedPlayers = 0;
        totalPlayers = 0;
        SpawnPlayers();
    }

    public void StartCountDown()
    {
        countDown.startCountdown = true;
    }


    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            camera.StopFollowMouse();
            uiManager.ExitMenu.SetActive(true);
        }

        if (isRunning)
        {
            Game_Clock += Time.deltaTime;
        }

        //TODO: DEBUG PURPOSES
        if (Input.GetKeyDown(KeyCode.R))
        {
            LocalPlayerRespawn();
        }
    }

    // Called by countdown clock
    public void StartRace()
    {
        isRunning = true;
        localPlayer.isRunning = true;
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
        GameObject p = Instantiate(GameManager.instance.LocalPlayerObj, spawns[Client.instance.clientInfo.spawnId], Quaternion.identity); //sceneManager.spawnPoints[myId]
        p.GetComponent<PlayerController>().camera = camera.transform;

        localPlayer = p.GetComponent<PlayerManager>();

        camera.enabled = true;
        camera.player = p.transform;
        camera.ragdoll = p.GetComponent<PlayerController>().pelvis;
    }

    public void SpawnRemotePlayer(Guid id, string username)
    {
        GameObject p = Instantiate(GameManager.instance.RemotePlayerObj, spawns[Client.peers[id].SpawnId], Quaternion.identity);
        players.Add(id, p.GetComponent<RemotePlayerManager>());
        players[id].SetIdentification(id, username);
    }
    #endregion

    #region Respawn Player
    //Sent from other players to respawn
    public void PlayerRespawn(Guid id, int checkPointNum)
    {
        Vector3 newPos = GetRespawnPosition(Client.instance.clientExeID, checkPointNum);

        if (id == Client.instance.clientInfo.id)
            localPlayer.Respawn(newPos, Quaternion.identity);
        else
            players[id].Respawn(newPos, Quaternion.identity);
    }

    public void LocalPlayerRespawn()
    {
        Vector3 newPos = GetRespawnPosition(Client.instance.clientExeID, playerCheckPoint);
        localPlayer.Respawn(newPos, Quaternion.identity);
        ClientSend.PlayerRespawn(playerCheckPoint);
    }

    private Vector3 GetRespawnPosition(int id, int checkPointNum)
    {
        Debug.Log($"numCheck:{checkPointNum}");
        if (checkPointNum == 0)
            return spawns[id - 1];
        else
            return checkPoints[checkPointNum - 1].position;
    }
    #endregion

    public void FinishRaceForLocalPlayer()
    {
        qualified = true;
        confetti.Play();
        ClientSend.PlayerFinish(Game_Clock);
        localPlayer.gameObject.SetActive(false);
        UpdateQualified();
        uiManager.Qualified();
    }


    public void FinishRaceForPlayer(Guid id, float time)
    {
        players[id].gameObject.SetActive(false);
        UpdateQualified();
    }


    public void UpdateQualified()
    {
        qualifiedPlayers++;
        Debug.Log($"Qualified:{qualifiedPlayers}");
        if (qualifiedPlayers >= players.Count + 1)
            FinishRace();
    }

    public void FinishRace()
    {
        Debug.Log("Race finished. Go to main menu.");
        camera.StopFollowMouse();
        if (!qualified)
        {
            uiManager.UnQualified();
        }
        //GameManager.instance.LoadMainMenu();
    }

    public void SetCheckPoint(int newCheckPoint)
    {
        playerCheckPoint = newCheckPoint;
    }
}

