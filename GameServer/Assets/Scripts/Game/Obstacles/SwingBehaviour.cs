using UnityEngine;

public class SwingBehaviour : MonoBehaviour
{
    private RoomScene roomScene;

    [SerializeField]
    private float speed = 5f;
    [SerializeField]
    private float angle = 30;
    [SerializeField]
    private float offset;
    [SerializeField]
    private bool left;

    private Quaternion start, end;

    private void Awake()
    {
        start = SwingRotation(angle);
        end = SwingRotation(-angle);
    }

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
    void FixedUpdate()
    {
        if (left)
            transform.rotation = Quaternion.Lerp(start, end, ((Mathf.Sin((roomScene.Game_Clock + offset) * speed + Mathf.PI / 2) + 1.0f) / 2.0f));
        else
            transform.rotation = Quaternion.Lerp(end, start, ((Mathf.Sin((roomScene.Game_Clock + offset) * speed + Mathf.PI / 2) + 1.0f) / 2.0f));
    }

    // Calculate rotation needed to reach the desired max angle
    Quaternion SwingRotation(float angle)
    {
        Quaternion swingRot = transform.rotation;
        float angleZ = swingRot.eulerAngles.z + angle;

        if (angleZ > 180)
            angleZ -= 360;
        else if (angleZ < -180)
            angleZ += 360;

        swingRot.eulerAngles = new Vector3(swingRot.eulerAngles.x, swingRot.eulerAngles.y, angleZ);
        return swingRot;
    }
}
