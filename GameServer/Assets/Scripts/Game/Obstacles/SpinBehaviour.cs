using UnityEngine;

public class SpinBehaviour : Obstacle
{
    public Rigidbody rb;
    public float rotationSpeed;

    // Update is called once per frame
    void Update()
    {
        if (roomScene.isRunning)
        {
            rb.rotation = Quaternion.Euler(roomScene.gameLogic.Clock * rotationSpeed, -90, -90);
            rb.angularVelocity = Vector3.right * (roomScene.gameLogic.Clock > 0 ? 2f : 0f);
        }
    }
}
