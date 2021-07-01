using UnityEngine;

/// <summary>
/// Contains all methods needed information about a player in a determined moment of the simulation.
/// If ragdolled, then it will be stored the pelvis(root) position, rotation, velocity
/// </summary>
public class SimulationState
{
    public int simulationFrame;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 velocity;
    public bool ragdoll;

    public SimulationState(int _simulationFrame, Vector3 _position, Quaternion _rotation, Vector3 _velocity, bool _ragdoll)
    {
        simulationFrame = _simulationFrame;
        position = _position;
        rotation = _rotation;
        velocity = _velocity;
        ragdoll = _ragdoll;
    }

    public SimulationState()
    {
    }
}
