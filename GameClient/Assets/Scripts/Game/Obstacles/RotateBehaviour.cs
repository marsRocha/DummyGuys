﻿using UnityEngine;

public class RotateBehaviour : Obstacle
{
#pragma warning disable 0649
    [SerializeField]
    private Rigidbody rb;
    [SerializeField]
    private float rotationSpeed;
    [SerializeField]
    private bool x, y, z;
#pragma warning restore 0649

    // Start is called before the first frame update
    private void Start()
    {
        rb.centerOfMass = Vector3.zero;
    }

    // Update is called once per frame
    void Update()
    {
        if (mapController.isRunning)
        {
            rb.rotation = Quaternion.Euler(x ? (mapController.gameLogic.Clock * rotationSpeed) : 0f, y ? (mapController.gameLogic.Clock * rotationSpeed) : 0f, z ? (mapController.gameLogic.Clock * rotationSpeed) : 0f);
            rb.angularVelocity = transform.up * (mapController.gameLogic.Clock > 0 ? 2f : 0f);
        }
    }
}
