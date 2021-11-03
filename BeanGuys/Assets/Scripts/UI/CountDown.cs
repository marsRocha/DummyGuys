using TMPro;
using UnityEngine;

public class Countdown : MonoBehaviour
{
#pragma warning disable 0649
    [SerializeField]
    private float startTime;
    private float currentTime;
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
                countdownText.text = currentTime.ToString("0");
            }
            else
            {
                countdownText.text = "GO!";
            }

            if (currentTime <= 0)
            {
                MapController.instance.StartRace();
                StopCountdown();
            }
        }
    }

    public void StartCountdown()
    {
        startTime = 3;
        currentTime = startTime;

        countdownUI.gameObject.SetActive(true);
        countdownText = countdownUI.GetChild(0).GetComponent<TextMeshProUGUI>();
        countdownText.enabled = true;
        startCountdown = true;
    }

    public void StopCountdown()
    {
        countdownUI.gameObject.SetActive(false);
        this.enabled = false;
    }
}
