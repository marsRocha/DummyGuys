using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveBehaviour : MonoBehaviour
{
    private RoomScene roomScene;

    private Rigidbody rb;
    public float speed, offset;
    private Vector3 initialPos;
    public Vector3[] points;

    // Start is called before the first frame update
    void Start()
    {
        roomScene = gameObject.scene.GetRootGameObjects()[0].GetComponent<RoomScene>();

        initialPos = transform.position;
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        transform.position = Vector3.Lerp(initialPos + points[0], initialPos + points[1], ((Mathf.Sin((roomScene.Game_Clock + offset) * speed) + 1.0f) / 2.0f));
    }
}
