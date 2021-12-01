

using System;
using UnityEngine;

public class GameLogic
{
    public int Tickrate; // Ticks per second 30 - 128;
    public int lerpPeriod; // in milliseconds
    public bool playerInteraction;

    public int DelayedTick;
    public float SecPerTick; // ( 1 / Tickrate);

    public int SnapshotRate; // Number of times per second a clients sends a player state
    public float SecPerState; // ( 1 / psnapshotRate);

    public int Tick { get; private set; } = 0;
    public int InterpolationTick { get; private set; } = 0;

    public float Clock { get; private set; } = 0;

    public GameLogic()
    {
        Tickrate = RoomSettings.TICKRATE;
        lerpPeriod = RoomSettings.INTERPOLATION;
        playerInteraction = RoomSettings.PLAYER_INTERACTION;

        DelayedTick = (int)(Tickrate * (lerpPeriod * 0.001f));
        SecPerTick = (float)Math.Round(Double.Parse(((float)1 / (float)Tickrate).ToString()), 5);

        SnapshotRate = 20;
        SecPerState = (float)Math.Round(Double.Parse(((float)1 / (float)SnapshotRate).ToString()), 5);

        Debug.Log("oh snap" + SnapshotRate + " current" + SecPerState + " secTick"+ SecPerTick);

        Tick = 0;
        InterpolationTick = 0;
        Clock = 0;
    }

    public void SetTickrate()
    {
        Tickrate = RoomSettings.TICKRATE;
        DelayedTick = (int)(Tickrate * (lerpPeriod * 0.001f));
        SecPerTick = 1 / Tickrate;
    }

    public void SetTick(int _serverTick)
    {
        Tick = _serverTick;
        InterpolationTick = _serverTick - DelayedTick;
    }

    public void SetClock(float _serverClock)
    {
        Clock = _serverClock;
    }
}
