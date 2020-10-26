using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Swing : MonoBehaviour
{
    public float speed = 5f;
    private float startTime;
    public float angle = 30;
    public float timeToWait = 1f;
    public bool Left;

    Quaternion start, end;

    // Start is called before the first frame update
    void Start()
    {
        startTime = 0;
        start = SwingRotation(angle);
        end = SwingRotation(-angle);

        StartCoroutine(WaitFor(timeToWait));
    }

    // Update is called once per frame
    void FixedUpdate()
    {

        startTime += Time.deltaTime;
        if(Left)
            transform.rotation = Quaternion.Lerp(start, end, (Mathf.Sin( startTime * speed + Mathf.PI / 2) + 1.0f) / 2.0f);
        else
            transform.rotation = Quaternion.Lerp(end, start, (Mathf.Sin(startTime * speed + Mathf.PI / 2) + 1.0f) / 2.0f);
    }

    private IEnumerator WaitFor(float time)
    {
        yield return new WaitForSecondsRealtime(time);
    }

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
