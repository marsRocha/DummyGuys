using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Rigidbody rb;
    public CapsuleCollider cp;
    public Transform camera;
    public Animator anim;

    [Header("Movement settings")]
    public float moveSpeed = 9;
    public float jumpForce = 100;
    public float diveForce = 100;
    private Vector3 move;
    [Range(0,1)]
    public float turnSpeed;


    [Header("Collsion settings")]
    public bool isGrounded = false;
    public LayerMask collisionMask;
    public float checkDistance;
    private bool isJumping = false;
    private bool isFalling = false;
    private bool isDiving = false;

    // Start is called before the first frame update
    void Start()
    {
        move = new Vector3();   
    }

    // Update is called once per frame
    void Update()
    {
        CheckIsGrounded();

        if (!isDiving)
        {
            Movement();
            Jump();
        }
        Dive();
    }

    private void FixedUpdate()
    {
        if(!isDiving)
            rb.velocity = new Vector3(move.x, rb.velocity.y, move.z);
    }

    private void Dive()
    {
        if (Input.GetKeyDown(KeyCode.E) && isGrounded) //  && !isDiving
        {
            isDiving = true;

            rb.AddForce((transform.forward * diveForce + Vector3.up * jumpForce), ForceMode.Impulse);

            /*//If jumping while falling, reset y velocity.
            Vector3 vel = rb.velocity;
            if (rb.velocity.y < 0.5f)
                rb.velocity = new Vector3(vel.x, 0, vel.z);
            else if (rb.velocity.y > 0)
                rb.velocity = new Vector3(vel.x, vel.y / 2, vel.z);*/

            anim.SetBool("isDiving", true);
        }
        else if(isGrounded)
        {
            isDiving = false;
            anim.SetBool("isDiving", false);
        }
    }

    private void Jump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !isJumping && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isJumping = true;
            anim.SetBool("isJumping", true);
        }
        else if (isGrounded)
        {
            isJumping = false;
            anim.SetBool("isJumping", false);
        }
    }

    private void Movement()
    {
        move = new Vector3(Input.GetAxis("Horizontal"), 0.0f, Input.GetAxis("Vertical"));
        move = ToCameraSpace(move);
        move.Normalize();
        move *= moveSpeed * Time.deltaTime;

        //player's current velocity
        if (move.x != 0f || move.y != 0f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(move), turnSpeed * Time.deltaTime);
            anim.SetBool("isRunning", true);
        }
        else anim.SetBool("isRunning", false);
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

    private void CheckIsGrounded()
    {
        RaycastHit hit;
        Physics.Raycast(cp.bounds.center, Vector3.down, out hit, cp.bounds.extents.y + checkDistance, collisionMask);
        if(hit.collider)
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
            /*if (!isJumping)
            {
                isFalling = true;
                //anim.SetBool("isFalling", true);
            }*/
        }
    }
}
