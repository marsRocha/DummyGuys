using System;
using System.Collections.Generic;
using UnityEngine;

public class LagCompensation
{
    private RoomScene roomScene;
    private Dictionary<int, List<PlayerRecord>> playerRecords;
#pragma warning disable 0649
    private Dictionary<int, PlayerRecord> backupRecords;
#pragma warning restore 0649

    private static float maxLagComp = 1;
    public bool isActive { get; private set; }

    public LagCompensation(RoomScene _roomScene)
    {
        roomScene = _roomScene;
        isActive = false;
    }

    public void Start()
    {
        //Initialize playerRecords
        playerRecords = new Dictionary<int, List<PlayerRecord>>();
        foreach (Player player in roomScene.players.Values)
        {
            playerRecords.Add(player.Id, new List<PlayerRecord>());
        }
    }

    /// <summary>Clears & stops recording</summary>
    public void Stop()
    {
        isActive = false;
        playerRecords.Clear();
        playerRecords.Clear();
    }

    /// <summary>Adds new player records and deletes old ones</summary>
    public void UpdatePlayerRecords()
    {
        if (isActive)
        {
            // Loop through every player
            foreach (int _clientRoomId in playerRecords.Keys)
            {
                // Player doesnt exist, so clear all records
                if (!roomScene.players[_clientRoomId])
                {
                    playerRecords.Remove(_clientRoomId);
                    continue;
                }

                // Add a record this tick
                playerRecords[_clientRoomId].Add(new PlayerRecord(roomScene.players[_clientRoomId].transform.position, roomScene.players[_clientRoomId].transform.rotation, roomScene.players[_clientRoomId].tick));

                // Loop through every record
                for (int i = 0; i < playerRecords[_clientRoomId].Count; i++)
                {
                    // Check if the playerRecord doesnt exist or if the element doesnt exist
                    if (playerRecords[_clientRoomId][i] == null)
                        continue;

                    // Check difference with the server
                    if (roomScene.gameLogic.Tick - playerRecords[_clientRoomId][i].tick > Utils.timeToTicks(maxLagComp))
                    {
                        // Remove if the difference is to big
                        playerRecords[_clientRoomId].RemoveAt(i);
                    }
                }
            }
        }
    }

    #region Backtrack players (Backup, Backtrack and Restore)
    public void Backtrack(int _clientRoomId, int _tick, float _lerpAmount = 0.1f)
    {
        if (!roomScene.players[_clientRoomId])
            return;

        backupRecords = new Dictionary<int, PlayerRecord>();
        // Backtrack and backup the players
        foreach (Player player in roomScene.players.Values)
        {
            // Dont backtrack the player who requested the backtack
            if (player.Id == _clientRoomId)
                continue;

            Backup(player);
            BacktrackPlayer(player, _tick, _lerpAmount);
        }
    }

    private void Backup(Player _player)
    {
        backupRecords.Add(_player.Id, new PlayerRecord(_player.transform.position, _player.transform.rotation, _player.tick));
    }

    private void BacktrackPlayer(Player _player, int _tick, float _lerpAmount)
    {
        int currentRecord = -1;

        // Loop through records and find the current one            
        for (int i = 0; i < playerRecords[_player.Id].Count; i++)
        {
            if (playerRecords[_player.Id][i].tick == _tick)
            {
                currentRecord = i;
                break;
            }
        }

        // Record couldnt be found, so we cant backtrack the player
        // so get the closest to the tick
        if (currentRecord <= -1)
        {
            float minDifference = float.MaxValue;

            // Loop through records and find the closest smaller one
            for (int i = 0; i < playerRecords[_player.Id].Count; i++)
            {
                float currentDifference = Mathf.Abs(_tick - playerRecords[_player.Id][i].tick);
                if (minDifference > currentDifference)
                {
                    currentRecord = i;
                    minDifference = currentDifference;
                }
            }
        }

        // Record couldnt be found or the current record surpasses the amount of player records,
        // so we cant backtrack the player, return
        if (currentRecord <= -1 || currentRecord >= playerRecords[_player.Id].Count)
            return;

        PlayerRecord record = playerRecords[_player.Id][currentRecord];
        if (record == null)
            return;


        // There is no next record, so just use the current record values
        if (currentRecord + 1 >= playerRecords[_player.Id].Count)
        {
            _player.transform.position = record.position;
            _player.transform.rotation = record.rotation;
            return;
        }

        PlayerRecord nextRecord = playerRecords[_player.Id][currentRecord + 1];

        // Set player position and rotation
        _player.transform.position = Vector3.Lerp(record.position, nextRecord.position, _lerpAmount);
        _player.transform.rotation = Quaternion.Lerp(record.rotation, nextRecord.rotation, _lerpAmount);
    }

    /// <summary>Restore players back to their positions, with the exception of whom asked for the backtrack</summary>
    /// <param name="_playerException">player who requested backtrack</param>
    public void Restore(int _playerException)
    {
        foreach (Player player in roomScene.players.Values)
        {
            if (player.Id == _playerException)
                continue;

            player.transform.position = backupRecords[player.Id].position;
            player.transform.rotation = backupRecords[player.Id].rotation;
        }

        backupRecords.Clear();
    }
    #endregion
}