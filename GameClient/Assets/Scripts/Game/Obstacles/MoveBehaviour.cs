using UnityEngine;

public class MoveBehaviour : Obstacle
{
#pragma warning disable 0649
    [SerializeField]
    private float speed, offset;
    [SerializeField]
    private Vector3[] points;
#pragma warning restore 0649
    private Vector3 initialPos;


    // Start is called before the first frame update
    void Start()
    {
        initialPos = transform.position;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (mapController.isRunning)
        {
            transform.position = Vector3.Lerp(initialPos + points[0], initialPos + points[1], ((Mathf.Sin((mapController.gameLogic.Clock + offset) * speed) + 1.0f) / 2.0f));
        }
    }

    /*private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        foreach (Vector3 p in points)
            Gizmos.DrawSphere(transform.position + p, 0.1f);
    }*/
}
