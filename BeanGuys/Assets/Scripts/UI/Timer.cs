using TMPro;
using UnityEngine;

public class Timer : MonoBehaviour
{
    private float startTime;
    private float currentTime;
    private TextMeshProUGUI countdownText;
    public bool startCountdown = false;

    // Start is called before the first frame update
    void Start()
    {
        startTime = 5;
        currentTime = startTime;
        countdownText = this.GetComponent<TextMeshProUGUI>();
        countdownText.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (startCountdown)
        {
            if (!countdownText.enabled)
                countdownText.enabled = true;

            currentTime -= 1 * Time.deltaTime;

            if (currentTime >= 1)
            {
                countdownText.text = "Returning to Main Menu in (" + currentTime.ToString("0") + ")";
            }

            if (currentTime <= 0)
            {
                GameManager.instance.LeaveRoom();
            }
        }
    }
}
