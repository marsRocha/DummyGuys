using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interpolator : MonoBehaviour
{
    public float interpolation = 0.1f;

    private List<PlayerState> playerStates;

    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private float lastTime;

    private int lastTick;
    private float lastLerpAmount;

    private PlayerState currentPlayerState;

    [SerializeField] private float timeElapsed = 0f;
    [SerializeField] private float timeToReachTarget = 0.1f;

    [Header("Behaviours")]
    public bool isLocalPlayer = false;
    public bool Sync = false;

    // Start is called before the first frame update
    void Start()
    {
        if (isLocalPlayer)
            Sync = false;

        playerStates = new List<PlayerState>();

        lastPosition = transform.position;
        lastRotation = transform.rotation;
        lastTime = Time.time;

        lastTick = 0;
        lastLerpAmount = 0f;

        // The localPlayer uses a different tick
        int currentTick = isLocalPlayer ? 0 : GlobalVariables.clientTick - Utils.TimeToTicks(interpolation);
        if (currentTick < 0)
            currentTick = 0;

        currentPlayerState = new PlayerState(currentTick, Time.time, Time.time, transform.position, transform.position, transform.rotation, transform.rotation);
    }

    // Update is called once per frame
    void Update()
    {
        if (playerStates.Count <= 0)
            return;

        // Compute render timestamp.
        long render_timestamp = (long)(GlobalVariables.clientTick - (0.001f / Client.tickrate));

        // Find the two positions surrounding the rendering timestamp.
        // Drop older updates
        while (playerStates.Count >= 2 && playerStates[1].tick <= render_timestamp)
        {
            playerStates.RemoveAt(0);
        }

        // Interpolate between the two surrounding authoritative positions.
        if (playerStates.Count >= 2 && playerStates[0].tick <= render_timestamp && render_timestamp <= playerStates[1].tick) // :: Addition (instead of nested array, use struct to access named fields)
        {
            PlayerState from = playerStates[0];
            PlayerState to = playerStates[1];


            //rb.position = (x0 + (x1 - x0) * (render_timestamp - t0) / (t1 - t0));
            transform.position = Vector3.Lerp(from.position, to.position, Mathf.Abs((render_timestamp - from.time) / (to.time - from.time)));
            transform.rotation = Quaternion.Lerp(from.rotation, to.rotation, Mathf.Abs((render_timestamp - from.time) / (to.time - from.time)));
        }
    }

    // Updates are used to add a new tick to the list
    // the list is sorted and then set the last tick info to the respective variables

    internal void NewPlayerState(int _tick, Vector3 _position, Quaternion _rotation)
    {
        playerStates.Add(new PlayerState(_tick, Time.time, lastTime, _position, lastPosition, _rotation, lastRotation));

    }

}


//used to store incoming desired states for the player to be in
class PlayerState
{
    public int tick;

    public float time;
    public float lastTime;
    public Vector3 position;
    public Vector3 lastPosition;
    public Quaternion rotation;
    public Quaternion lastRotation;

    public static PlayerState zero = new PlayerState(0, 0, 0, Vector3.zero, Vector3.zero, Quaternion.identity, Quaternion.identity);

    internal PlayerState(int _tick, float _time, float _lastTime, Vector3 _position, Vector3 _lastPosition, Quaternion _rotation, Quaternion _lastRotation)
    {
        tick = _tick;
        time = _time;
        lastTime = _lastTime;
        position = _position;
        rotation = _rotation;
        lastPosition = _lastPosition;
        lastRotation = _lastRotation;
    }
}
