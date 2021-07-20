using UnityEngine;

public class RotateBehaviour : MonoBehaviour
{
    [SerializeField]
    private Rigidbody rb;
    [SerializeField]
    private float rotationSpeed;

    // Start is called before the first frame update
    private void Start()
    {
        rb.centerOfMass = Vector3.zero;
    }
    void Update()
    {
        rb.rotation = Quaternion.Euler(0f, GameLogic.Clock * rotationSpeed, 0f);
        rb.angularVelocity = transform.up * (GameLogic.Clock > 0 ? 2f : 0f);
    }
}
