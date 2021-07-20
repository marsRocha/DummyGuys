using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomScene : MonoBehaviour
{
    private Room room;
    private LogicTimer logicTimer;

    //Objects inside Scene
    [HideInInspector]
    public Vector3[] spawns;
    public Transform[] checkPoints;
    [SerializeField]
    private Transform[] playerObjs;

    public Dictionary<Guid, Player> players;

    //Controls game
    public float Game_Clock;
    public int tick;
    public bool isRunning { get; private set; } = false;

    //Stores every player's info on checkpoint
    public Dictionary<Guid, int> playerCheckPoint;

    private int qualifiedPlayers;
    private int totalPlayers;

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
            Game_Clock += Time.deltaTime;
        }

        logicTimer.Update();
    }

    private void FixedTime()
    {
        if (!isRunning)
        {
            tick = 0;
            return;
        }

        ServerTime();
        tick++;
    }

    private void ServerTime()
    {
        foreach ( Player p in players.Values)
        {
            p.tick = tick;
        }
        RoomSend.ServerTick(room.RoomId, tick, Game_Clock);
    }

    public void Initialize(Guid _roomId)
    {
        room = Server.Rooms[_roomId];
        Game_Clock = 0;
        isRunning = false;

        qualifiedPlayers = 0;
        totalPlayers = room.Clients.Count;
        playerCheckPoint = new Dictionary<Guid, int>();

        players = new Dictionary<Guid, Player>();
        //TODO: REMOVE THIS FROM HERE ONCE WE HAVE PLAYERS ALREADY INSTANTIATED ON SCENE
        spawns = new Vector3[60];
        for (int i = 0; i <= 3; i++)
        {
            for (int j = 0; j <= 14; j++)
                spawns[(14 * i) + j] = new Vector3(-18.06f + 2.58f * j, 0.5f, -2.58f * i);
        }
    }

    public void StartRace()
    {
        isRunning = true;

        foreach( Player p in players.Values)
            p.StartPlayer();
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

        Debug.Log("Players ready!");
    }
    
    public Player SpawnPlayer(Guid _playerId, int _spawnId)
    {
        playerObjs[_spawnId].gameObject.SetActive(true);
        Player p = playerObjs[_spawnId].GetComponent<Player>();
        p.Initialize(_playerId, room.RoomId);

        return p;
    }

    #region Respawn Player
    //Sent from other players to respawn
    public void PlayerRespawn(Guid _playerId)
    {
        Vector3 newPos = GetRespawnPosition(room.Clients[_playerId].SpawnId, playerCheckPoint[_playerId]);
        players[_playerId].Respawn(newPos, Quaternion.identity);
        RoomSend.PlayerRespawn(room.RoomId, _playerId, playerCheckPoint[_playerId]);
        Debug.Log("Sent respawn");
    }

    private Vector3 GetRespawnPosition(int id, int checkPointNum)
    {
        Debug.Log($"numCheck:{checkPointNum}");
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
        Debug.Log("Race finished. Go to main menu.");
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

        Game_Clock = 0;
        isRunning = false;

        qualifiedPlayers = 0;
        totalPlayers = room.Clients.Count;
    }

    public void Stop()
    {
        // Destroy game's scene
        PhysicsSceneManager.RemoveSimulation(room.RoomId);
    }
}
