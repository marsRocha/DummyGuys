using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpinBehaviour : MonoBehaviour
{
    public Rigidbody rb;
    public float rotationSpeed;

    void Update()
    {
        rb.rotation = Quaternion.Euler(GameLogic.Clock * rotationSpeed, -90, -90);
        rb.angularVelocity = Vector3.right * (GameLogic.Clock > 0 ? 2f : 0f);
    }
}