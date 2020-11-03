using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public Transform ToFollow;
    private Vector3 targetPosition;

    public Vector3 FollowingOffset = new Vector3(0f, 1.5f, 0f);
    public Vector3 FollowingOffsetDirection = new Vector3(0f, 0f, 0f);

    [Header("Clamp values for zoom of camera")]
    public Vector2 DistanceRanges = new Vector2(5f, 10f);
    private float targetDistance;
    private float animatedDistance;

    public Vector2 RotationRanges = new Vector2(-60f, 60f);
    private Vector2 targetSphericRotation = new Vector2(0f, 0f);
    private Vector2 animatedSphericRotation = new Vector2(0f, 0f);

    public float RotationSensitivity = 10f;

    [Range(0.1f, 1f)]
    public float RotationSpeed = 1f;
    [Range(0f, 1f)]
    public float HardFollowValue = 1f;
    public float ScrollSpeed = 5f;

    public bool LockCursor = true;

    private bool rotateCamera = true;
    private RaycastHit sightObstacleHit;

    //Layer mask to check obstacles in sight ray
    public LayerMask SightLayerMask;
    //How far forward raycast should check collision for camera
    public float CollisionOffset = 1f;


    void Start()
    {
        targetDistance = (DistanceRanges.x + DistanceRanges.y) / 2;
        animatedDistance = DistanceRanges.y;

        targetSphericRotation = new Vector2(0f, 23f);
        animatedSphericRotation = targetSphericRotation;

        if (LockCursor)
        {
            HelperSwitchCursor();
        }
    }

    void LateUpdate()
    {
        if (ToFollow)
        {
            InputCalculations();
            ZoomCalculations();
            FollowCalculations();
            RaycastCalculations();
            SwitchCalculations();
        }
    }

    void InputCalculations()
    {
        targetDistance -= (Input.GetAxis("Mouse ScrollWheel") * ScrollSpeed);

        if (!rotateCamera) return;

        targetSphericRotation.x += Input.GetAxis("Mouse X") * RotationSensitivity;
        targetSphericRotation.y -= Input.GetAxis("Mouse Y") * RotationSensitivity;
    }

    private void ZoomCalculations()
    {
        if (!sightObstacleHit.transform) 
            targetDistance = Mathf.Clamp(targetDistance, DistanceRanges.x, DistanceRanges.y);

        animatedDistance = Mathf.Lerp(animatedDistance, targetDistance, Time.deltaTime * 8f);
    }

    private void FollowCalculations()
    {
        targetSphericRotation.y = HelperClampAngle(targetSphericRotation.y, RotationRanges.x, RotationRanges.y);

        if (RotationSpeed < 1f) animatedSphericRotation = new Vector2(Mathf.LerpAngle(animatedSphericRotation.x, targetSphericRotation.x, Time.deltaTime * 30 * RotationSpeed), Mathf.LerpAngle(animatedSphericRotation.y, targetSphericRotation.y, Time.deltaTime * 30 * RotationSpeed)); else animatedSphericRotation = targetSphericRotation;

        Quaternion rotation = Quaternion.Euler(animatedSphericRotation.y, animatedSphericRotation.x, 0f);
        transform.rotation = rotation;

        Vector3 targetPosition = ToFollow.transform.position + FollowingOffset;

        if (HardFollowValue < 1f)
        {
            float lerpValue = Mathf.Lerp(0.5f, 40f, HardFollowValue);
            targetPosition = Vector3.Lerp(this.targetPosition, targetPosition, Time.deltaTime * lerpValue);
        }

        this.targetPosition = targetPosition;
    }

    private void RaycastCalculations()
    {
        Vector3 followPoint = ToFollow.transform.position + FollowingOffset + transform.TransformVector(FollowingOffsetDirection);
        Quaternion cameraDir = Quaternion.Euler(targetSphericRotation.y, targetSphericRotation.x, 0f);
        Ray directionRay = new Ray(followPoint, cameraDir * -Vector3.forward);

        // If there is something in sight ray way
        if (Physics.Raycast(directionRay, out sightObstacleHit, targetDistance + CollisionOffset, SightLayerMask, QueryTriggerInteraction.Ignore))
        {
            transform.position = sightObstacleHit.point - directionRay.direction * CollisionOffset;
        }
        else
        {
            Vector3 rotationOffset = transform.rotation * -Vector3.forward * animatedDistance;
            transform.position = targetPosition + rotationOffset + transform.TransformVector(FollowingOffsetDirection);
        }
    }

    //Switching cursor state
    private void SwitchCalculations()
    {
        if (LockCursor)
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                HelperSwitchCursor();
                if (Cursor.visible) rotateCamera = false; else rotateCamera = true;
            }
        }
    }

    #region Ultilities

    //Clamping angle in 360 circle
    private float HelperClampAngle(float angle, float min, float max)
    {
        if (angle < -360)
            angle += 360.0f;

        if (angle > 360)
            angle -= 360.0f;

        return Mathf.Clamp(angle, min, max);
    }

    //Switching cursor state for right work of camera rotating mechanics
    private void HelperSwitchCursor()
    {
        if (Cursor.visible)
        {
            if (Application.isFocused)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    #endregion
}
