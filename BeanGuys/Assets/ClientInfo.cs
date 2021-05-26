using System;
using UnityEngine;

public class ClientInfo : MonoBehaviour
{
    public Guid id;
    public string username;
    public int spawnId;

    public ClientInfo(Guid _id, string _username, int _spawnId)
    {
        id = _id;
        username = _username;
        spawnId = _spawnId;
    }
}
