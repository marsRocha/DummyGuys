using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameLogic
{
    public int Tickrate { get; private set; } // Ticks per second 30 - 128;
    public bool playerInteraction { get; private set; }
    public int Tick { get; private set; }
    public float Clock { get; private set; }

    public GameLogic()
    {
        Tickrate = ServerData.TICKRATE;
        playerInteraction = ServerData.PLAYER_INTERACTION;

        Tick = 0;
        Clock = 0;
    }

    public void SetTick(int _serverTick)
    {
        Tick = _serverTick;
    }

    public void SetClock(float _serverClock)
    {
        Clock = _serverClock;
    }
}
