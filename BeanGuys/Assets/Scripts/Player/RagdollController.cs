using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RagdollController : MonoBehaviour
{
    Rigidbody[] ragdollRbs;
    Collider[] ragdollCols;
    [SerializeField]
    Transform[] ragdollRs;

    public CapsuleCollider cp;
    public Animator animator;
    public PlayerController playerController;

    [Header("Settings")]
    [SerializeField]
    private float sensitiveForce;
    [SerializeField]
    private float ragdollTime;
    [SerializeField]
    private float lerpSpeed;
    [SerializeField]
    private float transitionStartTime = 2f;
    [SerializeField]
    private float transitionDuration = 4f;

    private bool ragdollActive = false;
    public Transform ragdollCenter;


    // Start is called before the first frame update
    void Start()
    {
        ragdollRbs = this.gameObject.GetComponentsInChildren<Rigidbody>();
        ragdollCols = this.gameObject.GetComponentsInChildren<Collider>();
        DeactivateRagdoll();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.Z))
        {
            ActivateRagdoll();
        }
        if (Input.GetKey(KeyCode.X))
        {
            DeactivateRagdoll();
        }
    }

    public void ActivateRagdoll()
    {
        ragdollActive = true;
        cp.isTrigger = true;
        this.GetComponent<Rigidbody>().isKinematic = true;

        playerController.enabled = false;
        animator.enabled = false;

        for (int i = 1; i < ragdollRbs.Length; i++)
            ragdollRbs[i].isKinematic = false;

        for (int i = 1; i < ragdollCols.Length; i++)
            ragdollCols[i].enabled = true;

        StartCoroutine(DeactivateRagdoll(ragdollTime));
    }

    private void DeactivateRagdoll()
    {
        ragdollActive = false;

        cp.isTrigger = false;
        this.GetComponent<Rigidbody>().isKinematic = false;

        for (int i = 1; i < ragdollRbs.Length; i++)
            ragdollRbs[i].isKinematic = true;
        for (int i = 1; i < ragdollCols.Length; i++)
            ragdollCols[i].enabled = false;

        animator.enabled = true;
        playerController.enabled = true;
    }

    private IEnumerator DeactivateRagdoll(float time)
    {
        yield return new WaitForSecondsRealtime(time);

        //not working
        /*foreach( Rigidbody r in ragdollRbs)
        {
            foreach (Transform t in ragdollRs)
            {
                Quaternion animationRotation = r.transform.localRotation;
                float lerp = 1 - Mathf.Clamp01((Time.time - transitionStartTime) / transitionDuration);
                r.transform.localRotation = Quaternion.Lerp(animationRotation, r.rotation, lerp);
            }
        }*/
        DeactivateRagdoll();
    }

    private void OnCollisionEnter(Collision collision)
    {
        /*Debug.Log("collision: " + collision.relativeVelocity.magnitude);
        if (collision.relativeVelocity.magnitude > sensitiveForce && transform.InverseTransformPoint(collision.contacts[0].point).y > 0.164f)
        {
            ActivateRagdoll();
        }
        else if (collision.gameObject.layer == LayerMask.NameToLayer("RotatingCylinder"))
        {
            //playerController.AddExplodingForce();
        }*/
    }
}
