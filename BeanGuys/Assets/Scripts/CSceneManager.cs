using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using TMPro;
using UnityEngine;

public class CSceneManager : MonoBehaviour
{
    public static CSceneManager instance;

    public int tick_number { get; private set; } = 0;

    [Header("UI stuff")]
    public CountDown countDownUI;
    public TextMeshProUGUI qualifiedTxt;
    public GameObject exitUI;
    public GameObject qualifiedUI;
    public GameObject unqualifiedUI;

    [Header("Movable Objects")]
    public Moving[] moveObjs;
    public MoveWait[] moveWaitObjs;
    public Swing[] swingObjs;
    public RotatingCylinder blades;
    public Spinning[] spinObjs;

    [Header("Players Stuff")]
    [SerializeField]
    public Vector3[] spawnPoints;
    public Transform camera;

    [Header("Dubug variables to remove")]
    public bool debug;
    public GameObject player;
    private GameObject playerObj;

    #region Singleton
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(this);
        }
    }
    #endregion


    private void Start()
    {
        /*points = new Vector3[60];
        for (int i = 0; i <= 3; i++)
        {
            for(int j = 0; j <= 14; j++)
                points[(14 * i) + j] = new Vector3(-18.06f + 2.58f * j, 0,-2.58f * i);
        }*/

        //only for debugging
        if (debug)
        {
            ActivateScene();
            GameObject p = GameObject.Instantiate(player, spawnPoints[10], Quaternion.identity);
            p.GetComponent<PlayerController>().camera = camera;
            playerObj = p;
            camera.GetComponent<PlayerCamera>().enabled = true;
            camera.GetComponent<PlayerCamera>().player = p.transform;
            camera.GetComponent<PlayerCamera>().ragdoll = p.GetComponent<PlayerController>().pelvis;
            p.GetComponent<PlayerController>().isRunning = true;
        }
    }

    public void StartCountDown()
    {
        countDownUI.startCountdown = true;
    }

    public void ActivateScene()
    {
        /*foreach (Moving m in moveObjs)
            m.isRunning = true;

        foreach (MoveWait m in moveWaitObjs)
            m.isRunning = true;

        foreach (Swing s in swingObjs)
            s.isRunning = true;

        blades.isRunning = true;

        /*foreach (Spinning s in spinObjs)
            s.isRunning = true;*/
    }

    public void ActivateExitMenu()
    {
        exitUI.SetActive(true);
        camera.GetComponent<PlayerCamera>().StopFollowMouse();
    }

    public void ActivateCameraFollow()
    {
        camera.GetComponent<PlayerCamera>().StartFollowMouse();
    }

    public void UpdateQualified(int qualified, int maxPlayers)
    {
        qualifiedTxt.text = $"{qualified}/{maxPlayers}";
    }

    public void FinishRaceForPlayer()
    {
        qualifiedUI.SetActive(true);
        StartCoroutine(CloseFinishUI(2f));

        //camera looking at random player
    }

    IEnumerator CloseFinishUI(float delayTime)
    {
        yield return new WaitForSeconds(delayTime);

        qualifiedUI.SetActive(false);
    }
}
