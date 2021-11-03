using UnityEngine;

public class SpinBehaviour : Obstacle
{
#pragma warning disable 0649
    [SerializeField]
    private Rigidbody rb;
    [SerializeField]
    private float rotationSpeed;
#pragma warning restore 0649

    void Update()
    {
        if (mapController.isRunning)
        {
            rb.rotation = Quaternion.Euler(mapController.gameLogic.Clock * rotationSpeed, -90, -90);
            rb.angularVelocity = Vector3.right * (mapController.gameLogic.Clock > 0 ? 2f : 0f);
        }
    }
}