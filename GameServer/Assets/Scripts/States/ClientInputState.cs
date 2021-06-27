using UnityEngine;

//State sent from the client, includes inputs and result position and rotation used to check if needs to be corrected
public class ClientInputState
{
    public float Tick;
    public int SimulationFrame;

    public float HorizontalAxis, VerticalAxis;
    public bool Jump, Dive;
    public Quaternion LookingRotation;

    public Vector3 position;
    public Quaternion rotation;
    public bool ragdoll;
}

//TODO: TICK IS A FLOAT BUT IS USED AS AN INTEGER
