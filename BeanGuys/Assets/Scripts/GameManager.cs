﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance = null;

    private CSceneManager sceneManager;

    public GameObject player, localPlayer;
    private GameObject playerObj;
    //network id
    public int myId;

    private int qualifiedPlayers;
    private int totalPlayers;


    [Header("States")]
    public bool isRunning;


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

    // Start is called before the first frame update
    void Start()
    {
        isRunning = false;
        qualifiedPlayers = 0;
        totalPlayers = 0;
    }

    public void LoadGameScene()
    {
        SceneManager.LoadScene("Map");
    }

    private void OnLevelWasLoaded(int level)
    {
        sceneManager = GameObject.Find("SceneManager").GetComponent<CSceneManager>();
        SpawnPlayer();
        Debug.Log("go countdown");
        sceneManager.StartCountDown();
    }

    public void SpawnPlayer()
    {
        GameObject p = GameObject.Instantiate(player, sceneManager.spawnPoints[myId], Quaternion.identity);
        p.GetComponent<PlayerController>().camera = sceneManager.camera;
        playerObj = p;
        sceneManager.camera.GetComponent<PlayerCamera>().enabled = true;
        sceneManager.camera.GetComponent<PlayerCamera>().ToFollow = p.transform;
        totalPlayers++;
        UpdateQualified();
    }

    public void SpawnLocalPlayer(int id)
    {
        Instantiate(localPlayer, sceneManager.spawnPoints[id], Quaternion.identity);
        totalPlayers++;
        UpdateQualified();
    }

    //quando tiver multiplayer é melhor mandar mensagem para o servidor e depois se estiver correto entao começar o jogo
    public void StartGame()
    {
        sceneManager.ActivateScene();
        playerObj.GetComponent<PlayerController>().isRunning = true;
        isRunning = true;
    }

    //receber mensagem do servidor para atualizar isto
    public void UpdateQualified()
    {
        //qualifiedPlayers++;

        if(qualifiedPlayers >= totalPlayers)
        {
            //finish game
        }
        else //only update UI
        {
            sceneManager.UpdateQualified(qualifiedPlayers, totalPlayers);
        }
    }
}
