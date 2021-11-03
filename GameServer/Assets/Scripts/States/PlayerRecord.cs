using UnityEngine;

/// <summary>
/// Contains all information needed about a player position for Lag Compensation.
/// </summary>

public class PlayerRecord
{
    public Vector3 position;
    public Quaternion rotation;
    public int tick;

    public PlayerRecord()
    {
        position = new Vector3();
        rotation = new Quaternion();
        tick = new int();
    }
    public PlayerRecord(Vector3 _position, Quaternion _rotation, int _tick)
    {
        position = _position;
        rotation = _rotation;
        tick = _tick;
    }
}
