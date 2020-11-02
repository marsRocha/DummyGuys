using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotatingCylinder : MonoBehaviour
{
    public Transform[] toRotate;
    public float[] speeds;

    [HideInInspector]
    public bool isRunning;

    // Start is called before the first frame update
    void Start()
    {
        isRunning = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (isRunning)
        {
            for (int i = 0; i < toRotate.Length; i++)
            {
                //toRotate[i].rotation = Quaternion.SetLookRotation(Vector3.up * speeds[i] * Time.deltaTime);
                toRotate[i].Rotate(Vector3.up * speeds[i] * Time.deltaTime);
            }
        }
    }
}
