using UnityEngine;

public class SpinBehaviour : MonoBehaviour
{
    private RoomScene roomScene;

    public Rigidbody rb;
    public float rotationSpeed;

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
    }

    // Update is called once per frame
    void Update()
    {
        rb.rotation = Quaternion.Euler(roomScene.Game_Clock * rotationSpeed, -90, -90);
        rb.angularVelocity = Vector3.right * (roomScene.Game_Clock > 0 ? 2f : 0f);
    }
}
