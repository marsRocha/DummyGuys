using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Components
    private Rigidbody rb;
    public Transform pelvis;
    private CapsuleCollider cp;
    private LogicTimer logicTimer;
    private PhysicsScene physicsScene;
    private RagdollController ragdollController;

    [Header("Movement Variables")]
    private float gravityForce = 15f;
    private float moveSpeed = 300f, turnSpeed = 10f;
    private float jumpForce = 12f, jumpCooldown = 0.1f;
    private float diveForwardForce = 7f, diveUpForce = 7f, diveCooldown = 0.5f;
    private float dashforce = 10f, dashTime = 0.5f;
    private Vector3 move;
    private float jumpTime, diveTime;
    private ClientInputState currentInputs;

    [Header("Collision Variables")]
    private float checkDistance = 1.52f;
    private float checkDistanceLayed = 1.07f;
    private float getUpDelay = 0.4f, getUpTime;
    public LayerMask collisionMask;
    private Vector3 colDir;
    private Vector3 groundNormal;
    private Vector3 collisionForce, collisionPoint;

    // States
    private bool grounded;
    private bool ragdolled, getUp;
    private bool jumping;
    private bool diving;
    private bool dashing;
    private bool readyToJump = true, readyToDive = true;
    private bool dashTriggered;

    public void StartController(LogicTimer _logicTimer)
    {
        logicTimer = _logicTimer;

        move = new Vector3();
        cp = GetComponent<CapsuleCollider>();
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = Vector3.up * 0.9009846f;
        physicsScene = gameObject.scene.GetPhysicsScene();
        ragdollController = GetComponent<RagdollController>();

        collisionForce = Vector3.zero;
        collisionPoint = Vector3.zero;
    }

    // Used in fixed update
    public void UpdateController(ClientInputState currentInput)
    {
        this.currentInputs = currentInput;

        // Check if there as been an impact last frame
        if (collisionForce != Vector3.zero && collisionPoint != Vector3.zero)
        {
            rb.AddForceAtPosition(collisionForce, collisionPoint, ForceMode.Impulse);

            collisionForce = Vector3.zero; collisionPoint = Vector3.zero;
        }

        GroundChecking();

        // Extra gravity
        rb.AddForce(Vector3.down * gravityForce * logicTimer.FixedDeltaTime);

        // Wait for player to getUp
        if (getUp && getUpTime < Time.time + getUpDelay)
            getUp = false;

        if (!ragdolled && !getUp)
        {
            Movement();
        }
    }

    #region Movement
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
                jumpTime = Time.time + jumpCooldown;
                readyToJump = false;
            }
            if (diving)
            {
                diving = false;
                CounterMovement();

                cp.direction = 1;
                getUp = true;
                getUpTime = Time.time + getUpDelay;
                diveTime = Time.time + diveCooldown;
                readyToDive = false;
            }
        }
        else
        {
            grounded = false;
            groundNormal = Vector3.zero;
        }
    }

    private void Movement()
    {
        Dash();
        Jump();
        Dive();
        Walk();

        CounterMovement();
    }

    private Vector3 ToCameraSpace(Vector3 moveVector)
    {
        Vector3 camFoward = (currentInputs.LookingRotation * Vector3.forward);
        Vector3 camRight = (currentInputs.LookingRotation * Vector3.right);

        camFoward.y = 0;
        camRight.y = 0;

        camFoward.Normalize();
        camRight.Normalize();

        Vector3 moveDirection = (camFoward * moveVector.z + camRight * moveVector.x);

        return moveDirection;
    }

    // MOVEMENT TYPES
    private void Walk()
    {
        if (!diving && !dashing)
        {
            move = new Vector3(currentInputs.HorizontalAxis, 0f, currentInputs.VerticalAxis);

            if (move.sqrMagnitude > 0.1f)
            {
                //Camera direction
                move = ToCameraSpace(move);

                //For better movement on slopes/ramps
                move = Vector3.ProjectOnPlane(move, groundNormal);

                move.Normalize();
                move *= moveSpeed * logicTimer.FixedDeltaTime;

                rb.AddForce(move, ForceMode.VelocityChange);
            }

            //Player rotation and animations
            if (move.x != 0f || move.z != 0f)
            {
                //Character rotation
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(transform.forward + new Vector3(move.x, 0f, move.z)), turnSpeed * logicTimer.FixedDeltaTime);
            }
        }
    }

    private void Jump()
    {
        if (!readyToJump && jumpTime < Time.time)
            readyToJump = true;

        if (currentInputs.Jump && !jumping && grounded && !dashing && readyToJump)
        {
            readyToJump = false;
            rb.AddForce(Vector3.up * -rb.velocity.y, ForceMode.VelocityChange); // In case of slopes
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumping = true;
        }
    }

    private void Dive()
    {
        if (!readyToDive && diveTime < Time.time)
            readyToDive = true;

        if (currentInputs.Dive && !diving && (grounded || jumping) && !dashing && readyToDive)
        {
            readyToDive = false;
            if (jumping)
            {
                jumping = false;
                readyToJump = true; // Reset jump
                rb.AddForce(Vector3.up * diveUpForce * 0.5f, ForceMode.Impulse);
            }
            else
            {
                rb.AddForce(Vector3.up * diveUpForce, ForceMode.Impulse);
            }
            // Capsule collider's direction goes to the Z-Axis
            cp.direction = 2;

            // If falling while diving, reset velocity
            rb.velocity = Vector3.zero;

            rb.AddForce(transform.forward * diveForwardForce, ForceMode.Impulse);
            diving = true;
        }
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

    public void ActivateDash(Vector3 _direction)
    {
        dashTriggered = true;
        colDir = _direction;
    }

    private void ResetDash()
    {
        dashing = false;
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
    #endregion

    private void ResetBehaviours()
    {
        jumping = false;
        diving = false;
    }

    public void EnterRagdoll()
    {
        ragdolled = true;
        ResetBehaviours();

        GetComponent<Player>().Ragdolled = true;
        ragdollController.RagdollIn();
    }

    public void ExitRagdoll()
    {
        ragdolled = false;
        cp.direction = 1;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (ragdollController.State == RagdollState.Animated)
        {
            Vector3 collisionDirection = collision.contacts[0].normal;

            // Determine what has been and operate accordingly
            if (collision.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
            {
                // Add obstacle extra force
                collisionForce = collisionDirection * ragdollController.ObstacleModifier;
                collisionPoint = collision.contacts[0].point;

                EnterRagdoll();
            }
            else if (collision.gameObject.layer == LayerMask.NameToLayer("Bounce"))
            {
                // Add bounce extra force
                collisionForce = collisionDirection * ragdollController.BounceModifier;
                collisionPoint = collision.contacts[0].point;
            }
            else if (collision.gameObject.layer == LayerMask.NameToLayer("Floor"))
            {
                if (collision.impulse.magnitude >= 20)
                {
                    EnterRagdoll();
                }
            }
            else if (collision.impulse.magnitude >= ragdollController.MinForce || (jumping || diving))
            {
                // Add extra force
                collisionForce = collisionDirection * ragdollController.Modifier;
                collisionPoint = collision.contacts[0].point;

                EnterRagdoll();
            }
        }
    }
}
