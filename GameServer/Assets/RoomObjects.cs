using System;
using System.Collections.Generic;
using UnityEngine;

public class RoomObjects : MonoBehaviour
{
    public GameObject playerObject;

    [HideInInspector]
    public Vector3[] spawns;
    public Transform[] checkPoints;

    //Controls game
    public float Game_Clock;
    public bool isRunning { get; private set; } = false;

    //TO MODIFY: Should store every player's info on checkpoint ( or store it inside player class)
    public int playerCheckPoint { get; private set; } = 0;

    public void Initialize(int totalPlayers)
    {
        spawns = new Vector3[60];
        for (int i = 0; i <= 3; i++)
        {
            for (int j = 0; j <= 14; j++)
                spawns[(14 * i) + j] = new Vector3(-18.06f + 2.58f * j, 0, -2.58f * i);
        }
    }

    public Player SpawnPlayer(Guid _playerId, int _spawnId)
    {
        Player p = Instantiate(playerObject, spawns[_spawnId], Quaternion.identity).GetComponent<Player>();
        p.Initialize(_playerId);

        return p;
    }
}

