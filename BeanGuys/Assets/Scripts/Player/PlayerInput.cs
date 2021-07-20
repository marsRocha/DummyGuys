using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    [Header("Inputs")]
    public string forward;
    public string sideways;
    public KeyCode jumpKey;
    public KeyCode diveKey;

    public float x { get; private set; } = 0;
    public float y { get; private set; } = 0;
    public bool jump { get; private set; } = false;
    public bool dive { get; private set; } = false;

    private Inputs inputState;
    public void MovementInput()
    {
        // Walk
        x = Input.GetAxis(forward);
        y = Input.GetAxis(sideways);
        // Behaviours
        jump = Input.GetKey(jumpKey);
        dive = Input.GetKey(diveKey);

        // Set input
        inputState = new Inputs
        {
            HorizontalAxis = x,
            VerticalAxis = y,
            Jump = jump,
            Dive = dive
        };
    }

    public Inputs GetInputs()
    {
        return inputState;
    }
}

public class Inputs{
    public float HorizontalAxis, VerticalAxis;
    public bool Jump, Dive;
}
