using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Components
    private Rigidbody rb;
    public Transform pelvis;
    private CapsuleCollider cp;
    private Animator animator;
    private LogicTimer logicTimer;
    private RagdollController ragdollController;
    private PlayerAudioManager playerAudio;

    public int currentAnimation { get; private set; } = 0;
    private int lastAnimation = 0;

    [Header("Movement Variables")]
    private float gravityForce = 15f;
    private float moveSpeed = 250f, turnSpeed = 10f;
    private float jumpForce = 10.6f, jumpCooldown = 0.1f;
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
    private Vector3 groundNormal;
    private Vector3 collisionForce, collisionPoint;

    [Header("Particle Systems")]
    [SerializeField]
    private ParticleSystem jumpPs;
    [SerializeField]
    private ParticleSystem bumpPs;
    [SerializeField]
    private ParticleSystem respawnPs;
    [SerializeField]
    private ParticleSystem checkpointPs;

    // Collision
    private Vector3 bounceDir;
    private Vector3 bouncePos;

    // Grab varuables
    public GameObject grabbedObj { get; private set; }
    public LayerMask interactionMask;

    // Pushed variables
    private bool push;
    private Vector3 push_direction;
    public Guid pushedObj { get; private set; }
    private float pushCooldown = 3.5f, pushTime;

    // States
    private bool grounded;
    private bool ragdolled, getUp;
    private bool jumping;
    private bool diving;
    private bool dashing;
    private bool readyToJump = true, readyToDive = true;
    private bool dashTriggered;
    private bool grabbed;
    public bool grabbing { get; private set; }

    public void StartController(LogicTimer _logicTimer)
    {
        logicTimer = _logicTimer;

        move = new Vector3();
        cp = GetComponent<CapsuleCollider>();
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = Vector3.up * 0.9009846f;
        animator = transform.GetChild(0).GetComponent<Animator>();
        ragdollController = GetComponent<RagdollController>();
        playerAudio = GetComponent<PlayerAudioManager>();

        collisionForce = Vector3.zero;
        collisionPoint = Vector3.zero;
    }

    // Used in fixed update
    public void UpdateController(ClientInputState _currentInputs)
    {
        currentInputs = _currentInputs;

        // Check if there as been an impact last frame
        if (collisionForce != Vector3.zero && collisionPoint != Vector3.zero)
        {
            //rb.AddExplosionForce(1000, collisionPoint, 10);

            rb.isKinematic = true;
            rb.isKinematic = false;

            rb.AddForceAtPosition(collisionForce, collisionPoint, ForceMode.Impulse);
            // Reset values
            collisionForce = Vector3.zero; collisionPoint = Vector3.zero;
            // Play hit audio
            playerAudio.PlayImpact(2);
        }

        if (push)
        {
            EnterRagdoll(transform.position);

            rb.AddForceAtPosition( push_direction * 60f, transform.position + Vector3.up, ForceMode.Impulse);

            playerAudio.PlayImpact(2);
            push = false;
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
        Physics.Raycast(pelvis.position + (diving ? transform.forward * 0.67f : Vector3.zero), Vector3.down, out hit, -0.735f + (diving ? checkDistanceLayed : checkDistance), collisionMask);
        if (hit.collider)
        {
            grounded = true;
            groundNormal = hit.normal;

            if (jumping)
            {
                jumping = false;
                animator.SetBool("isJumping", false);
                jumpTime = Time.time + jumpCooldown;
                readyToJump = false;
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

        AnimAudio();
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
        if (!diving && !dashing && !grabbing)
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
        if (!readyToJump && jumpTime < Time.time)
            readyToJump = true;

        if (currentInputs.Jump && !jumping && grounded && !dashing && readyToJump)
        {
            readyToJump = false;
            groundNormal = Vector3.zero;
            rb.AddForce(Vector3.up * -rb.velocity.y, ForceMode.VelocityChange);//in case of slopes
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
            jumping = true;
            animator.SetBool("isJumping", true);
            jumpPs.Play();
        }
    }

    private void Dive()
    {
        if (!readyToDive && diveTime < Time.time)
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
            rb.AddForce(collisionForce * dashforce, ForceMode.Impulse);
            dashing = true;

            Invoke(nameof(ResetDash), dashTime);
        }
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

    public void Bump(Vector3 _point)
    {
        bumpPs.transform.position = _point;
        bumpPs.Play();
    }

    // Determine current animation and sounds to play
    private void AnimAudio()
    {
        if (move.magnitude == 0)
        {
            currentAnimation = 0;
        }
        else
        {
            currentAnimation = 1;
        }
        if (jumping)
        {
            currentAnimation = 2;
            if(lastAnimation != currentAnimation)
                playerAudio.PlayMovement(0);
        }
        if (diving)
        {
            currentAnimation = 3;
            if (lastAnimation != currentAnimation)
                playerAudio.PlayMovement(1);
        }

        lastAnimation = currentAnimation;
    }
    #endregion

    #region Actions

    public bool TryGrab()
    {
        if (ragdolled || diving || jumping)
            return false;

        if (!grabbing)
            animator.SetBool("isGrabbing", true);

        RaycastHit hit;
        Physics.Raycast(pelvis.position, transform.forward, out hit, 1, interactionMask);
        if (hit.collider)
        {
            grabbing = true;
            grabbedObj = hit.collider.transform.root.gameObject;
            return true;
        }

        return false;
    }

    public void StopGrab()
    {
        animator.SetBool("isGrabbing", false);
        grabbing = false;
        grabbedObj = null;
    }

    // Action confirmed by the server
    public void Grab(bool _isgrabbing)
    {
        grabbing = _isgrabbing;
        animator.SetBool("isGrabbing", _isgrabbing);
    }

    public void Grabbed(bool _isbeingGrabbed)
    {
        grabbed = _isbeingGrabbed;
    }

    public bool TryPush()
    {
        if (ragdolled || diving || jumping || Time.time < pushTime)
            return false;

        animator.SetTrigger("Push");

        RaycastHit hit;
        Physics.Raycast(pelvis.position, transform.forward, out hit, 2, interactionMask);
        if (hit.collider)
        {
            pushTime = Time.time + pushCooldown;
            pushedObj = hit.collider.transform.root.GetComponent<RemotePlayerManager>().Id;
            return true;
        }
        return false;
    }

    public void Push()
    {
        animator.SetBool("isGrabbing", true);
        animator.SetBool("isGrabbing", false);
    }

    public void Pushed(Vector3 _direction)
    {
        push = true;
        push_direction = _direction;
    }
    #endregion

    private void ResetBehaviours()
    {
        animator.SetBool("isRunning", false);
        jumping = false;
        animator.SetBool("isJumping", false);
        diving = false;
        animator.SetBool("isDiving", false);
        currentAnimation = 0;
    }

    public void SetCurrentionAnimation(int animationNum)
    {
        currentAnimation = animationNum;
    }

    #region Ragdoll
    public void EnterRagdoll(Vector3 _point)
    {
        ragdolled = true;
        ResetBehaviours();

        GetComponent<PlayerManager>().Ragdolled = true;
        ragdollController.RagdollIn();

        // Player collision effect
        Bump(_point);
    }

    public void ExitRagdoll()
    {
        ragdolled = false;
        cp.direction = 1;
        ragdollController.RagdollOut();
        GetComponent<PlayerManager>().Ragdolled = false;
    }
    #endregion

    private void OnCollisionEnter(Collision collision)
    {
        Vector3 collisionDirection = -(collision.contacts[0].point - transform.position).normalized;

        // Determine what has been and operate accordingly
        if (collision.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
        {
            // Add obstacle extra force
            collisionForce = collisionDirection * ragdollController.ObstacleModifier;
            collisionPoint = collision.contacts[0].point;

            EnterRagdoll(collisionPoint);
        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Bounce"))
        {
            // Add bounce extra force
            collisionForce = collisionDirection * ragdollController.BounceModifier;
            collisionForce.y = Mathf.Clamp(collisionForce.y, 0, 15);
            collisionPoint = collision.contacts[0].point;

            bounceDir = collisionForce;
            bouncePos = collisionPoint;

            // Play jump effect
            Bump(collisionPoint);
        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Floor"))
        {
            if (collision.impulse.magnitude >= 20)
            {
                EnterRagdoll(collision.contacts[0].point);
            }
        }
        else if (collision.impulse.magnitude >= ragdollController.MinForce || (jumping || diving))
        {
            // Add extra force
            collisionForce = collisionDirection * ragdollController.Modifier;
            collisionPoint = collision.contacts[0].point;

            EnterRagdoll(collision.contacts[0].point);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.black;

        Gizmos.DrawLine(pelvis.position, pelvis.position + (transform.forward.normalized * 2));

    }

    #region Behavior EFX
    public void Die()
    {
        playerAudio.PlayEffect(5);
    }

    public void Respawn()
    {
        currentAnimation = 0;
        respawnPs.Play();
        playerAudio.PlayEffect(6);
    }

    public void Checkpoint()
    {
        checkpointPs.Play();
        playerAudio.PlayEffect(4);
    }
    #endregion
}
