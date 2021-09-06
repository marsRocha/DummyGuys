using UnityEngine;

public class RotateBehaviour : MonoBehaviour
{
    private RoomScene roomScene;

    [SerializeField]
    private Rigidbody rb;
    [SerializeField]
    private float rotationSpeed;
    [SerializeField]
    private bool x, y, z;

    // Start is called before the first frame update
    private void Start()
    {
        foreach (GameObject obj in gameObject.scene.GetRootGameObjects())
        {
            if (obj.GetComponent<RoomScene>())
            {
                roomScene = obj.GetComponent<RoomScene>();
                break;
            }
        }

        rb.centerOfMass = Vector3.zero;
    }

    // Update is called once per frame
    void Update()
    {
        rb.rotation = Quaternion.Euler(x ? (roomScene.Game_Clock * rotationSpeed) : 0f, y ? (roomScene.Game_Clock * rotationSpeed) : 0f, z ? (roomScene.Game_Clock * rotationSpeed) : 0f);
        rb.angularVelocity = transform.up * (roomScene.Game_Clock > 0 ? 2f : 0f);
    }
}
