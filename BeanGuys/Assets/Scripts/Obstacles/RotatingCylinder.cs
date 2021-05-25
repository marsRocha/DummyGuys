using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotatingCylinder : MonoBehaviour
{
    public Rigidbody rb;
    public float rotationSpeed;

    void Update()
    {
        rb.rotation = Quaternion.Euler(MapController.instance.Game_Clock * rotationSpeed, -90, -90);
        rb.angularVelocity = Vector3.right * (MapController.instance.Game_Clock > 0 ? 2f : 0f);
    }
}