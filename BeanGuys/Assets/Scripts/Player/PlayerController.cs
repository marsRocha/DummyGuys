using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Rigidbody rb;
    public CapsuleCollider cp;
    public Transform camera;
    public Animator anim;

    [Header("Movement settings")]
    public float moveSpeed = 9;
    public float jumpForce = 6;
    public float diveUpForce = 4;
    public float diveForce = 4;
    private Vector3 move;
    [Range(0,1)]
    public float turnSpeed;
    public float extraGravity;


    [Header("Ground settings")]
    public bool isGrounded = false;
    public LayerMask collisionMask;
    public float checkDistance;
    public  bool isJumping = false;
    //private bool isFalling = false;
    public bool isDiving = false;

    [Header("Collision Settings")]
    private bool bounce = false;
    public float bounceForce = 10;
    private Vector3 collisionDirection;

    [Header("Particle Systems")]
    public ParticleSystem jumpPS;
    public ParticleSystem landingPS;

    //variable controled by GameManager
    //[HideInInspector]
    public bool isRunning = false;


    // Start is called before the first frame update
    void Start()
    {
        move = new Vector3();   
    }

    // Update is called once per frame
    void Update()
    {
        if (isRunning)
        {
            if (!isDiving)
            {
                Movement();
                Jump();
            }
            Dive();
        }
    }

    private void FixedUpdate()
    {
        if (isRunning)
        {
            if (!bounce)
            {
                CheckIsGrounded();

                if (!isDiving)
                    rb.velocity = new Vector3(move.x, rb.velocity.y, move.z);

                ExtraDownForce();
            }
            else
            {
                rb.AddForce(collisionDirection * bounceForce, ForceMode.Impulse);
                bounce = false;
                Debug.Log("Bounced");
            }
        }
    }

    private void ExtraDownForce()
    {
        rb.AddForce(Vector3.down * extraGravity);
    }

    private void Dive()
    {
        if (Input.GetKeyDown(KeyCode.E) && !isDiving && (isGrounded || isJumping)) 
        {
            if (isJumping)
            {
                isJumping = false;
                anim.SetBool("isJumping", false);
            }
            //capsule collider's direction goes to the Z-Axis
            cp.direction = 2;
            isDiving = true;

            rb.velocity = Vector3.zero;

            rb.AddForce((transform.forward * diveForce + Vector3.up * diveUpForce), ForceMode.VelocityChange);


            anim.SetBool("isDiving", true);
        }
    }

    private void Jump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !isJumping && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isJumping = true;
            anim.SetBool("isJumping", true);
            //anim.SetBool("isRunning", false);
            jumpPS.Play();
        }
    }

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
            if (!isJumping)
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
            if (!isGrounded)
            {
                isGrounded = true;
                if (isJumping)
                {
                    isJumping = false;
                    anim.SetBool("isJumping", false);
                }
                else if (isDiving)
                {
                    isDiving = false;
                    anim.SetBool("isDiving", false);
                    cp.direction = 1;
                }
            }
        }
        else
        {
            isGrounded = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Bounce"))
        {
            Debug.Log("collision");
            Vector3 collisionDirection = collision.contacts[0].point - transform.position;
            // We then get the opposite (-Vector3) and normalize it
            collisionDirection = -collisionDirection.normalized;

            rb.AddForce(collisionDirection * 150, ForceMode.Impulse);
        }
    }
}
