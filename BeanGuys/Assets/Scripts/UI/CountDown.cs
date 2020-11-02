using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CountDown : MonoBehaviour
{
    private float startTime;
    private float currentTime;
    private TextMeshProUGUI countdownText;
    [HideInInspector]
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
            Debug.Log("started");
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
                //quando tiver multiplayer é melhor mandar mensagem para o servidor e depois se estiver correto entao começar o jogo
                GameManager.instance.StartGame();
                this.gameObject.SetActive(false);
            }
        }
        Debug.Log("doing");
    }
}
