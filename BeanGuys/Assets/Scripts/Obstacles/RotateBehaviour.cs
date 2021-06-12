using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateBehaviour : MonoBehaviour
{
    public Rigidbody rb;
    public float rotationSpeed;

    private void Start()
    {
        rb.centerOfMass = Vector3.zero;
    }
    void Update()
    {
        rb.rotation = Quaternion.Euler(0f, MapController.instance.Game_Clock * rotationSpeed, 0f);
        rb.angularVelocity = transform.up * (MapController.instance.Game_Clock > 0 ? 2f : 0f);
    }
}
