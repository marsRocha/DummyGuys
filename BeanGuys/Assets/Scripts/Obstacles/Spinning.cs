using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spinning : MonoBehaviour
{
    private MapController mapController;

    public HingeJoint hinge;
    private bool started = false;

    private void Start()
    {
        mapController = GameObject.Find("MapController").GetComponent<MapController>();
    }

    void Update()
    {
        if(mapController.isRunning && !started)
        {
            hinge.useMotor = true;
            started = true;
        }
        else if (started && mapController.isRunning)
        {
            hinge.useMotor = false;
            started = false;
        }
    }
}
