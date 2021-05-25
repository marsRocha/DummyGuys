using TMPro;
using UnityEngine;

public class CountDown : MonoBehaviour
{
    private float startTime;
    private float currentTime;
    private TextMeshProUGUI countdownText;
    public bool startCountdown = false;

    // Start is called before the first frame update
    void Start()
    {
        startTime = 3;
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
                countdownText.text = currentTime.ToString("0");
            }
            else
            {
                countdownText.text = "GO!";
            }

            if (currentTime <= 0)
            {
                MapController.instance.StartRace();
                this.gameObject.SetActive(false);
            }
        }
    }
}
