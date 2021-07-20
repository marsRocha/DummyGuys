using System;
using System.Collections.Generic;
using UnityEngine;

public class MapController : MonoBehaviour
{
    public static MapController instance;

    // Components
    [SerializeField]
    private new PlayerCamera camera;
    [SerializeField]
    private ParticleSystem confetti;
    [SerializeField]
    private UIManager uiManager;
    [SerializeField]
    private CountDown startCountDown;
    [SerializeField]
    private Timer returnCountDown;
    [SerializeField]
    private Transform[] checkPoints;
    private Vector3[] spawns;

    public Dictionary<Guid, RemotePlayerManager> players { get; private set; }
    public PlayerManager localPlayer { get; private set; }

    public bool isRunning { get; private set; } = false;

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
                spawns[(14 * i) + j] = new Vector3(-18.06f + 2.58f * j, 0, -2.58f * i);
        }

        players = new Dictionary<Guid, RemotePlayerManager>();

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
                spawns[(14 * i) + j] = new Vector3(-18.06f + 2.58f * j, 0.5027312f, -2.58f * i);
        }

        players = new Dictionary<Guid, RemotePlayerManager>();
        SpawnPlayers();

        isRunning = true;
        qualified = false;

        qualifiedPlayers = 0;
        totalPlayers = players.Count + 1;
    }

    public void StartCountDown()
    {
        startCountDown.startCountdown = true;
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            camera.StopFollowMouse();
            uiManager.OpenMenu();
        }

        if (isRunning)
        {
            GameLogic.SetClock(GameLogic.Clock + Time.deltaTime);
        }
    }

    // Called by countdown clock
    public void StartRace()
    {
        isRunning = true;
        if(!GameManager.instance.debug)
            localPlayer.Running = true;
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
        GameObject p = Instantiate(GameManager.instance.LocalPlayerObj, spawns[ClientInfo.instance.SpawnId], Quaternion.identity);
        p.transform.GetChild(0).GetChild(1).GetComponent<SkinnedMeshRenderer>().material = PlayerColor.instance.materials[ClientInfo.instance.Color];

        localPlayer = p.GetComponent<PlayerManager>();

        camera.enabled = true;
        camera.SetFollowTargets( p.transform, p.GetComponent<PlayerController>().pelvis);
    }

    public void SpawnRemotePlayer(Guid _id, string _username, int _color)
    {
        GameObject p = Instantiate(GameManager.instance.RemotePlayerObj, spawns[Client.peers[_id].SpawnId], Quaternion.identity);
        p.transform.GetChild(0).GetChild(1).GetComponent<SkinnedMeshRenderer>().material = PlayerColor.instance.materials[_color];

        players.Add(_id, p.GetComponent<RemotePlayerManager>());
        players[_id].Initialize(_id, _username);
    }
    #endregion

    #region Respawn Player
    public void PlayerRespawn(Guid id, int checkPointNum)
    {
        Vector3 newPos = GetRespawnPosition(Client.peers[id].SpawnId, checkPointNum);

        Debug.Log("doing nothing");
        //players[id].Respawn(newPos, Quaternion.identity); //TODO: CHECK IF THIS WORKS
    }

    public void LocalPlayerRespawn(int _checkPointNum)
    {
        Vector3 newPos = GetRespawnPosition(ClientInfo.instance.SpawnId, _checkPointNum);
        localPlayer.Respawn(newPos, Quaternion.identity);
    }

    private Vector3 GetRespawnPosition(int id, int checkPointNum)
    {
        Debug.Log($"numCheck:{checkPointNum}");
        if (checkPointNum == 0)
            return spawns[id];
        else
            return checkPoints[checkPointNum - 1].position;
    }
    #endregion

    public void PlayerFinish(Guid _clientId)
    {
        if (_clientId == ClientInfo.instance.Id)
        {
            qualified = true;
            confetti.Play();
            localPlayer.gameObject.SetActive(false);
            UpdateQualified();
            uiManager.OnQualified(qualifiedPlayers, totalPlayers, ClientInfo.instance.Username);

            // Enter spectate mode
            camera.StartSpectating();
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
        Debug.Log("Race finished. Go to main menu.");
        camera.StopFollowMouse();
        if (!qualified)
            uiManager.UnQualified();
        else 
            uiManager.Qualified();

        returnCountDown.gameObject.SetActive(true);
        returnCountDown.startCountdown = true;
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

