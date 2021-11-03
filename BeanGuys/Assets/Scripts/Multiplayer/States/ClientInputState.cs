using UnityEngine;

public class ClientInputState
{
    public int Tick; // Global clock
    public int SimulationFrame; // PlayerObject tick

    public float ForwardAxis, LateralAxis;
    public bool Jump, Dive;
    public Quaternion LookingRotation;
}