using UnityEngine;

public class ClientInputState
{
    public float Tick; // Global clock
    public int SimulationFrame; // PlayerObject tick

    public float HorizontalAxis, VerticalAxis;
    public bool Jump, Dive;
    public Quaternion LookingRotation;
}