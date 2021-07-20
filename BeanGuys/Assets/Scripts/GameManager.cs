using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance = null;
    [SerializeField]
    private MapController mapController;

    public int totalPlayers = 0;

    // Ping stuff
    public float pingCountdown = 1f;
    public float pingCountdownLimit = 1f;

    [Header("States")]
    public bool isRunning;
    public bool isOnline;
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

    public void FixedUpdate()
    {
        if (!debug)
        {
            if (Client.instance.isConnected)
            {
                pingCountdown += Time.fixedDeltaTime;
                if (pingCountdown >= pingCountdownLimit)
                {
                    pingCountdown = 0;
                    ClientSend.Ping();
                }
            }
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

            if (isOnline)
                ClientSend.PlayerReady();
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
        if (!debug)
            GameObject.Find("SceneManager").GetComponent<MenuSceneManager>().UpdatePlayerCountUI(totalPlayers);
        else
            GameObject.Find("Canvas").transform.GetChild(1).GetComponent<TMP_Text>().text = $"{totalPlayers}/60 Players";
    }

    public void SpawnRemotePlayers()
    {
        foreach(Peer peer in Client.peers.Values)
        {
            mapController.SpawnRemotePlayer(peer.Id, peer.Username, peer.Color);
        }
    }

    public void StartGame()
    {
        Debug.Log("Start Game");
        if (debug)
        {
            mapController.StartRace();
        }
        else mapController.StartCountDown();
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

    public void PlayerMovement(Guid _id, int _tick, Vector3 _position, Quaternion _rotation, bool _ragdoll, int _animation)
    {
        if (mapController.players.TryGetValue(_id, out RemotePlayerManager _player))
        {
            _player.NewPlayerState(_tick, _position, _rotation, _ragdoll, _animation);
        }
    }

    public void PlayerCorrection(SimulationState simulationState)
    {
        if (!mapController.localPlayer.gameObject)
            return;

        mapController.localPlayer.ReceivedCorrectionState(simulationState);
    }

    public void PlayerRespawn(int _checkPointNum)
    {
        mapController.LocalPlayerRespawn(_checkPointNum);
    }

    public void PlayerFinish(Guid _clientId)
    {
        mapController.PlayerFinish(_clientId);
    }

    public void EndGame()
    {
        mapController.EndRace();
    }

    public void LeaveRoom()
    {
        Debug.Log("Game has ended");
        Client.instance.Disconnect();
        LoadMainMenu();
    }
}
