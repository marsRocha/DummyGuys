using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Components")]
    public Transform pelvis;
    private CapsuleCollider cp;
    private Rigidbody rb;
    private PhysicsScene physicsScene;

    [Header("Movement Variables")]
    public float gravityForce;
    public float moveSpeed, turnSpeed;
    public float jumpForce, jumpCooldown;
    public float diveForwardForce, diveUpForce, diveCooldown;
    public float dashforce, dashTime;
    public Vector3 move;
    private ClientState inputs;

    [Header("Collision Variables")]
    public float checkDistance;
    public float checkDistanceLayed;
    public float getUpTime;
    public LayerMask collisionMask;
    private Vector3 colDir;
    private Vector3 groundNormal;

    [Header("States")]
    public bool grounded;
    public bool ragdolled, getUp;
    public bool jumping = false, diving = false, dashing = false;
    private bool readyToJump = true, readyToDive = true;
    private bool dashTriggered;

    // Start is called before the first frame update
    public void StartController()
    {
        move = new Vector3();
        cp = GetComponent<CapsuleCollider>();
        rb = GetComponent<Rigidbody>();
        physicsScene = gameObject.scene.GetPhysicsScene();
    }

    public void FixedUpdateController(ClientState _inputs)
    {
        inputs = _inputs;

        GroundChecking();

        //Extra gravity
        rb.AddForce(Vector3.down * gravityForce * Time.fixedDeltaTime);

        if (!ragdolled && !getUp)
        {
            Movement();
        }
    }

    #region Movement
    private Vector3 ToCameraSpace(Vector3 moveVector)
    {
        Vector3 camFoward = (inputs.LookingRotation * Vector3.forward);
        Vector3 camRight = (inputs.LookingRotation * Vector3.right);

        camFoward.y = 0;
        camRight.y = 0;

        camFoward.Normalize();
        camRight.Normalize();

        Vector3 moveDirection = (camFoward * moveVector.z + camRight * moveVector.x);

        return moveDirection;
    }

    private void Movement()
    {
        Dash();
        Jump();
        Dive();
        Walk();

        CounterMovement();
    }

    private void CounterMovement()
    {
        if (!dashing && !diving)
        {
            rb.AddForce(Vector3.right * -rb.velocity.x, ForceMode.VelocityChange);
            rb.AddForce(Vector3.forward * -rb.velocity.z, ForceMode.VelocityChange);
        }

        if (rb.velocity.y > 0f && !jumping && !diving)
            rb.AddForce(Vector3.up * -rb.velocity.y, ForceMode.VelocityChange);
        else // apply gravity on jump
            rb.AddForce(Vector3.down * gravityForce, ForceMode.Force);
    }

    private void GetUp()
    {
        getUp = false;
    }

    //MOVEMENT TYPES
    private void Walk()
    {
        if (!diving && !dashing)
        {
            move = new Vector3(inputs.HorizontalAxis, 0f, inputs.VerticalAxis);

            if (move.sqrMagnitude > 0.1f)
            {
                //Camera direction
                move = ToCameraSpace(move);
                Debug.DrawLine(this.transform.position, this.transform.position + move, Color.red, 1f);

                //For better movement on slopes/ramps
                move = Vector3.ProjectOnPlane(move, groundNormal);

                move.Normalize();
                move *= moveSpeed * Time.deltaTime;

                rb.AddForce(move, ForceMode.VelocityChange);
            }

            //Player rotation and animations
            if (move.x != 0f || move.z != 0f)
            {
                //Character rotation
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(transform.forward + new Vector3(move.x, 0f, move.z)), turnSpeed * Time.deltaTime);
            }
        }
    }

    private void Jump()
    {
        if (inputs.Jump && !jumping && grounded && !dashing && readyToJump)
        {
            readyToJump = false;
            rb.AddForce(Vector3.up * -rb.velocity.y, ForceMode.VelocityChange);//in case of slopes
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumping = true;
        }
    }

    private void ResetJump()
    {
        readyToJump = true;
    }

    private void Dive()
    {
        if (inputs.Dive && !diving && (grounded || jumping) && !dashing && readyToDive)
        {
            readyToDive = false;

            if (jumping)
            {
                jumping = false;
                ResetJump();
                rb.AddForce(Vector3.up * diveUpForce * 0.5f, ForceMode.Impulse);
            }
            else
            {
                rb.AddForce(Vector3.up * diveUpForce, ForceMode.Impulse);
            }
            //capsule collider's direction goes to the Z-Axis
            cp.direction = 2;

            //if falling while diving, reset velocity
            rb.velocity = Vector3.zero;

            rb.AddForce(transform.forward * diveForwardForce, ForceMode.Impulse);
            diving = true;
        }
    }

    private void ResetDive()
    {
        readyToDive = true;
    }

    private void Dash()
    {
        if (dashTriggered)
        {
            dashTriggered = false;
            rb.AddForce(colDir * dashforce, ForceMode.Impulse);
            dashing = true;

            Invoke(nameof(ResetDash), dashTime);
        }
    }

    private void ResetDash()
    {
        dashing = false;
    }
    #endregion

    #region GroundChecking / Snapping
    //TODO: MODIFIED (REMOVED RAGDOLLIN AND ANIMS)
    private void GroundChecking()
    {
        RaycastHit hit;
        physicsScene.Raycast(pelvis.position + (diving ? transform.forward * 0.67f : Vector3.zero), Vector3.down, out hit, -0.735f + (diving ? checkDistanceLayed : checkDistance), collisionMask);
        if (hit.collider)
        {
            grounded = true;
            groundNormal = hit.normal;

            if (jumping)
            {
                jumping = false;
                Invoke(nameof(ResetJump), jumpCooldown);
            }
            if (diving)
            {
                diving = false;
                CounterMovement();

                cp.direction = 1;
                getUp = true;
                Invoke(nameof(GetUp), getUpTime);
                Invoke(nameof(ResetDive), diveCooldown);
            }
        }
        else
        {
            grounded = false;
            groundNormal = Vector3.zero;
        }
    }

    #endregion
}