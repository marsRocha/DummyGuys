using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance = null;
    [SerializeField]
    private MapController mapController;

    public int totalPlayers = 0;

    //REMOVE THIS FROM HERE LATER ON
    public GameObject RemotePlayerObj, LocalPlayerObj;

    [Header("States")]
    public bool isRunning;
    public bool debug;

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
        isRunning = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            ClientSend.Test();
            Debug.Log("sent test");
        }

        //TODO: FOR P2P PURPOSE, REMOVE AFTERWARDS
        if (Input.GetKeyDown(KeyCode.I))
        {
            ClientSend.Map();
            LoadGameScene("Level1");
        }
        if (Input.GetKeyDown(KeyCode.O))
        {
            ClientSend.StartGame();
            StartGame();
        }
    }

    #region Scene Loading related

    public void LoadGameScene(string levelId)
    {
        if(!debug)
            SceneManager.LoadScene(levelId);
        else
        {
            Debug.Log("DEBUG: GameWorld Loaded");
            GameObject.Find("Canvas").SetActive(false);
            mapController.InitializeDebug();
        }
    }

    public void LoadMainMenu()
    {
        SceneManager.LoadScene(0);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// When secene is loaded initialize variables needed for the GameTypeEvent
    /// Afterwards send confirmation to the Server that it's ready to start race
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (SceneManager.GetActiveScene().name == "MainMenu")
        {
            
        }
        else //Game world
        {
            if (!debug)
            {
                Debug.Log("GameWorld Loaded");
                mapController = GameObject.Find("MapController").GetComponent<MapController>();
                mapController.Initialize();
            }
        }
    }
    #endregion

    public void UpdatePlayerCount()
    {
        totalPlayers++;
        if(!debug)
        GameObject.Find("SceneManager").GetComponent<MenuSceneManager>().UpdatePlayerCountUI(totalPlayers);
    }

    public void SpawnRemotePlayers()
    {
        foreach(Peer peer in Client.peers.Values)
        {
            if(peer.tcp.socket != null)
                mapController.SpawnRemotePlayer(peer.Id, peer.Username);
        }
    }

    //TODO: REMOVE AFTERWARDS ONLY FOR P2P FUNCTIONS
    public void StartGame()
    {
        Debug.Log("Start Game");
        if (debug)
        {
            mapController.StartRace();
        }
        else mapController.StartCountDown();
    }

    public void EndGame()
    {
        Debug.Log("Game has ended");
        Client.instance.Disconnect();
        LoadMainMenu();
    }

    public void RemovePlayer(Guid peerID)
    {
        if (mapController != null && mapController.isRunning)
        {
            ThreadManager.ExecuteOnMainThread(() =>
            {
                GameObject p = mapController.players[peerID].gameObject;
                mapController.players.Remove(peerID);
                Destroy(p);
            });
        }
    }

    #region Player messages
    public void PlayerMovement(Guid peerID, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angular_velocity, float tick_number)
    {
        mapController.players[peerID].UpdateMovement(position, rotation, velocity, angular_velocity, tick_number);
    }

    public void PlayerAnim(Guid peerID, int animNum)
    {
        mapController.players[peerID].UpdateAnimaiton(animNum);
    }

    public void PlayerRespawn(Guid peerID, int checkPointNum)
    {
        mapController.PlayerRespawn(peerID, checkPointNum);
    }

    public void PlayerFinish(Guid peerID, float time)
    {
        mapController.FinishRaceForPlayer(peerID, time);
    }
    #endregion
}
