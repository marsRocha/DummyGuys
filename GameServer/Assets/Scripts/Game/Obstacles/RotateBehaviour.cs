using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateBehaviour : MonoBehaviour
{
    private RoomScene roomScene;

    public Rigidbody rb;
    public float rotationSpeed;

    // Start is called before the first frame update
    private void Start()
    {
        roomScene = gameObject.scene.GetRootGameObjects()[0].GetComponent<RoomScene>();

        rb.centerOfMass = Vector3.zero;
    }

    // Update is called once per frame
    void Update()
    {
        rb.rotation = Quaternion.Euler(0f, roomScene.Game_Clock * rotationSpeed, 0f);
        rb.angularVelocity = transform.up * (roomScene.Game_Clock > 0 ? 2f : 0f);
    }
}
