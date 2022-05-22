using UnityEngine;

/// <summary>
/// Contains all methods to control the player's camera.
/// </summary>
public class PlayerCamera : MonoBehaviour
{
    // Bodys
    [SerializeField]
    private Transform player;
    [SerializeField]
    private Transform ragdoll;

    [HideInInspector]
    public bool followRagdoll;
    private Vector3 targetPosition;

    // Position
    [SerializeField]
    private Vector3 FollowingOffset = new Vector3(0f, 1.5f, 0f);
    [SerializeField]
    private Vector3 FollowingOffsetDirection = new Vector3(0f, 0f, 0f);

    [Header("Clamp values for zoom of camera")]
    private float targetDistance;
    private float animatedDistance;

    // Rotation
    [SerializeField]
    private Vector2 RotationRanges = new Vector2(-60f, 60f);
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

#pragma warning disable 0649
    // LayerMask to check obstacles in sight ray
    [SerializeField]
    private LayerMask SightLayerMask;
#pragma warning restore 0649
    // How far forward-raycast should check collision for camera
    [SerializeField]
    private float CollisionOffset = 1f;

    // Spectating
    private int playerIndex = 0;
    private bool spectating = false;

    void Start()
    {
        targetDistance = 10f;
        animatedDistance = 10f;

        targetSphericRotation = new Vector2(0f, 23f);
        animatedSphericRotation = targetSphericRotation;

        if (LockCursor)
        {
            HelperSwitchCursor();
        }
    }

    private void Update()
    {
        if (!player) return;

    }

    private void LateUpdate()
    {
        if (player && ragdoll)
        {
            InputCalculations();
            FollowCalculations();
            RaycastCalculations();
            SwitchCalculations();
        }

        if (spectating)
        {
            Spectating();
        }
    }

    public void SetFollowTargets(Transform _player, Transform _ragdoll)
    {
        player = _player;
        ragdoll = _ragdoll;
    }

    void InputCalculations()
    {
        targetDistance -= (Input.GetAxis("Mouse ScrollWheel") * ScrollSpeed);

        if (!rotateCamera) return;

        targetSphericRotation.x += Input.GetAxis("Mouse X") * RotationSensitivity;
        targetSphericRotation.y -= Input.GetAxis("Mouse Y") * RotationSensitivity;
    }

    private void FollowCalculations()
    {
        targetSphericRotation.y = HelperClampAngle(targetSphericRotation.y, RotationRanges.x, RotationRanges.y);

        if (RotationSpeed < 1f) animatedSphericRotation = new Vector2(Mathf.LerpAngle(animatedSphericRotation.x, targetSphericRotation.x, Time.deltaTime * 30 * RotationSpeed), Mathf.LerpAngle(animatedSphericRotation.y, targetSphericRotation.y, Time.deltaTime * 30 * RotationSpeed)); else animatedSphericRotation = targetSphericRotation;

        Quaternion rotation = Quaternion.Euler(animatedSphericRotation.y, animatedSphericRotation.x, 0f);
        transform.rotation = rotation;

        Vector3 targetPosition = (followRagdoll ? ragdoll.position : player.position) + FollowingOffset;

        if (HardFollowValue < 1f)
        {
            float lerpValue = Mathf.Lerp(0.5f, 40f, HardFollowValue);
            targetPosition = Vector3.Lerp(this.targetPosition, targetPosition, Time.deltaTime * lerpValue);
        }

        this.targetPosition = targetPosition;
    }

    private void RaycastCalculations()
    {
        Vector3 followPoint = (followRagdoll ? ragdoll.position : player.position) + FollowingOffset + transform.TransformVector(FollowingOffsetDirection);
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

    public void StopFollowMouse()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        rotateCamera = false;
    }

    public void StartFollowMouse()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        rotateCamera = true;
    }

    public void StartSpectating()
    {
        spectating = true;
        player = MapController.instance.GetPlayerTransform(playerIndex);
    }

    public void Spectating()
    {
        bool next = Input.GetKeyDown(KeyCode.Mouse0);
        bool previous = Input.GetKeyDown(KeyCode.Mouse1);

        if (next)
        {
            if (playerIndex + 1 < MapController.instance.players.Count)
                playerIndex++;
            else 
                playerIndex = 0;

            player = MapController.instance.GetPlayerTransform(playerIndex);
        }
        else if (previous)
        {
            if (playerIndex - 1 > 0)
                playerIndex--;
            else
                playerIndex = MapController.instance.players.Count - 1;

            player = MapController.instance.GetPlayerTransform(playerIndex);
        }
    }
    #endregion
}
