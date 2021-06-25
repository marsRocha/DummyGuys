using System;
using UnityEngine;

/// <summary>
/// Contains all methods to control a remote player.
/// </summary>
public class RemotePlayerManager : MonoBehaviour
{
    public Guid Id;
    public string Username;

    private Interpolator interpolator;

    // Start is called before the first frame update
    void Start()
    {
        interpolator = GetComponent<Interpolator>();
    }

    /// <summary>Setup remote player's information.</summary>
    /// <param name="_id">The id of the player.</param>
    /// <param name="_username">The username of the player.</param>
    public void Initialize(Guid _id, string _username)
    {
        Id = _id;
        Username = _username;
    }

    /// <summary>Adds a newly received player state to the player's interpolator.</summary>
    /// <param name="_tick">Tick of the player state.</param>
    /// <param name="_position">Position of the player state.</param>
    /// <param name="_rotation">Rotation of the player state.</param>
    public void NewPlayerState(int _tick, Vector3 _position, Quaternion _rotation)
    {
        interpolator.NewPlayerState(_tick, _position, _rotation);
    }
}
