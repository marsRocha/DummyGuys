﻿using System;
using System.Collections.Generic;
using UnityEngine;

public class RoomScene : MonoBehaviour
{
    private Room room;
    private LogicTimer logicTimer;
    private PhysicsScene physicsScene;
    private LagCompensation lagCompensation;

    //Objects inside Scene
    [HideInInspector]
    public Vector3[] spawns;
    public Transform[] checkPoints;
#pragma warning disable 0649
    [SerializeField]
    private Transform[] playerObjs;
    [SerializeField]
    private Countdown timer;
    [SerializeField]
    public GameLogic gameLogic;
#pragma warning restore 0649

    public Dictionary<Guid, Player> players;
    public bool isRunning { get; private set; } = false;

    //Stores every player's info on checkpoint
    public Dictionary<Guid, int> playerCheckPoint;

    private int qualifiedPlayers;

    // Tick state
    public float tickCountdown = 1f;
    public float tickCountdownLimit = 1f;

    private void Start()
    {
        logicTimer = new LogicTimer(() => FixedTime());
        logicTimer.Start();
    }

    // Update is called once per frame
    void Update()
    {
        if (isRunning)
        {
            gameLogic.SetClock(gameLogic.Clock + Time.deltaTime);
        }

        logicTimer.Update();
    }

    private void FixedTime()
    {
        if (!isRunning)
        {
            gameLogic.SetTick(0);
            return;
        }

        physicsScene.Simulate(logicTimer.FixedDeltaTime);

        SendServerTick();
        lagCompensation.UpdatePlayerRecords();
        gameLogic.SetTick(gameLogic.Tick + 1);
    }

    private void SendServerTick()
    {
        foreach ( Player p in players.Values)
        {
            p.tick = gameLogic.Tick;
        }

        tickCountdown += Time.fixedDeltaTime;
        if (tickCountdown >= tickCountdownLimit)
        {
            tickCountdown = 0;
            RoomSend.ServerTick(room.RoomId, gameLogic.Tick, gameLogic.Clock);
        }
    }

    public void Initialize(Guid _roomId)
    {
        physicsScene = gameObject.scene.GetPhysicsScene();
        gameLogic = new GameLogic();
        lagCompensation = new LagCompensation(this);

        room = Server.Rooms[_roomId];
        gameLogic.SetClock(0);
        isRunning = false;

        qualifiedPlayers = 0;
        playerCheckPoint = new Dictionary<Guid, int>();

        players = new Dictionary<Guid, Player>();

        spawns = new Vector3[60];
        for (int i = 0; i <= 3; i++)
        {
            for (int j = 0; j <= 14; j++)
                spawns[(15 * i) + j] = new Vector3(-18.06f + 2.58f * j, 0.5f, -2.58f * i);
        }

        SpawnPlayers();

        if (ServerData.LAG_COMPENSATION)
            lagCompensation.Start();
    }

    public void StartCountdown()
    {
        timer.StartTimer(1, 3);
    }
    
    public void StartRace()
    {
        isRunning = true;

        foreach( Player p in players.Values)
            p.StartPlayer(isRunning);
    }

    public void StopRace()
    {
        isRunning = false;

        foreach (Player p in players.Values)
            p.StartPlayer(isRunning);
    }

    public void SpawnPlayers()
    {
        foreach(Client client in room.Clients.Values)
        {
            Player p = SpawnPlayer(client.Id, client.SpawnId);
            room.Clients[client.Id].SetPlayer(p);
            players.Add(client.Id, p);
            playerCheckPoint.Add(client.Id, 0);
        }
    }
    
    public Player SpawnPlayer(Guid _playerId, int _spawnId)
    {
        playerObjs[_spawnId].gameObject.SetActive(true);
        Player p = playerObjs[_spawnId].GetComponent<Player>();
        p.Initialize(_playerId, room.RoomId);

        return p;
    }

    public void PlayerGrab(Guid _grabber, int _tick)
    {
        if (!gameLogic.playerInteraction)
            return;

        Guid grabbed = Guid.Empty;

        if (!players[_grabber].GetGrab())
        {
            // Check if lag compensation system is active
            if (lagCompensation.isActive)
            {
                lagCompensation.Backtrack(_grabber, _tick);

                // Check if player has someone to grab
                grabbed = players[_grabber].TryGrab();
                if (grabbed != Guid.Empty)
                    players[_grabber].Grab();

                lagCompensation.Restore(_grabber);
            }
            else
            {
                // Check if player has someone to grab
                grabbed = players[_grabber].TryGrab();
                if (grabbed != Guid.Empty)
                    players[_grabber].Grab();
            }
        }

        if(grabbed != Guid.Empty)
            RoomSend.PlayerGrab(room.RoomId, _grabber, grabbed);
    }

    public void PlayerLetGo(Guid _grabber, Guid _grabbed)
    {
        if (!gameLogic.playerInteraction)
            return;

        if (!players[_grabber] || !players[_grabbed])
            return;

        players[_grabber].LetGo();

        RoomSend.PlayerLetGo(room.RoomId, _grabber, _grabbed);
    }

    public void PlayerPush (Guid _pusher, int _tick)
    {
        if (!gameLogic.playerInteraction)
            return;

        Guid pushed = Guid.Empty;

        // Check if lag compensation system is active
        if (lagCompensation.isActive)
        {
            lagCompensation.Backtrack(_pusher, _tick);

            // Check if player has someone to push
            pushed = players[_pusher].TryPush();

            lagCompensation.Restore(_pusher);
        }
        else
        {
            // Check if player has someone to push
            pushed = players[_pusher].TryPush();
        }

        if (pushed != Guid.Empty)
            RoomSend.PlayerPush(room.RoomId, _pusher, pushed);
    }

    #region Respawn Player
    //Sent from other players to respawn
    public void PlayerRespawn(Guid _playerId)
    {
        Vector3 newPos = GetRespawnPosition(room.Clients[_playerId].SpawnId, playerCheckPoint[_playerId]);
        players[_playerId].ReceivedRespawn(newPos, Quaternion.identity);
        RoomSend.PlayerRespawn(room.RoomId, _playerId, playerCheckPoint[_playerId]);
    }

    private Vector3 GetRespawnPosition(int id, int checkPointNum)
    {
        if (checkPointNum == 0)
            return spawns[id];
        else
            return checkPoints[checkPointNum - 1].position;
    }
    #endregion

    public void FinishRacePlayer(Guid _clientId)
    {
        // Check if player has already finished
        if (!room.Clients[_clientId].finished)
        {
            room.Clients[_clientId].finished = true;
            RoomSend.PlayerFinish(room.RoomId, _clientId);
            players[_clientId].gameObject.SetActive(false);

            // Update number of qualified players
            qualifiedPlayers++;

            // Check if everyone has reach the finish line
            if (qualifiedPlayers >= players.Count)
                EndRace();
        }
    }

    public void EndRace()
    {
        room.EndGame();
    }

    public void SetCheckPoint(Guid _playerId, int newCheckPoint)
    {
        playerCheckPoint[_playerId] = newCheckPoint;
    }

    public void Reset()
    {
        foreach (Player p  in players.Values)
        {
            p.Reset(spawns[playerCheckPoint[p.Id]]);
            p.gameObject.SetActive(false);
        }

        players.Clear();
        playerCheckPoint.Clear();

        gameLogic.SetClock(0);
        isRunning = false;

        qualifiedPlayers = 0;

        lagCompensation.Stop();
    }

    public void Stop()
    {
        lagCompensation.Stop();
        StopRace();

        // Destroy game's scene
        PhysicsSceneManager.RemoveSimulation(room.RoomId);
    }
}
