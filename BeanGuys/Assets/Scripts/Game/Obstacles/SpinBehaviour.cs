using UnityEngine;

public class SpinBehaviour : MonoBehaviour
{
    [SerializeField]
    private Rigidbody rb;
    [SerializeField]
    private float rotationSpeed;

    void Update()
    {
        rb.rotation = Quaternion.Euler(GameLogic.Clock * rotationSpeed, -90, -90);
        rb.angularVelocity = Vector3.right * (GameLogic.Clock > 0 ? 2f : 0f);
    }
}