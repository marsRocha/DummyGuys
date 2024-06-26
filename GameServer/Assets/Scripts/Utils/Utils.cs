﻿using UnityEngine;

public class Utils
{
    public static float TickInterval()
    {
        return ticksToTime(1);
    }

    public static int timeToTicks(float _time)
    {
        return Mathf.FloorToInt(_time / (1f / ServerData.TICK_RATE));
    }

    public static float ticksToTime(int _ticks)
    {
        return (float)_ticks * (1f / ServerData.TICK_RATE);
    }
}