using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    private Rigidbody rb;
    public float speed;
    private Vector3 currentPoint, initialPos;
    public Vector3[] points;
    private int currentIndex;
    public bool showPoints;

    // Start is called before the first frame update
    void Start()
    {
        initialPos = this.transform.position;
        rb = GetComponent<Rigidbody>();
        currentIndex = 0;
        currentPoint = (initialPos + points[currentIndex]);
    }

    void FixedUpdate()
    {
        rb.velocity = (currentPoint - this.transform.position).normalized * speed * Time.deltaTime;
    }

    private void Update()
    {
        if ( Vector3.Distance(transform.position, currentPoint) <= 0.1f) //going to point1
        {
            currentIndex++;
            if (currentIndex >= points.Length)
                currentIndex = 0;
            currentPoint = (initialPos + points[currentIndex]);
        }
    }

    private void OnDrawGizmos()
    {
        if (showPoints)
        {
            Gizmos.color = Color.red;

            foreach (Vector3 p in points)
                Gizmos.DrawSphere(this.transform.position + p, 1);
        }
    }
}
