using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomScene : MonoBehaviour
{
    public static RoomScene instance;

    private Room room;

    //Objects inside Scene
    [HideInInspector]
    public Vector3[] spawns;
    public Transform[] checkPoints;

    public Dictionary<Guid, Player> players;

    //Controls game
    public float Game_Clock;
    public bool isRunning { get; private set; } = false;

    //Stores every player's info on checkpoint
    public Dictionary<Guid, int> playerCheckPoint;

    private int qualifiedPlayers;
    private int totalPlayers;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Debug.Log("Instance RoomScene already exists, destroying object.");
            Destroy(this);
        }
    }

    //TODO: REMOVE THIS FROM HERE ONCE WE HAVE PLAYERS ALREADY INSTANTIATED ON SCENE
    public void Initialize(Guid _roomId, Scene scene)
    {
        room = Server.Rooms[_roomId];
        players = new Dictionary<Guid, Player>();
        Game_Clock = 0;
        isRunning = false;

        qualifiedPlayers = 0;
        totalPlayers = room.ClientsInfo.Count;
        playerCheckPoint = new Dictionary<Guid, int>();

        spawns = new Vector3[60];
        for (int i = 0; i <= 3; i++)
        {
            for (int j = 0; j <= 14; j++)
                spawns[(14 * i) + j] = new Vector3(-18.06f + 2.58f * j, 0, -2.58f * i);
        }

        SpawnPlayers(scene);
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
        room.PlayerRespawn(_playerId, playerCheckPoint[_playerId]);
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

    public void FinishRacePlayer(Player _player)
    {
        UpdateQualified();
        room.PlayerFinish(_player.id, Game_Clock);
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
        room.EndGame();
    }

    //TODO: Should I send player checkpoint?
    public void SetCheckPoint(Guid _playerId, int newCheckPoint)
    {
        playerCheckPoint[_playerId] = newCheckPoint;
    }
}
