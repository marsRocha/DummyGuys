using System;

public class ClientInfo
{
    public readonly Guid id;
    public readonly string username;
    public readonly int spawnId;

    public ClientInfo(Guid _id, string _username, int _spawnId)
    {
        id = _id;
        username = _username;
        spawnId = _spawnId;
    }
}
