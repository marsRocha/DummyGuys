using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MenuSceneManager : MonoBehaviour
{
    public GameObject MainMenuCamera;
    public GameObject LookingMenuCamera;

    public GameObject MainMenuObj;
    public GameObject LookingMenuObj;
    public GameObject ExitMenuObj;

    public TMP_Text PlayerCount;
    public TMP_Text Quit_txt;

    private bool state; //true = menu, false = looking

    // Start is called before the first frame update
    private void Start()
    {
        MainMenuCamera.SetActive(true);
        LookingMenuCamera.SetActive(false);

        MainMenuObj.SetActive(true);
        LookingMenuObj.SetActive(false);
        ExitMenuObj.SetActive(false);

        state = true;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ExitMenuObj.SetActive(true);

            if (state)
            {
                Quit_txt.text = "Quit game?";
            }
            else
            {
                Quit_txt.text = "Quit to main menu?";
            }
        }
    }

    public void LookingMenu()
    {
        MainMenuCamera.SetActive(false);
        LookingMenuCamera.SetActive(true);

        MainMenuObj.SetActive(false);
        LookingMenuObj.SetActive(true);
        ExitMenuObj.SetActive(false);

        state = false;
    }

    public void MainMenu()
    {
        MainMenuCamera.SetActive(true);
        LookingMenuCamera.SetActive(false);

        MainMenuObj.SetActive(true);
        LookingMenuObj.SetActive(false);
        ExitMenuObj.SetActive(false);

        state = true;
    }

    public void UpdatePlayerCountUI(int count)
    {
        PlayerCount.text = $"{count}/60 players";
    }

    public void ExitYes()
    {
        if (state)
            Application.Quit();
        else
            NetworkManager.instance.GoOffline();
    }

    public void ExitCancel()
    {
        ExitMenuObj.SetActive(false);
    }
}
