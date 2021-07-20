using TMPro;
using UnityEngine;

public class MenuSceneManager : MonoBehaviour
{
    [SerializeField]
    private GameObject MainMenuCamera;
    [SerializeField]
    private GameObject LookingMenuCamera;

    [SerializeField]
    private GameObject MainMenuObj;
    [SerializeField]
    private GameObject LookingMenuObj;
    [SerializeField]
    private GameObject ExitMenuObj;

    [SerializeField]
    private TMP_Text PlayerCount;
    [SerializeField]
    private TMP_Text Quit_txt;

    [SerializeField]
    private GameObject menuPlayer;
    [SerializeField]
    private GameObject lookingPlayer;

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

    public void SetColorPlayerObjs()
    {
        menuPlayer.transform.GetChild(1).GetComponent<SkinnedMeshRenderer>().material = PlayerColor.instance.materials[ClientInfo.instance.Color];
        lookingPlayer.transform.GetChild(1).GetComponent<SkinnedMeshRenderer>().material = PlayerColor.instance.materials[ClientInfo.instance.Color];
    }
}
