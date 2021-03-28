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

    public int x { get; private set; } = 0;
    public int y { get; private set; } = 0;
    public bool jump { get; private set; } = false;
    public bool dive { get; private set; } = false;

    //For now, so i can test the inputs received over the network
    public bool controllable;

    public void MovementInput()
    {
        if (controllable)
        {
            //Walk
            x = (int)Input.GetAxis(forward);
            y = (int)Input.GetAxis(sideways);
            //Behaviours
            jump = Input.GetKey(jumpKey);
            dive = Input.GetKey(diveKey);
        }
    }

    public Inputs GetInputs()
    {
        return new Inputs(x, y, jump, dive);
    }
}
