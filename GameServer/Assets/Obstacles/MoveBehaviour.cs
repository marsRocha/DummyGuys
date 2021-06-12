using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveBehaviour : MonoBehaviour
{
    private Rigidbody rb;
    public float speed, offset;
    private Vector3 initialPos;
    public Vector3[] points;
    private int currentIndex;

    // Start is called before the first frame update
    void Start()
    {
        initialPos = transform.position;
        rb = GetComponent<Rigidbody>();
        currentIndex = 0;
    }

    void FixedUpdate()
    {
        transform.position = Vector3.Lerp(initialPos + points[0], initialPos + points[1], ((Mathf.Sin((RoomScene.instance.Game_Clock + offset) * speed) + 1.0f) / 2.0f));
    }
}
