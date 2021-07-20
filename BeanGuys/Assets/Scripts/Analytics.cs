using TMPro;
using UnityEngine;

public class Analytics : MonoBehaviour
{
    [SerializeField]
    private GameObject content;
    [SerializeField]
    private TMP_Text pingTxt;

    // States
    [SerializeField]
    private bool active;

    private void Start()
    {
        content.SetActive(active);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.U))
        {
            active = !active;
            content.SetActive(active);
        }

        if (active)
        {
            UpdatePing();
        }
    }

    private void UpdatePing()
    {
        if (Client.instance)
        {
            pingTxt.text = $"Ping: {Client.instance.ping}ms";
        }
        else
        {
            pingTxt.text = "Ping: 0ms";
        }
    }
}
