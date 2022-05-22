using System.Collections.Generic;
using UnityEngine;

public class Interpolator : MonoBehaviour
{
    private RemotePlayerManager remotePlayer;

    private List<PlayerState> futureTransformUpdates; // Oldest first

    private PlayerState to;
    private PlayerState from;
    private PlayerState previous;

    [SerializeField] private float timeElapsed = 0f;
    [SerializeField] private float timeToReachTarget = 0.1f;
    private float lerpPeriod = 0.1f;

    public void StartInterpolator(RemotePlayerManager _remotePlayer)
    {
        remotePlayer = _remotePlayer;

        futureTransformUpdates = new List<PlayerState>();

        to = new PlayerState(MapController.instance.gameLogic.Tick, transform.position, transform.rotation, remotePlayer.Ragdolled, remotePlayer.currentAnimation);
        from = new PlayerState(MapController.instance.gameLogic.InterpolationTick, transform.position, transform.rotation, remotePlayer.Ragdolled, remotePlayer.currentAnimation);
        previous = new PlayerState(MapController.instance.gameLogic.InterpolationTick, transform.position, transform.rotation, remotePlayer.Ragdolled, remotePlayer.currentAnimation);
    }

    private void Update()
    {
        for (int i = 0; i < futureTransformUpdates.Count; i++)
        {
            if (MapController.instance.gameLogic.Tick >= futureTransformUpdates[i].tick)
            {
                previous = to;
                to = futureTransformUpdates[i];
                from = new PlayerState(MapController.instance.gameLogic.InterpolationTick, 
                    transform.position, transform.rotation, remotePlayer.Ragdolled, remotePlayer.currentAnimation);
                futureTransformUpdates.RemoveAt(i);
                timeElapsed = 0;
                timeToReachTarget = ((to.tick - from.tick) * MapController.instance.gameLogic.SecPerTick) + lerpPeriod;

                // Ragdoll state, if not in the correct ragdoll state then transition to it
                if (from.ragdoll != to.ragdoll)
                    remotePlayer.SetRagdoll(to.ragdoll);

                // Animations
                remotePlayer.SetAnimation(to.animation);
            }
        }

        timeElapsed += Time.deltaTime;
        Interpolate(timeElapsed / timeToReachTarget);
    }

    internal void NewPlayerState(int _tick, Vector3 _position, Quaternion _rotation, bool _ragdoll, int _animation)
    {
        /*if (_tick <= GameLogic.InterpolationTick)
        {
            return;
        }*/

        if (futureTransformUpdates.Count == 0)
        {
            futureTransformUpdates.Add(new PlayerState(_tick, _position, _rotation, _ragdoll, _animation));
            return;
        }

        for (int i = 0; i < futureTransformUpdates.Count; i++)
        {
            if (_tick < futureTransformUpdates[i].tick)
            {
                // Transform update is older
                futureTransformUpdates.Insert(i, new PlayerState(_tick, _position, _rotation, _ragdoll, _animation));
                break;
            }
        }
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
}