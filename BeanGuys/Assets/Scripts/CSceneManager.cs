using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using TMPro;
using UnityEngine;

public class CSceneManager : MonoBehaviour
{
    [Header("Scene stuff")]
    public CountDown countDownUI;
    public TextMeshProUGUI qualifiedTxt;
    public GameObject exitUI;

    [Header("Movable Objects")]
    public Moving[] moveObjs;
    public MoveWait[] moveWaitObjs;
    public Swing[] swingObjs;
    public RotatingCylinder[] rotatingObjs;

    [Header("Players Stuff")]
    [SerializeField]
    public Vector3[] spawnPoints;
    public Transform camera;

    [Header("Dubug variables to remove")]
    public bool debug;
    public GameObject player;
    private GameObject playerObj;

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
            camera.GetComponent<PlayerCamera>().ToFollow = p.transform;
            p.GetComponent<PlayerController>().isRunning = true;
        }
    }

    public void StartCountDown()
    {
        countDownUI.startCountdown = true;
    }

    public void ActivateScene()
    {
        foreach (Moving m in moveObjs)
            m.isRunning = true;

        foreach (MoveWait m in moveWaitObjs)
            m.isRunning = true;

        foreach (Swing s in swingObjs)
            s.isRunning = true;

        foreach (RotatingCylinder r in rotatingObjs)
            r.isRunning = true;
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
}
