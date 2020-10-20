using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RagdollTest : MonoBehaviour
{
    public float sensitiveForce;
    public RagdollController ragdollController;
    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("collision: " + collision.relativeVelocity.magnitude);
        if (collision.relativeVelocity.magnitude > sensitiveForce)
        {
            ragdollController.ActivateRagdoll();
        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("RotatingCylinder"));
    }
}
