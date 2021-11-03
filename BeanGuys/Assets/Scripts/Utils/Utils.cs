using UnityEngine;

public class Utils
{
    public static float TickInterval()
    {
        return TicksToTime(1);
    }

    public static int TimeToTicks(float _time)
    {
        return Mathf.FloorToInt(_time / (1f / MapController.instance.gameLogic.Tickrate));
    }

    public static float TicksToTime(int _ticks)
    {
        return (float)_ticks * (1f / MapController.instance.gameLogic.Tickrate);
    }
}
