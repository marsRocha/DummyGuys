using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance = null;
    [SerializeField]
    private CSceneManager sceneManager;
    [SerializeField]
    private MapController mapController;


    //TODO: FOR NOW STAYS HERE
    public TMP_Text playersCount;
    public int totalPlayers = 0;

    //REMOVE THIS FROM HERE LATER ON
    public GameObject RemotePlayerObj, LocalPlayerObj;

    [Header("States")]
    public bool isRunning;

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
        //detetar pausar ou sair do jogo
        /*if (Input.GetKeyDown(KeyCode.Escape))
        {
            sceneManager.ActivateExitMenu();
        }*/

        //FOR DEBUG PURPOSES
        if (Input.GetKeyDown(KeyCode.O))
        {
            ClientSend.StartGame();
            StartGameDebug();
        }
    }

    public void ApplicationQuit()
    {
        Debug.Log("exit");
        Application.Quit();
    }

    public void LoadGameScene()
    {
        SceneManager.LoadScene("Map1");
    }

    public void LoadMainMenu()
    {
        SceneManager.LoadScene(0);
    }

    private void OnLevelWasLoaded(int level)
    {
        sceneManager = GameObject.Find("SceneManager").GetComponent<CSceneManager>();
        if (level != 0) // not mainmenu
        {
            sceneManager.StartCountDown();
        }
    }

    public void UpdatePlayerCount()
    {
        totalPlayers++;
        playersCount.text = $"{totalPlayers}/60 Players";
    }

    public void SpawnRemotePlayers()
    {
        foreach(Peer peer in Client.peers.Values)
        {
            if(peer.tcp.socket != null)
                mapController.SpawnRemotePlayer(peer.id, peer.username);
        }
    }

    //DEBUG, simulate changing scene and spawn all players
    public void StartGameDebug()
    {
        Debug.Log("Start Game");
        playersCount.transform.root.gameObject.SetActive(false);
        mapController.Initialize();
    }

    //quando tiver multiplayer é melhor mandar mensagem para o servidor e depois se estiver correto entao começar o jogo
    public void StartGame()
    {
        mapController.StartRace();
    }

    public void PlayerMovement(int peerID, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angular_velocity, int tick_number)
    {
        mapController.players[peerID].UpdateMovement(position, rotation, velocity, angular_velocity, tick_number);
    }
    
    public void PlayerAnim(int peerID, int animNum)
    {
        mapController.players[peerID].UpdateAnimaiton(animNum);
    }

    public void PlayerRespawn(int peerID, int checkPointNum)
    {
        mapController.PlayerRespawn(peerID, checkPointNum);
    }

    public void PlayerFinish(int peerID, float time)
    {
        mapController.FinishRaceForPlayer(peerID, time);
    }

    public void Disconnect(int peerID)
    {
        //TODO: Change to server states
        if (isRunning)
        {
            ThreadManager.ExecuteOnMainThread(() =>
            {
                GameObject p = mapController.players[peerID].gameObject;
                mapController.players.Remove(peerID);
                Destroy(p);
            });
        }
    }
}
