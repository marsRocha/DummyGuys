using System.Collections;
using System.Collections.Generic;
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

    private void Start()
    {
        /*points = new Vector3[60];
        for (int i = 0; i <= 3; i++)
        {
            for(int j = 0; j <= 14; j++)
                points[(14 * i) + j] = new Vector3(-18.06f + 2.58f * j, 0,-2.58f * i);
        }*/
    }

    private void Update()
    {
        //this update is only for debugging
        if (Input.GetKeyDown(KeyCode.P))
        {
            ActivateScene();
            GameObject.Find("Player").GetComponent<PlayerController>().isRunning = true;
            Debug.Log("ACTIVATED");
        }
    }

    public void StartCountDown()
    {
        Debug.Log("done");
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

    public void ActivatePauseMenu()
    {
        exitUI.SetActive(true);
        //camera.
    }

    public void UpdateQualified(int qualified, int maxPlayers)
    {
        qualifiedTxt.text = $"{qualified}/{maxPlayers}";
    }
}
