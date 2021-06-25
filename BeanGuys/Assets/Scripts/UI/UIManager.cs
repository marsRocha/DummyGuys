using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Contains all methods to control and update the in-game user interface
/// </summary>
public class UIManager : MonoBehaviour
{
    // Canvas
    [SerializeField]
    private GameObject HUD, ExitMenu;

    // Frames
    [SerializeField]
    private GameObject PlayersFrame, WinnerFrame, LoserFrame;

    // Qualified number text
    [SerializeField]
    private TMP_Text qualifedNum;

    // Qualified feed
    [SerializeField]
    private Transform qualifiedFeedParent;
    [SerializeField]
    private GameObject qualifiedItem;

    /// <summary>Setup all ui elements.</summary>
    public void Initialize()
    {
        HUD.SetActive(true);
        ExitMenu.SetActive(false);

        PlayersFrame.SetActive(true);
        WinnerFrame.SetActive(false);
        LoserFrame.SetActive(false);
    }

    /// <summary>Activates/Deactivates the exit menu.</summary>
    /// <param name="_activate">The state to which the menu will transition.</param>
    public void OpenExitMenu(bool _activate)
    {
        ExitMenu.SetActive(_activate);
    }

    /// <summary>Activates the winner frame element if player qualified in the race.</summary>
    public void Qualified()
    {
        WinnerFrame.SetActive(true);
    }

    /// <summary>Activates the loser frame element if player did not qualify in the race.</summary>
    public void UnQualified()
    {
        LoserFrame.SetActive(true);
    }

    /// <summary>Updates the qualified number of players and displays the player on the qualified feed.</summary>
    /// <param name="_qualified">The number of qualified players.</param>
    /// <param name="_total">The total players in the race.</param>
    /// <param name="_username">The player's username that qualified.</param>
    public void OnQualified(int _qualified, int _total, string _username)
    {
        // Update qualified number text
        qualifedNum.text = $"{_qualified}/{_total}";

        // Add event to qualified feed
        GameObject item = Instantiate(qualifiedItem, qualifiedFeedParent);
        // Setup display information
        item.transform.GetChild(0).GetComponent<TMP_Text>().text = _username;
        // Destroy object after x seconds
        Destroy(item, 4f);
    }
}
