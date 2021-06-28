using UnityEngine;

public class Utils
{
    public static float TickInterval()
    {
        return TicksToTime(1);
    }

    public static int TimeToTicks(float _time)
    {
        return Mathf.FloorToInt(_time / (1f / GameLogic.Tickrate));
    }

    public static float TicksToTime(int _ticks)
    {
        return (float)_ticks * (1f / GameLogic.Tickrate);
    }
}
