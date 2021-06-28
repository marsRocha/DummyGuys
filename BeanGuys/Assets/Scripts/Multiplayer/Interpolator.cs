using System.Collections.Generic;
using UnityEngine;

public class Interpolator : MonoBehaviour
{
    private List<PlayerState> futureTransformUpdates = new List<PlayerState>(); // Oldest first

    private PlayerState to;
    private PlayerState from;
    private PlayerState previous;

    [SerializeField] private float timeElapsed = 0f;
    [SerializeField] private float timeToReachTarget = 0.1f;

    private void Start()
    {
        to = new PlayerState(GameLogic.Tick, transform.position, transform.rotation);
        from = new PlayerState(GameLogic.InterpolationTick, transform.position, transform.rotation);
        previous = new PlayerState(GameLogic.InterpolationTick, transform.position, transform.rotation);
    }

    private void Update()
    {
        for (int i = 0; i < futureTransformUpdates.Count; i++)
        {
            if (GameLogic.Tick >= futureTransformUpdates[i].tick)
            {
                previous = to;
                to = futureTransformUpdates[i];
                from = new PlayerState(GameLogic.InterpolationTick, transform.position, transform.rotation);
                futureTransformUpdates.RemoveAt(i);
                timeElapsed = 0;
                timeToReachTarget = (to.tick - from.tick) * GameLogic.SecPerTick;
            }
        }

        timeElapsed += Time.deltaTime;
        Interpolate(timeElapsed / timeToReachTarget);
    }

    #region Interpolate
    private void Interpolate(float _lerpAmount)
    {
        InterpolatePosition(_lerpAmount);
        InterpolateRotation(_lerpAmount);
    }

    private void InterpolatePosition(float _lerpAmount)
    {
        if (to.position == previous.position)
        {
            // If this object isn't supposed to be moving, we don't want to interpolate and potentially extrapolate
            if (to.position != from.position)
            {
                // If this object hasn't reached it's intended position
                transform.position = Vector3.Lerp(from.position, to.position, _lerpAmount); // Interpolate with the _lerpAmount clamped so no extrapolation occurs
            }
            return;
        }

        transform.position = Vector3.LerpUnclamped(from.position, to.position, _lerpAmount); // Interpolate with the _lerpAmount unclamped so it can extrapolate
    }

    private void InterpolateRotation(float _lerpAmount)
    {
        if (to.rotation == previous.rotation)
        {
            // If this object isn't supposed to be rotating, we don't want to interpolate and potentially extrapolate
            if (to.rotation != from.rotation)
            {
                // If this object hasn't reached it's intended rotation
                transform.rotation = Quaternion.Slerp(from.rotation, to.rotation, _lerpAmount); // Interpolate with the _lerpAmount clamped so no extrapolation occurs
            }
            return;
        }

        transform.rotation = Quaternion.SlerpUnclamped(from.rotation, to.rotation, _lerpAmount); // Interpolate with the _lerpAmount unclamped so it can extrapolate
    }
    #endregion

    internal void NewPlayerState(int _tick, Vector3 _position, Quaternion _rotation)
    {
        if (_tick <= GameLogic.InterpolationTick)
        {
            return;
        }

        if (futureTransformUpdates.Count == 0)
        {
            futureTransformUpdates.Add(new PlayerState(_tick, _position, _rotation));
            return;
        }

        for (int i = 0; i < futureTransformUpdates.Count; i++)
        {
            if (_tick < futureTransformUpdates[i].tick)
            {
                // Transform update is older
                futureTransformUpdates.Insert(i, new PlayerState(_tick, _position, _rotation));
                break;
            }
        }
    }
}

/*using System.Collections.Generic;
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
        int currentTick = isLocalPlayer ? 0 : GameLogic.clientTick - Utils.TimeToTicks(interpolation);
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
        long render_timestamp = (long)(GameLogic.clientTick - (0.001f / Client.tickrate));

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
*/


//used to store incoming desired states for the player to be in
class PlayerState
{
    public float tick;
    public Vector3 position;
    public Quaternion rotation;

    public static PlayerState zero = new PlayerState(0, Vector3.zero, Quaternion.identity);

    internal PlayerState(float _tick, Vector3 _position, Quaternion _rotation)
    {
        tick = _tick;
        position = _position;
        rotation = _rotation;
    }
}