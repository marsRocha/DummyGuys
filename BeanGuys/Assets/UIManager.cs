using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject HUD;
    public GameObject ExitMenu;

    public GameObject PlayersFrame;
    public GameObject WinnerFrame;
    public GameObject LoserFrame;

    public TMP_Text qualifedNum;

    // Start is called before the first frame update
    void Start()
    {
    }

    private void Initialize()
    {
        HUD.SetActive(true);
        ExitMenu.SetActive(false);

        PlayersFrame.SetActive(true);
        WinnerFrame.SetActive(false);
        LoserFrame.SetActive(false);
    }

    public void Qualified()
    {
        WinnerFrame.SetActive(true);
        CloseUIin(2, WinnerFrame);
    }

    public void UnQualified()
    {
        LoserFrame.SetActive(true);
        CloseUIin(2, LoserFrame);
    }


    public void UpdateQualified(int num, int max)
    {
        qualifedNum.text = $"{num}/{max}";
    }

    IEnumerator CloseUIin(float delayTime, GameObject frame)
    {
        yield return new WaitForSeconds(delayTime);

        frame.SetActive(false);
    }
}
