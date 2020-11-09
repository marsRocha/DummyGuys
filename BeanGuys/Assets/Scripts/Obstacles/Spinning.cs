using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spinning : MonoBehaviour
{
    public HingeJoint hinge;
    public bool isRunning = false;
    private bool started = false;

    // Update is called once per frame
    void Update()
    {
        if(isRunning && !started)
        {
            hinge.useMotor = true;
            started = true;
        }
    }
}
