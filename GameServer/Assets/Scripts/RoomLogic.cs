using System;
using System.Collections.Generic;
using UnityEngine;

public partial class Room
{
    public Dictionary<Guid, Player> players;

    public RoomObjects roomObjects;

    //Controls game
    public float Game_Clock;
    public bool isRunning { get; private set; } = false;

    //TO MODIFY: Should store every player's info on checkpoint ( or store it inside player class)
    public int playerCheckPoint { get; private set; } = 0;

    private int qualifiedPlayers;
    private int totalPlayers;

    public void Initialize()
    {
        players = new Dictionary<Guid, Player>();
        Game_Clock = 0;
        isRunning = false;

        qualifiedPlayers = 0;
        totalPlayers = ClientsInfo.Count;

        roomObjects.Initialize(totalPlayers);
        SpawnPlayers();
    }

    // Update is called once per frame
    void Update()
    {
        if (isRunning)
        {
            Game_Clock += Time.deltaTime;
        }
    }

    public void StartRace()
    {
        isRunning = true;
    }

    public void SpawnPlayers()
    {
        foreach(ClientInfo clientInfo in ClientsInfo.Values)
        {
            Player p = roomObjects.SpawnPlayer(clientInfo.id, clientInfo.spawnId);
            Server.Clients[clientInfo.id].SetPlayer(p);
            players.Add(clientInfo.id, p);
        }

        Debug.Log("Players spawned");
    }

    #region Respawn Player
    //Sent from other players to respawn
    public void PlayerRespawn(Guid id, int checkPointNum)
    {
        /*Vector3 newPos = GetRespawnPosition(id, checkPointNum);
        players[id].Respawn(newPos, Quaternion.identity);
        ServerSend.PlayerRespawn(playerCheckPoint);*/
    }

    private Vector3 GetRespawnPosition(int id, int checkPointNum)
    {
        Debug.Log($"numCheck:{checkPointNum}");
        if (checkPointNum == 0)
            return roomObjects.spawns[id];
        else
            return roomObjects.checkPoints[checkPointNum - 1].position;
    }
    #endregion

    public void FinishRacePlayer(Player _player)
    {
        UpdateQualified();
        PlayerFinish(_player.id, Game_Clock);
        _player.gameObject.SetActive(false);
    }

    public void UpdateQualified()
    {
        qualifiedPlayers++;
        Debug.Log($"Qualified:{qualifiedPlayers}");
        if (qualifiedPlayers >= players.Count + 1)
            EndRace();
    }

    public void EndRace()
    {
        Debug.Log("Race finished. Go to main menu.");
        //GameManager.instance.LoadMainMenu();
    }

    public void SetCheckPoint(int newCheckPoint)
    {
        playerCheckPoint = newCheckPoint;
    }
}
