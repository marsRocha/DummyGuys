using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPlayerController : MonoBehaviour
{
    Rigidbody rb;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        
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
