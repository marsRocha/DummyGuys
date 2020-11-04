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
    public float timeToWait = 0;
    public bool wait = false;
    //[HideInInspector]
    public bool isRunning = false;

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
        if(!wait && isRunning)
            rb.velocity = (currentPoint - this.transform.position).normalized * speed * Time.deltaTime;
    }

    private void Update()
    {
        if (isRunning)
        {
            if (Vector3.Distance(transform.position, currentPoint) <= 0.5f) //going to point1
            {
                wait = true;
                rb.isKinematic = true;
                StartCoroutine(WaitFor(timeToWait));
            }
        }
    }

    private IEnumerator WaitFor(float time)
    {
        yield return new WaitForSeconds(time);

        currentIndex++;
        if (currentIndex >= points.Length)
            currentIndex = 0;
        currentPoint = (initialPos + points[currentIndex]);
        wait = false;
        rb.isKinematic = false;
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
