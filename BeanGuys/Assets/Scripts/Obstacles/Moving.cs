using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Moving : MonoBehaviour
{
    private Rigidbody rb;
    public float speed, somthing;
    private Vector3 currentPoint, initialPos;
    public Vector3[] points;
    private int currentIndex;

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
        transform.position = Vector3.Lerp(points[0], points[1], ((Mathf.Sin(MapController.instance.Game_Clock * speed + somthing) + 1.0f) / 2.0f));
    }

    private void Update()
    {
        /*if (Vector3.Distance(transform.position, currentPoint) <= 0.5f) //going to point1
        {
            currentIndex++;
            if (currentIndex >= points.Length)
                currentIndex = 0;
            currentPoint = (initialPos + points[currentIndex]);
        }*/
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        foreach (Vector3 p in points)
            Gizmos.DrawSphere(p, 0.1f);
    }
}
