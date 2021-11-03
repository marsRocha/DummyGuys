using TMPro;
using UnityEngine;

public class Timer : MonoBehaviour
{
    private float startTime;
    private float currentTime;
#pragma warning disable 0649
    [SerializeField]
    private Transform countdownUI;
#pragma warning restore 0649
    private TextMeshProUGUI countdownText;
    public bool startCountdown = false;

    // Update is called once per frame
    private void Update()
    {
        if (startCountdown)
        {
            currentTime -= 1 * Time.deltaTime;

            if (currentTime >= 1)
            {
                countdownText.text = "Returning to Main Menu in (" + currentTime.ToString("0") + ")";
            }

            if (currentTime <= 0)
            {
                StopCountdown();
            }
        }
    }

    public void StartCountdown()
    {
        startTime = 5;
        currentTime = startTime;

        countdownText = countdownUI.GetComponent<TextMeshProUGUI>();
        countdownText.enabled = true;
        startCountdown = true;
    }

    public void StopCountdown()
    {
        GameManager.instance.LeaveRoom();
        this.enabled = false;
    }
}
