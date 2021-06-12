using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerController : MonoBehaviour
{
    [Header("Components")]
    public Transform pelvis;
    private CapsuleCollider cp;
    private Rigidbody rb;
    private Animator anim;
    private RagdollController ragdollController;

    [Header("Movement Variables")]
    public float gravityForce;
    public float moveSpeed, turnSpeed;
    public float jumpForce, jumpCooldown;
    public float diveForwardForce, diveUpForce, diveCooldown;
    public float dashforce, dashTime;
    private Vector3 move;
    private ClientInputState currentInputs;

    [Header("Collision Variables")]
    public float checkDistance;
    public float checkDistanceLayed;
    public float getUpTime;
    public LayerMask collisionMask;
    private Vector3 colDir;
    private Vector3 groundNormal;
    private float onAirTime;

    [Header("States")]
    public bool grounded;
    public bool ragdolled, getUp;
    //[HideInInspector]
    public bool isRunning = false;
    //[HideInInspector]
    public bool jumping = false, diving = false, dashing = false;
    private bool readyToJump = true, readyToDive = true;
    private bool dashTriggered;
    public animNum currentAnim;

    [Header("Particle Systems")]
    public ParticleSystem jumpPs;
    public ParticleSystem bumpPs;
    public ParticleSystem respawnPs;

    [Header("DEBUG")]
    public bool debug;

    // Start is called before the first frame update
    public void StartController()
    {
        move = new Vector3();
        cp = GetComponent<CapsuleCollider>();
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        ragdollController = GetComponent<RagdollController>();
        currentAnim = animNum.idle;
    }

    // Update is called once per frame
    public void UpdateController()
    {
        if (!ragdolled && !getUp && !grounded)
        {
            onAirTime += Time.deltaTime;
        }
    }

    public void FixedUpdateController(ClientInputState currentInput)
    {
        this.currentInputs = currentInput;

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
        Vector3 camFoward = (currentInputs.LookingRotation * Vector3.forward);
        Vector3 camRight = (currentInputs.LookingRotation * Vector3.right);

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

        //Check animation
        if (move.magnitude != 0)
            currentAnim = animNum.run;
        else
            currentAnim = animNum.idle;
        if (jumping)
            currentAnim = animNum.jump;
        if (diving)
            currentAnim = animNum.dive;
        //on GroudChecking in diving currentAnim is set to idle, 
        //this is because invoke makes it so we have to wait that the player is up to know that we are not diving
        //look for a better solution?
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
            move = new Vector3(currentInputs.HorizontalAxis, 0f, currentInputs.VerticalAxis);

            if (move.sqrMagnitude > 0.1f)
            {
                //Camera direction
                move = ToCameraSpace(move);
                Debug.DrawLine(this.transform.position, this.transform.position + move, Color.red, 1f);

                //For better movement on slopes/ramps
                move = Vector3.ProjectOnPlane(move, groundNormal);

                move.Normalize();
                move *= moveSpeed * Time.deltaTime;
                Debug.DrawLine(this.transform.position, this.transform.position + move, Color.yellow, 1f);

                rb.AddForce(move, ForceMode.VelocityChange);
            }

            //Player rotation and animations
            if (move.x != 0f || move.z != 0f)
            {
                //Character rotation
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(transform.forward + new Vector3(move.x, 0f, move.z)), turnSpeed * Time.deltaTime);
                if (!jumping)
                {
                    anim.SetBool("isRunning", true);
                }
            }
            else if ((move.x == 0f || move.z == 0f) && (currentInputs.HorizontalAxis == 0 && currentInputs.VerticalAxis == 0))
            {
                anim.SetBool("isRunning", false);
            }
        }
    }

    private void Jump()
    {
        if (currentInputs.Jump && !jumping && grounded && !dashing && readyToJump)
        {
            //Debug.Log("Jump");
            readyToJump = false;
            rb.AddForce(Vector3.up * -rb.velocity.y, ForceMode.VelocityChange);//in case of slopes
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumping = true;
            anim.SetBool("isJumping", true);
            jumpPs.Play();
        }
    }

    private void ResetJump()
    {
        readyToJump = true;
    }

    private void Dive()
    {
        if (currentInputs.Dive && !diving && (grounded || jumping) && !dashing && readyToDive)
        {
            readyToDive = false;
            //Debug.Log("DIVE");
            anim.SetBool("isRunning", false);
            if (jumping)
            {
                jumping = false;
                ResetJump();
                anim.SetBool("isJumping", false);
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
            anim.SetBool("isDiving", true);
            jumpPs.Play();
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
        //Debug.Log("Dash has been reseted");
        dashing = false;
    }
    #endregion

    #region GroundChecking / Snapping
    private void GroundChecking()
    {
        RaycastHit hit;
        Physics.Raycast(pelvis.position + (diving ? transform.forward * 0.67f : Vector3.zero), Vector3.down, out hit, -0.735f + (diving ? checkDistanceLayed : checkDistance), collisionMask);
        if (hit.collider)
        {
            grounded = true;
            groundNormal = hit.normal;

            /*if (onAirTime > 3f)
                ragdollController.RagdollIn();*/
            onAirTime = 0.0f;

            if (jumping)
            {
                jumping = false;
                anim.SetBool("isJumping", false);
                Invoke(nameof(ResetJump), jumpCooldown);
            }
            if (diving)
            {
                diving = false;
                currentAnim = animNum.idle;
                CounterMovement();
                anim.SetBool("isDiving", false);

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

        if (debug)
            Debug.DrawRay(pelvis.position + (diving ? transform.forward * 0.67f : Vector3.zero), Vector3.down * (-0.735f + (diving ? checkDistanceLayed : checkDistance)), Color.black);
    }

    #endregion

    public void Respawn()
    {
        respawnPs.Play();
    }

    /*
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Bounce") && (!dashing || !dashTriggered))
        {
            dashTriggered = true;
            //Debug.Log("Dashing");
            colDir = (collision.contacts[0].point - transform.position).normalized * -1;

            bumpPs.transform.position = collision.contacts[0].point;
            bumpPs.Play();
        }
        // nao entrar em ragdoll quando bate num "Bounce" obj e quando salta para cima dum cushion
        if ((collision.gameObject.layer != LayerMask.NameToLayer("Floor") && collision.gameObject.layer != LayerMask.NameToLayer("Wall")
            && collision.gameObject.layer != LayerMask.NameToLayer("Bounce")) && ragdollController.state == RagdollState.Animated)
        {
            Vector3 collisionDirection = collision.contacts[0].normal;

            //Debug.Log("collision force:" + collision.impulse.magnitude);
            //Debug.Log("collision relative Velocity:" + collision.relativeVelocity.magnitude);
            if (collision.impulse.magnitude >= ragdollController.maxForce || (jumping || diving))
            {
                bumpPs.transform.position = collision.contacts[0].point;
                bumpPs.Play();

                ragdollController.RagdollIn();
                ragdollController.pelvis.AddForceAtPosition(-collisionDirection * ragdollController.impactForce, collision.contacts[0].point, ForceMode.Impulse);
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Obstacle") && ragdollController.state == RagdollState.Animated)
        {

            if (collision.impulse.magnitude >= ragdollController.maxForce || (jumping || diving))
            {
                Vector3 collisionDirection = collision.contacts[0].normal;

                bumpPs.transform.position = collision.contacts[0].point;
                bumpPs.Play();

                ragdollController.RagdollIn();
                ragdollController.pelvis.AddForceAtPosition(-collisionDirection * ragdollController.impactForce, collision.contacts[0].point, ForceMode.Impulse);
            }
        }
    }*/
}

public enum animNum { idle, run, jump, dive };
