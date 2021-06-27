using UnityEngine;

/// <summary>
/// Contains all methods needed information about a player in a determined moment of the simulation.
/// If ragdolled, then it will be stored the pelvis(root) position, rotation, velocity
/// </summary>
public class SimulationState
{
    public int simulationFrame;
    public Vector3 position, velocity;
    public Quaternion rotation;
    public bool ragdoll;

    public SimulationState(Vector3 _position, Quaternion _rotation, Vector3 _velocity, int _simulationFrame, bool _ragdoll)
    {
        position = _position;
        rotation = _rotation;
        velocity = _velocity;
        simulationFrame = _simulationFrame;
        ragdoll = _ragdoll;
    }

    public SimulationState()
    {
    }
}
