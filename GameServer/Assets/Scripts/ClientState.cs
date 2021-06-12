using UnityEngine;

//State sent from the client, includes inputs and result position and rotation used to check if needs to be corrected
public class ClientState
{
    public float Tick;
    public int SimulationFrame;
    public float HorizontalAxis, VerticalAxis;
    public bool Jump, Dive;
    public Quaternion LookingRotation;

    public Vector3 position;
    public Quaternion rotation;

    /*public ClientState(float tick, int simulationFrame, float x, float y, bool jump, bool dive, Quaternion lookingRotation, Vector3 position, Quaternion rotation)
    {
        this.Tick = tick;
        this.SimulationFrame = simulationFrame;
        this.HorizontalAxis = x;
        this.VerticalAxis = y;
        this.Jump = jump;
        this.Dive = dive;
        this.LookingRotation = rotation;
        this.position = position;
        this.rotation = rotation;
    }*/
}
