using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    public Guid id;
    public string username;

    public Player(Guid _id, string _username)
    {
        id = _id;
        username = _username;
        /*rotation = Quaternion.Identity;

        inputs = new bool[4];*/
    }

    /*public void Update()
    {
        Vector2 inputDirection = Vector2.Zero;
        if (inputs[0])
            inputDirection.Y += 1;
        if (inputs[1])
            inputDirection.Y -= 1;
        if (inputs[2])
            inputDirection.X += 1;
        if (inputs[3])
            inputDirection.X -= 1;

        Move(inputDirection);
    }

    //Calculate movement
    private void Move(Vector2 inputDirection)
    {
        Vector3 forward = Vector3.Transform(new Vector3(0, 0, 1), rotation);
        Vector3 right = Vector3.Normalize(Vector3.Cross(forward, new Vector3(0, 1, 0)));

        Vector3 moveDirection = right * inputDirection.X + forward * inputDirection.Y;
        position += moveDirection * moveSpeed;

        ServerSend.PlayerPosition(this);
        ServerSend.PlayerRotation(this);
        //server rotation does not happen here for that i have to look for client prediction and reconciliation to avoid jitering of player's screen
    }

    public void SetInput(bool[] _inputs, Quaternion _rotation)
    {
        inputs = _inputs;
        rotation = _rotation; 
    }*/
}
