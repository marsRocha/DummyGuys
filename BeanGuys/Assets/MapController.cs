using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapController : MonoBehaviour
{
    public Dictionary<int, RemotePlayerManager> players;
    public PlayerManager localPlayer;

    //Controls game
    public float Game_Clock { get; private set; } = 0;
    public bool isRunning { get; private set; } = false;


    [Header("Players Stuff")]
    public Transform camera;

    private int qualifiedPlayers;
    private int totalPlayers;

    // Start is called before the first frame update
    private void Start()
    {
        players = new Dictionary<int, RemotePlayerManager>();

        Game_Clock = 0;
        isRunning = false;

        qualifiedPlayers = 0;
        totalPlayers = 0;
    }

    // Update is called once per frame
    void Update()
    {
        //DEBUG PURPOSES
        if (Input.GetKeyDown(KeyCode.P))
        {
            localPlayer.isRunning = true;
        }
    }

    private void FixedUpdate()
    {
        if (isRunning)
        {
            Game_Clock += 1;
            Physics.Simulate(Time.fixedDeltaTime);
        }
    }

    public void StartRace()
    {
        isRunning = true;
    }


    public void SpawnLocalPlayer()
    {
        GameObject p = GameObject.Instantiate(GameManager.LocalPlayerObj, Vector3.zero, Quaternion.identity); //sceneManager.spawnPoints[myId]
        p.GetComponent<PlayerController>().camera = camera;

        localPlayer = p.GetComponent<PlayerManager>();

        camera.GetComponent<PlayerCamera>().enabled = true;
        camera.GetComponent<PlayerCamera>().player = p.transform;
        camera.GetComponent<PlayerCamera>().ragdoll = p.GetComponent<PlayerController>().pelvis;
        totalPlayers++;
        //UpdateQualified();
    }

    public void SpawnRemotePlayer(int id, string username)
    {
        GameObject p = Instantiate(GameManager.RemotePlayerObj, new Vector3(-2.6f, 0.0f, -5.2f), Quaternion.identity);
        players.Add(id, p.GetComponent<RemotePlayerManager>());

        totalPlayers++;
        //UpdateQualified();
    }

    public void UpdateQualified()
    {
        qualifiedPlayers++;
    }
}
