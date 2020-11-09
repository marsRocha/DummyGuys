using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveWait : MonoBehaviour
{
    private Rigidbody rb;
    public float speed;
    private Vector3 currentPoint, initialPos;
    public Vector3[] points;
    private int currentIndex;
    public bool showPoints;
    public float timeToWait;
    private float currentWaitedTime;
    public bool move;
    //[HideInInspector]
    public bool isRunning = false;

    // Start is called before the first frame update
    void Start()
    {
        initialPos = this.transform.position;
        rb = GetComponent<Rigidbody>();
        move = true;
        currentWaitedTime = 0.0f;

        rb.isKinematic = true;
        rb.useGravity = false;

        currentIndex = 0;
        currentPoint = (initialPos + points[currentIndex]);
    }

    void FixedUpdate()
    {
        if(move && isRunning)
            rb.velocity = (currentPoint - this.transform.position).normalized * speed * Time.deltaTime;
    }

    void Update()
    {
        if (isRunning)
        {
            if (move)
            {
                if (rb.isKinematic)
                {
                    rb.isKinematic = false;
                    //rb.useGravity = true;
                }

                if (Vector3.Distance(transform.position, currentPoint) < 0.1f)
                {
                    move = false;
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }
            }
            else
            {
                currentWaitedTime += Time.deltaTime;

                if (currentWaitedTime > timeToWait)
                {
                    currentWaitedTime = 0.0f;

                    currentIndex++;
                    if (currentIndex >= points.Length)
                        currentIndex = 0;
                    currentPoint = (initialPos + points[currentIndex]);

                    move = true;
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (showPoints)
        {
            Gizmos.color = Color.red;

            foreach (Vector3 p in points)
                Gizmos.DrawSphere(this.transform.position + p, 0.1f);
        }
    }
}
