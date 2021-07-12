using UnityEngine;

public class SpinBehaviour : MonoBehaviour
{
    private RoomScene roomScene;

    public Rigidbody rb;
    public float rotationSpeed;

    private void Start()
    {
        roomScene = gameObject.scene.GetRootGameObjects()[0].GetComponent<RoomScene>();
    }

    // Update is called once per frame
    void Update()
    {
        rb.rotation = Quaternion.Euler(roomScene.Game_Clock * rotationSpeed, -90, -90);
        rb.angularVelocity = Vector3.right * (roomScene.Game_Clock > 0 ? 2f : 0f);
    }
}
