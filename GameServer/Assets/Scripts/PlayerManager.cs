using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    private CapsuleCollider cp;
    private Rigidbody rb;

    private ClientInputState currentState;

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

    [Header("DEBUG")]
    public bool debug;

    // Start is called before the first frame update
    public void StartController(bool controlable)
    {
        move = new Vector3();
        cp = GetComponent<CapsuleCollider>();
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    public void UpdateController()
    {
        if (!ragdolled && !getUp && isRunning)
        {
            if (!grounded)
                onAirTime += Time.deltaTime;
        }
    }

    public void ProcessInputs(ClientInputState _currentState)
    {
        currentState = _currentState;

        GroundChecking();

        //Extra gravity
        rb.AddForce(Vector3.down * gravityForce * Time.fixedDeltaTime);

        if (!ragdolled && !getUp)
        {
            Movement();
        }
    }

    #region Movement
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
            move = new Vector3(currentState.HorizontalAxis, 0f, currentState.VerticalAxis);

            if (move.sqrMagnitude > 0.1f)
            {
                //Camera direction
                //move = ToCameraSpace(move);----------------------------------------------------------------------------
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
            }
        }
    }

    private void Jump()
    {
        /*if (currentInput.jump && !jumping && grounded && !dashing && readyToJump)
        {
            //Debug.Log("Jump");
            readyToJump = false;
            rb.AddForce(Vector3.up * -rb.velocity.y, ForceMode.VelocityChange);//in case of slopes
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumping = true;
        }*/
    }

    private void ResetJump()
    {
        readyToJump = true;
    }

    private void Dive()
    {
        /*if (currentInput.dive && !diving && (grounded || jumping) && !dashing && readyToDive)
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
        }*/
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
    private void GroundChecking()
    {
        /*RaycastHit hit;
        Physics.Raycast(pelvis.position + (diving ? transform.forward * 0.67f : Vector3.zero), Vector3.down, out hit, -0.735f + (diving ? checkDistanceLayed : checkDistance), collisionMask);
        if (hit.collider)
        {
            grounded = true;
            groundNormal = hit.normal;

            if (onAirTime > 3f)
                ragdollController.RagdollIn();
            onAirTime = 0.0f;

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
        }*/
    }
    #endregion

    /*
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Bounce") && (!dashing || !dashTriggered))
        {
            dashTriggered = true;
            //Debug.Log("Dashing");
            colDir = (collision.contacts[0].point - transform.position).normalized * -1;
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

                ragdollController.RagdollIn();
                ragdollController.pelvis.AddForceAtPosition(-collisionDirection * ragdollController.impactForce, collision.contacts[0].point, ForceMode.Impulse);
            }
        }
    }
    */
}