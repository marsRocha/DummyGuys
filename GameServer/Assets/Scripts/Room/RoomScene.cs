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
        Debug.Log("Start");
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

    public void Initialize(Guid _roomId, Scene scene)
    {
        room = Server.Rooms[_roomId];
        Game_Clock = 0;
        isRunning = false;

        qualifiedPlayers = 0;
        totalPlayers = room.ClientsInfo.Count;
        playerCheckPoint = new Dictionary<Guid, int>();

        players = new Dictionary<Guid, Player>();
        //TODO: REMOVE THIS FROM HERE ONCE WE HAVE PLAYERS ALREADY INSTANTIATED ON SCENE
        spawns = new Vector3[60];
        for (int i = 0; i <= 3; i++)
        {
            for (int j = 0; j <= 14; j++)
                spawns[(14 * i) + j] = new Vector3(-18.06f + 2.58f * j, 0.5027312f, -2.58f * i);
        }

        SpawnPlayers(scene);
    }

    public void StartRace()
    {
        isRunning = true;

        foreach( Player p in players.Values)
            p.StartPlayer();
    }

    public void SpawnPlayers(Scene scene)
    {
        foreach(ClientInfo clientInfo in room.ClientsInfo.Values)
        {
            Player p = SpawnPlayer(clientInfo.id, clientInfo.spawnId);
            Server.Clients[clientInfo.id].SetPlayer(p);
            players.Add(clientInfo.id, p);
            playerCheckPoint.Add(clientInfo.id, 0);

            SceneManager.MoveGameObjectToScene(p.gameObject, scene);
        }

        Debug.Log("Players spawned");
    }
    
    public Player SpawnPlayer(Guid _playerId, int _spawnId)
    {
        Player p = ((GameObject)Instantiate(Resources.Load("Player", typeof(GameObject)), spawns[_spawnId], Quaternion.identity)).GetComponent<Player>();
        p.Initialize(_playerId);

        return p;
    }

    #region Respawn Player
    //Sent from other players to respawn
    public void PlayerRespawn(Guid _playerId)
    {
        Vector3 newPos = GetRespawnPosition(room.ClientsInfo[_playerId].spawnId, playerCheckPoint[_playerId]);
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
        if (!room.ClientsInfo[_clientId].finished)
        {
            room.ClientsInfo[_clientId].finished = true;
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
        //TODO: once players are already instanciated in the game this will have to be changed
        players = new Dictionary<Guid, Player>();
        playerCheckPoint = new Dictionary<Guid, int>();
        Game_Clock = 0;
        isRunning = false;

        qualifiedPlayers = 0;
        totalPlayers = room.ClientsInfo.Count;
    }

    public void Stop()
    {
        // Destroy game's scene
        PhysicsSceneManager.RemoveSimulation(room.RoomId);
    }
}
