using System.Collections;
using System.Collections.Generic;
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

    public static GameObject RemotePlayerObj, LocalPlayerObj;

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
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            sceneManager.ActivateExitMenu();
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
            mapController.SpawnLocalPlayer();
            sceneManager.StartCountDown();
        }
    }

    public void SpawnRemotePlayer(int peerID, string username)
    {
        mapController.SpawnRemotePlayer(peerID, username);
    }

    //quando tiver multiplayer é melhor mandar mensagem para o servidor e depois se estiver correto entao começar o jogo
    public void StartGame()
    {
        mapController.StartRace();
    }

    //receber mensagem do servidor para atualizar isto
    public void UpdateQualified()
    {
        mapController.UpdateQualified();
    }

    public void FinishRaceForPlayer()
    {
        UpdateQualified();
        sceneManager.FinishRaceForPlayer();
    }

    public void AddInputMessage(int peerID, int x, int y, bool jump, bool dive, int tick_number)
    {
        mapController.players[peerID].AddInputMessage( x, y, jump, dive, tick_number);
    }
}
