using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPlayerController : MonoBehaviour
{
    //move variables
    public Vector3 move;
    public float gravityForce;
    public float moveSpeed;
    public float turnSpeed;

    //collision variables
    public float checkDistance;
    public LayerMask collisionMask;

    //States
    public bool grounded;
    public bool jumping;

    //Components
    public CapsuleCollider cp;
    public Rigidbody rb;
    public Animator anim;
    public Transform camera;

    // Start is called before the first frame update
    void Start()
    {
        move = new Vector3();
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        GroundChecking();
        Gravity();
        Movement();
    }

    private void FixedUpdate()
    {
        rb.velocity = new Vector3(move.x, rb.velocity.y, move.z);
    }

    #region Movement
    private void Movement()
    {
        move = new Vector3(Input.GetAxis("Horizontal"), 0.0f, Input.GetAxis("Vertical"));
        move = ToCameraSpace(move);
        move.Normalize();
        move *= moveSpeed * Time.deltaTime;


        //player's current velocity
        if (Input.GetAxis("Horizontal") != 0f || Input.GetAxis("Vertical") != 0f)
        {
            //Character rotation
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(move), turnSpeed * Time.deltaTime);
            if (!jumping)
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

    #endregion

    private void Gravity()
    {
        if (!grounded)
        {
            move.y += gravityForce * -1;
        }
    }

    #region GroundChecking / Snapping

    private void GroundChecking()
    {
        RaycastHit hit;
        Physics.Raycast(cp.bounds.center, Vector3.down, out hit, cp.bounds.extents.y + checkDistance, collisionMask);
        if (hit.collider)
        {
            grounded = true;
            //GroundConfirm(tempHit);

        }
        else
            grounded = false;

    }

    #endregion

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Bounce"))
        {
            Debug.Log("collision");
            Vector3 collisionDirection = collision.contacts[0].point - transform.position;
            // We then get the opposite (-Vector3) and normalize it
            collisionDirection = -collisionDirection.normalized;

            rb.AddForce(collisionDirection * 15, ForceMode.Impulse);
        }
    }
}
