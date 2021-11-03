using UnityEngine;

public class Countdown : MonoBehaviour
{
    private RoomScene roomScene;

    private int action;
    private float timeLeft;

    private void Start()
    {
        foreach (GameObject obj in gameObject.scene.GetRootGameObjects())
        {
            if (obj.GetComponent<RoomScene>())
            {
                roomScene = obj.GetComponent<RoomScene>();
                break;
            }
        }
    }

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
                roomScene.StartRace();
                break;
            case 2:
                roomScene.EndRace();
                break;
            default:
                break;
        }
    }
}
