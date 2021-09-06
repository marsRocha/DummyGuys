using UnityEngine;

public class MoveBehaviour : MonoBehaviour
{
    [SerializeField]
    private float speed, offset;
    private Vector3 initialPos;
    [SerializeField]
    private Vector3[] points;

    // Start is called before the first frame update
    void Start()
    {
        initialPos = transform.position;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        transform.position = Vector3.Lerp(initialPos + points[0], initialPos + points[1], ((Mathf.Sin((GameLogic.Clock + offset) * speed) + 1.0f) / 2.0f));
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        foreach (Vector3 p in points)
            Gizmos.DrawSphere(transform.position + p, 0.1f);
    }
}
