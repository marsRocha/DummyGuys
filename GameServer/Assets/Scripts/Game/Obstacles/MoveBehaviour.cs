using UnityEngine;

public class MoveBehaviour : Obstacle
{
    public float speed, offset;
    private Vector3 initialPos;
    public Vector3[] points;

    // Start is called before the first frame update
    private void Awake()
    {
        initialPos = transform.position;
    }

    void FixedUpdate()
    {
        if(roomScene.isRunning)
            transform.position = Vector3.Lerp(initialPos + points[0], initialPos + points[1], ((Mathf.Sin((roomScene.gameLogic.Clock + offset) * speed) + 1.0f) / 2.0f));
    }
}
