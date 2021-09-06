using TMPro;
using UnityEngine;

public class Analytics : MonoBehaviour
{
    [SerializeField]
    private GameObject content;
    private float nextTime;

    [SerializeField]
    private TMP_Text cpuTxt;

    [SerializeField]
    private TMP_Text pingTxt;

    [SerializeField]
    private TMP_Text packetUpTxt;
    public static int packetsUp;
    [SerializeField]
    private TMP_Text packetDownTxt;
    public static int packetsDown;

    [SerializeField]
    private TMP_Text bandwidthUpTxt;
    public static int bandwidthUp;
    [SerializeField]
    private TMP_Text bandwidthDownTxt;
    public static int bandwidthDown;

    // States
    [SerializeField]
    private bool active;

    private void Start()
    {
        nextTime = 0;
        content.SetActive(active);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.U))
        {
            active = !active;
            content.SetActive(active);
        }

        if (active)
        {
            nextTime += Time.fixedDeltaTime;
            if (nextTime >= 1)
            {
                UpdateFps();
                UpdatePing();
                UpdatePackets();
                UpdateBytes();

                // Reset variables
                packetsUp = 0;
                packetsDown = 0;
                bandwidthUp = 0;
                bandwidthDown = 0;

                nextTime = 0;
            }
        }
    }

    private void UpdateFps()
    {
        cpuTxt.text = string.Format("{0:0.0}ms ({1:0.}fps)", Time.deltaTime * 1000.0f, 1.0f / Time.deltaTime);
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

    private void UpdatePackets()
    {
        packetUpTxt.text = $"Packet Up/s: {packetsUp}";
        packetDownTxt.text = $"Packet Down/s: {packetsDown}";
    }

    private void UpdateBytes()
    {
        bandwidthUpTxt.text = $"Bytes Up/s: {bandwidthUp}";
        bandwidthDownTxt.text = $"Bytes Down/s: {bandwidthDown}";
    }
}
