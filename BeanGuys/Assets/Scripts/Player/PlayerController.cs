using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Components")]
    public Transform Pelvis;
    private CapsuleCollider cp;
    private Rigidbody rb;
    private Animator animator;
    private LogicTimer logicTimer;

    [Header("Movement Variables")]
    [SerializeField]
    private float gravityForce = 15f;
    [SerializeField]
    private float moveSpeed = 300f, turnSpeed = 10f;
    [SerializeField]
    private float jumpForce = 12f, jumpCooldown = 0.25f;
    [SerializeField]
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

    [Header("Particle Systems")]
    public ParticleSystem jumpPs;
    public ParticleSystem bumpPs;
    public ParticleSystem respawnPs;

    [Header("States")]
    public bool grounded;
    public bool ragdolled, getUp;
    public bool isRunning = false;
    public bool jumping { get; private set; }
    public bool diving { get; private set; }
    public bool dashing { get; private set; }
    private bool readyToJump = true, readyToDive = true;
    public bool dashTriggered { get; private set; }

    public void StartController(LogicTimer _logicTimer)
    {
        logicTimer = _logicTimer;

        move = new Vector3();
        cp = GetComponent<CapsuleCollider>();
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = Vector3.up * 0.9009846f;
        animator = transform.GetChild(0).GetComponent<Animator>();
    }

    //Used in fixed update
    public void UpdateController(ClientInputState currentInput)
    {
        this.currentInputs = currentInput;

        GroundChecking();

        //Extra gravity
        rb.AddForce(Vector3.down * gravityForce * logicTimer.FixedDeltaTime);

        //Wait for player to getUp
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
        Physics.Raycast(Pelvis.position + (diving ? transform.forward * 0.67f : Vector3.zero), Vector3.down, out hit, -0.735f + (diving ? checkDistanceLayed : checkDistance), collisionMask);
        if (hit.collider)
        {
            grounded = true;
            groundNormal = hit.normal;

            if (jumping)
            {
                jumping = false;
                animator.SetBool("isJumping", false);
                jumpTime = Time.time + jumpCooldown;
            }
            if (diving)
            {
                diving = false;
                animator.SetBool("isDiving", false);
                CounterMovement();

                cp.direction = 1;
                getUp = true;
                getUpTime = Time.time + getUpDelay;
                diveTime = Time.time + diveCooldown;
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

    //MOVEMENT TYPES
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
                if (!jumping)
                {
                    animator.SetBool("isRunning", true);
                }
            }
            else if ((move.x == 0f || move.z == 0f) && (currentInputs.HorizontalAxis == 0 && currentInputs.VerticalAxis == 0))
            {
                animator.SetBool("isRunning", false);
            }
        }
    }

    private void Jump()
    {
        if (!readyToJump && jumpTime < Time.time + jumpCooldown)
            readyToJump = true;

        if (currentInputs.Jump && !jumping && grounded && !dashing && readyToJump)
        {
            readyToJump = false;
            rb.AddForce(Vector3.up * -rb.velocity.y, ForceMode.VelocityChange);//in case of slopes
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumping = true;
            animator.SetBool("isJumping", true);
            jumpPs.Play();
        }
    }

    private void Dive()
    {
        if (!readyToDive && diveTime < Time.time + diveCooldown)
            readyToDive = true;

        if (currentInputs.Dive && !diving && (grounded || jumping) && !dashing && readyToDive)
        {
            readyToDive = false;
            animator.SetBool("isRunning", false);
            if (jumping)
            {
                jumping = false;
                animator.SetBool("isJumping", false);
                readyToJump = true; //Reset jump
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
            animator.SetBool("isDiving", true);
            jumpPs.Play();
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

    public void Respawn()
    {
        respawnPs.Play();
    }

    public void Bump(Vector3 _point)
    {
        bumpPs.transform.position = _point;
        bumpPs.Play();
    }
    #endregion

    private void ResetBehaviours()
    {
        animator.SetBool("isRunning", false);
        jumping = false;
        animator.SetBool("isJumping", false);
        diving = false;
        animator.SetBool("isDiving", false);
    }

    public void EnterRagdoll(Vector3 _point)
    {
        bumpPs.transform.position = _point;
        bumpPs.Play();

        ragdolled = true;
        ResetBehaviours();
    }
    
    public void ExitRagdoll()
    {
        ragdolled = false;
        cp.direction = 1;
    }
}
