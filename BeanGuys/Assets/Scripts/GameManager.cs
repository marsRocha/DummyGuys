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

    public void FixedUpdate()
    {
        if (isOnline)
        {
            pingCountdown += Time.fixedDeltaTime;
            if (pingCountdown >= pingCountdownLimit)
            {
                pingCountdown = 0;
                ClientSend.Ping();
            }
        }
    }

    #region Scene Loading related
    public void LoadGameScene(int _mapIndex)
    {
        if (!debug)
        {
            SceneManager.LoadScene(_mapIndex);
        }
        else
        {   // If debug, fake scene loading and send 'ready' message
            GameObject.Find("PlayersCount_UI").SetActive(false);
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
        if (SceneManager.GetActiveScene().name == "MainMenu_M")
        {
            return;
        }
        else //Game world
        {
            if (!debug)
            {
                Debug.Log("GameWorld Loaded");
                mapController = GameObject.Find("MapController").GetComponent<MapController>();
                mapController.Initialize();

                ClientSend.PlayerReady();
            }
        }
    }
    #endregion

    // Only called before game has started
    public void UpdatePlayerCount()
    {
        totalPlayers = Client.peers.Count + 1;
        if (!debug)
        {
            GameObject.Find("SceneManager").GetComponent<MenuSceneManager>().UpdatePlayerCountUI(totalPlayers);
        }
        else
            GameObject.Find("PlayersCount_UI").transform.GetChild(1).GetComponent<TMP_Text>().text = $"{totalPlayers}/60 Players";
    }

    public void JoinedRoom()
    {
        isOnline = true;
        if (!debug)
        {
            GameObject.Find("SceneManager").GetComponent<MenuSceneManager>().LookingMenu();
        }
        UpdatePlayerCount();
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
        if (!debug)
        {
            mapController.StartCountDown();
        }
        else
        {
            mapController.StartRace();
        }
    }

    public void RemovePlayer(Guid _clientId)
    {
        if (mapController != null && mapController.isRunning)
        {
            ThreadManager.ExecuteOnMainThread(() =>
            {
                mapController.PlayerLeft(_clientId);
            });
        }
    }

    public void PlayerMovement(Guid _id, int _tick, Vector3 _position, Quaternion _rotation, bool _ragdoll, int _animation)
    {
        if (mapController.players.TryGetValue(_id, out RemotePlayerManager _player))
        {
            _player.ReceivedPlayerState(_tick, _position, _rotation, _ragdoll, _animation);
        }
    }

    public void PlayerCorrection(PlayerState _playerState)
    {
        if (!mapController.localPlayer.gameObject)
            return;

        mapController.localPlayer.ReceivedCorrectionState(_playerState);
    }

    public void PlayerGrab(Guid _grabber, Guid _grabbed)
    {
        if (!mapController.localPlayer.gameObject) // || !mapController.players[_grabber].gameObject || !mapController.players[_grabbed].gameObject)
            return;

        // If the local player was grabbed apply grab behavior, otherwise the local player is the one who is grabbing
        if (_grabbed == ClientInfo.instance.Id)
            mapController.localPlayer.ReceivedGrabbed();
        else
            mapController.localPlayer.ReceivedGrabbing(_grabbed);
    }

    public void PlayerLetGo(Guid _grabber, Guid _grabbed)
    {
        if (!mapController.localPlayer.gameObject) // || !mapController.players[_grabber].gameObject || !mapController.players[_grabbed].gameObject)
            return;

        // If the local player was grabbed apply grab behavior, otherwise the local player is the one who is grabbing
        if (_grabbed == ClientInfo.instance.Id)
            mapController.localPlayer.ReceivedFreed();
        else
            mapController.localPlayer.ReceivedLetGo();
    }

    public void PlayerPush(Guid _pusher, Guid _pushed)
    {
        if (!mapController.localPlayer.gameObject) // || !mapController.players[_pusher].gameObject || !mapController.players[_pushed].gameObject)
            return;

        // If the local player was pushed apply push force, otherwise the local player is the one who pushed someone
        if(_pushed == ClientInfo.instance.Id)
        {
            Vector3 pushDirection = (mapController.localPlayer.transform.position - mapController.players[_pusher].transform.position).normalized;
            mapController.localPlayer.ReceivedPushed(pushDirection);
        }
        else
        {
            mapController.localPlayer.ReceivedPushing();
        }
    }

    public void PlayerRespawn(int _checkPointNum)
    {
        mapController.LocalPlayerRespawn(_checkPointNum);
    }

    public void PlayerFinish(Guid _clientId)
    {
        mapController.PlayerFinish(_clientId);
    }

    public void ServerTick(int _roomTick, float _roomClock)
    {
        if (_roomTick > mapController.gameLogic.Tick)
            mapController.gameLogic.SetTick(_roomTick);

        if (_roomClock > mapController.gameLogic.Clock)
            mapController.gameLogic.SetClock(_roomClock);
    }

    public void EndGame()
    {
        mapController.EndRace();
    }

    public void Refused()
    {
        Debug.Log("Server has refused the connection.");

        if (!debug)
        {
            LoadMainMenu();
        }
    }

    public void Disconnected()
    {
        Client.instance.Disconnect();
        isOnline = false;
        mapController.StopRace();
    }

    public void LeaveRoom()
    {
        Debug.Log("Leave Room");

        Client.instance.Disconnect();
        
        isOnline = false;
        if (!debug)
        {
            LoadMainMenu();
        }
    }
}
