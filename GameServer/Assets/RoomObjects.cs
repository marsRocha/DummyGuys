using System;
using System.Collections.Generic;
using UnityEngine;

public class RoomObjects : MonoBehaviour
{
    public Player[] players;

    public Transform[] spawns;
    public Transform[] checkPoints;

    //Controls game
    public float Game_Clock;
    public bool isRunning { get; private set; } = false;

    //TO MODIFY: Should store every player's info on checkpoint ( or store it inside player class)
    public int playerCheckPoint { get; private set; } = 0;

    // Update is called once per frame
    void Update()
    {
        if (isRunning)
        {
            Game_Clock += Time.deltaTime;
        }
    }


    public Player GetPlayerObject(Guid _playerId, int _spawnPos)
    {
        Player p = players[_spawnPos];
        p.Initialize(_playerId);

        return p;
    }
}

