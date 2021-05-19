using UnityEngine;

public class ClientInputState
{
    public float simulationFrame;

    public float HorizontalAxis;
    public float VerticalAxis;
    public bool jump, dive;

    public Vector3 position;
    public Quaternion rotation;
    public Vector3 velocity;
    public Vector3 angular_velocity;

    /*public ClientInputState(float tick_number, int x, int y, bool jump, bool dive, Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angular_velocity)
    {
        this.simulationFrame = tick_number;

        this.HorizontalAxis = x;
        this.VerticalAxis = y;
        this.jump = jump;
        this.dive = dive;

        this.position = position;
        this.rotation = rotation;
        this.velocity = velocity;
        this.angular_velocity = angular_velocity;
    }*/
}
