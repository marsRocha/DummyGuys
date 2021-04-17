using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapController : MonoBehaviour
{
    public static MapController instance;

    public Dictionary<Guid, RemotePlayerManager> players;
    public PlayerManager localPlayer; // { get; private set;} = null;

    //TODO: TO MODIFY
    public Transform[] spawns;
    public Transform[] checkPoints;

    //Controls game
    public float Game_Clock;
    public bool isRunning { get; private set; } = false;


    [Header("Components")]
    public Transform camera;
    public ParticleSystem confetti;

    public int playerCheckPoint { get; private set; } = 0;
    private int qualifiedPlayers;
    private int totalPlayers;


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

    // Start is called before the first frame update
    private void Start()
    {
        players = new Dictionary<Guid, RemotePlayerManager>();

        Game_Clock = 0;
        isRunning = false;

        qualifiedPlayers = 0;
        totalPlayers = 0;
    }

    public void Initialize()
    {
        SpawnPlayers();
        isRunning = true;
    }


    // Update is called once per frame
    void Update()
    {
        if (isRunning)
        {
            Game_Clock += Time.deltaTime;
        }

        //DEBUG PURPOSES
        if (Input.GetKeyDown(KeyCode.P))
        {
            localPlayer.isRunning = true;
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            LocalPlayerRespawn();
        }
    }

    private void FixedUpdate()
    {

    }

    public void StartRace()
    {
        isRunning = true;
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
        GameObject p = Instantiate(GameManager.instance.LocalPlayerObj, spawns[Client.instance.clientExeID].position, Quaternion.identity); //sceneManager.spawnPoints[myId]
        p.GetComponent<PlayerController>().camera = camera;

        localPlayer = p.GetComponent<PlayerManager>();

        camera.GetComponent<PlayerCamera>().enabled = true;
        camera.GetComponent<PlayerCamera>().player = p.transform;
        camera.GetComponent<PlayerCamera>().ragdoll = p.GetComponent<PlayerController>().pelvis;
    }

    public void SpawnRemotePlayer(Guid id, string username)
    {
        GameObject p = Instantiate(GameManager.instance.RemotePlayerObj, spawns[Client.instance.clientExeID - 1].position, Quaternion.identity);
        players.Add(id, p.GetComponent<RemotePlayerManager>());
        players[id].SetIdentification(id, username);
    }
    #endregion

    #region Respawn Player
    //Sent from other players to respawn
    public void PlayerRespawn(Guid id, int checkPointNum)
    {
        Vector3 newPos = GetRespawnPosition(Client.instance.clientExeID, checkPointNum);

        if (id == Client.instance.myId)
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
            return spawns[id - 1].position;
        else
            return checkPoints[checkPointNum - 1].position;
    }
    #endregion

    public void FinishRaceForLocalPlayer()
    {
        confetti.Play();
        ClientSend.PlayerFinish(Game_Clock);
        localPlayer.gameObject.SetActive(false);
        UpdateQualified();
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
        //GameManager.instance.LoadMainMenu();
    }

    public void SetCheckPoint(int newCheckPoint)
    {
        playerCheckPoint = newCheckPoint;
    }
}

