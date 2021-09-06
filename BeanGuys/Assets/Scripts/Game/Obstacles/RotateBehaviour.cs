using UnityEngine;

public class RotateBehaviour : MonoBehaviour
{
    [SerializeField]
    private Rigidbody rb;
    [SerializeField]
    private float rotationSpeed;
    [SerializeField]
    private bool x, y, z;

    // Start is called before the first frame update
    private void Start()
    {
        rb.centerOfMass = Vector3.zero;
    }

    // Update is called once per frame
    void Update()
    {
        rb.rotation = Quaternion.Euler(x ? (GameLogic.Clock * rotationSpeed) : 0f, y ? (GameLogic.Clock * rotationSpeed) : 0f, z ? (GameLogic.Clock * rotationSpeed) : 0f);
        rb.angularVelocity = transform.up * (GameLogic.Clock > 0 ? 2f : 0f);
    }
}
