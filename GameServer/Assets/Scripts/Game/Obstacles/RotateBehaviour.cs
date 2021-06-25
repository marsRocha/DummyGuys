using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateBehaviour : MonoBehaviour
{
    public Rigidbody rb;
    public float rotationSpeed;

    // Start is called before the first frame update
    private void Start()
    {
        rb.centerOfMass = Vector3.zero;
    }

    // Update is called once per frame
    void Update()
    {
        rb.rotation = Quaternion.Euler(0f, RoomScene.instance.Game_Clock * rotationSpeed, 0f);
        rb.angularVelocity = transform.up * (RoomScene.instance.Game_Clock > 0 ? 2f : 0f);
    }
}
