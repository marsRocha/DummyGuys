using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class NPlayerController : MonoBehaviour
{
    [Header("Movement Variables")]
    public float gravityForce;
    public float moveSpeed, turnSpeed;
    public float jumpForce, jumpCooldown;
    public float diveForwardForce, diveUpForce, diveCooldown;
    public float dashforce, dashTime;
    private Vector3 move;

    [Header("Collision Variables")]
    public float checkDistance;
    public float checkDistanceLayed;
    public float getUpTime;
    public LayerMask collisionMask;
    private Vector3 colDir;
    private Vector3 groundNormal;

    //Inputs
    private float x, y;
    private bool jump, dive;

    //States
    public bool grounded, ragdolled, getUp;
    private bool jumping = false, diving = false, dashing = false;
    private bool readyToJump = true, readyToDive = true;
    private bool dashTriggered;

    [Header("Components")]
    public Transform camera;
    public Transform pelvis;
    private CapsuleCollider cp;
    private Rigidbody rb;
    private Animator anim;

    [Header("Particle Systems")]
    public ParticleSystem jumpPs;

    [Header("DEBUG")]
    public bool debug;

    // Start is called before the first frame update
    void Start()
    {
        move = new Vector3();
        cp = GetComponent<CapsuleCollider>();
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        GroundChecking();

        if (!ragdolled && !getUp)
        {
            MovementInput();
        }
    }

    private void FixedUpdate()
    {
        //Extra gravity
        rb.AddForce(Vector3.down * gravityForce * Time.fixedDeltaTime);

        if (!ragdolled && !getUp)
        {
            Movement();
        }
    }

    #region Movement
    private void MovementInput()
    {
        //Walk
        x = Input.GetAxis("Horizontal");
        y = Input.GetAxis("Vertical");
        //Jump
        jump = Input.GetKey(KeyCode.Space);
        dive = Input.GetKey(KeyCode.E);
    }

    private Vector3 ToCameraSpace(Vector3 moveVector)
    {
        Vector3 camFoward = camera.forward.normalized;
        Vector3 camRight = camera.right.normalized;

        camFoward.y = 0;
        camRight.y = 0;

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
        if(!diving && !dashing)
        {
            move = new Vector3(x, 0f, y);

            if(move.sqrMagnitude > 0.1f)
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
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation( transform.forward + new Vector3(move.x, 0f, move.z)), turnSpeed * Time.deltaTime);
                if (!jumping)
                    anim.SetBool("isRunning", true);
            }
            else anim.SetBool("isRunning", false);
        }
    }

    private void Jump()
    {
        if (jump && !jumping && grounded && !dashing && readyToJump)
        {
            Debug.Log("Jump");
            readyToJump = false;
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
        if(dive && !diving && (grounded || jumping) && !dashing && readyToDive)
        {
            readyToDive = false;
            Debug.Log("DIVE");
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
        Debug.Log("Dash has been reseted");
        dashing = false;
    }
    #endregion

    #region GroundChecking / Snapping
    private void GroundChecking()
    {
        RaycastHit hit;
        Physics.Raycast(pelvis.position, Vector3.down, out hit, -0.735f + (diving ? checkDistanceLayed : checkDistance), collisionMask);
        if (hit.collider)
        {
            grounded = true;
            groundNormal = hit.normal;

            if (jumping)
            {
                jumping = false;
                anim.SetBool("isJumping", false);
                Invoke(nameof(ResetJump), jumpCooldown);
            }
            else if (diving)
            {
                diving = false;
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

        if(debug)
            Debug.DrawRay(pelvis.position, Vector3.down  * (- 0.735f + (diving ? checkDistanceLayed : checkDistance)), Color.black);
    }

    #endregion

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Bounce") && (!dashing || !dashTriggered))
        {
            dashTriggered = true;
            Debug.Log("Dashing");
            colDir = (collision.contacts[0].point - transform.position).normalized * -1;
        }
    }
}
