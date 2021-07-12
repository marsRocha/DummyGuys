using UnityEngine;

public class Timer : MonoBehaviour
{
    private int action;
    private float timeLeft;

    private void FixedUpdate()
    {
        timeLeft -= Time.fixedDeltaTime;
        if (timeLeft < 0)
        {
            Do();
        }
    }

    public void StartTimer(int _action, int _duration) // in seconds
    {
        action = _action;
        timeLeft = _duration;
    }

    private void Do()
    {
        switch (action)
        {
            case 1:
                gameObject.scene.GetRootGameObjects()[0].GetComponent<RoomScene>().StartRace();
                break;
            case 2:
                gameObject.scene.GetRootGameObjects()[0].GetComponent<RoomScene>().EndRace();
                break;
            default:
                break;
        }
    }
}
