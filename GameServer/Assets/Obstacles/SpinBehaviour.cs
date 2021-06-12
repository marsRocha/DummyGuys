using UnityEngine;

public class SpinBehaviour : MonoBehaviour
{
    public Rigidbody rb;
    public float rotationSpeed;

    // Update is called once per frame
    void Update()
    {
        rb.rotation = Quaternion.Euler(RoomScene.instance.Game_Clock * rotationSpeed, -90, -90);
        rb.angularVelocity = Vector3.right * (RoomScene.instance.Game_Clock > 0 ? 2f : 0f);
    }
}
