using UnityEngine;

public class SimulationState
{
    public int simulationFrame;
    public Vector3 position, velocity;
    public Quaternion rotation;

    public SimulationState(Vector3 position, Quaternion rotation, Vector3 velocity, int simulationFrame)
    {
        this.position = position;
        this.rotation = rotation;
        this.velocity = velocity;
        this.simulationFrame = simulationFrame;
    }
}