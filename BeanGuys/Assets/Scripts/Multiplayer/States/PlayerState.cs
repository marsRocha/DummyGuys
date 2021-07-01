using UnityEngine;

/// <summary>
/// Contains all methods needed information about a player in a determined moment of the simulation.
/// If ragdolled, then it will be stored the pelvis(root) position, rotation, velocity
/// </summary>
public class PlayerState
{
    public static PlayerState zero = new PlayerState(0, Vector3.zero, Quaternion.identity, false, 0);

    public int tick;
    public Vector3 position;
    public Quaternion rotation;
    public bool ragdoll;
    public int animation; // Animations: 0 - idle, 1 - running, 2 - jumping, 3 - diving

    public PlayerState(int _tick, Vector3 _position, Quaternion _rotation, bool _ragdoll, int _animation)
    {
        tick = _tick;
        position = _position;
        rotation = _rotation;
        ragdoll = _ragdoll;
        animation = _animation;
    }
}