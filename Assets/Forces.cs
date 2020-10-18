using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Forces : MonoBehaviour
{
    public Rigidbody rb;
    public Animator anim;

    public float jumpForce;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            rb.AddForce(Vector3.forward * jumpForce, ForceMode.Impulse);
            anim.SetBool("isDiving", true);
        }
    }
}
