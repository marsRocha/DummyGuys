using System.Collections.Generic;
using UnityEngine;

public class RagdollController : MonoBehaviour
{
    [Header("Components")]
    private PlayerController playerController;
    private Animator animator;
    private CapsuleCollider cp;
    public Rigidbody pelvis;

    [Header("Ragdoll components")]
    private Rigidbody[] ragdollRbs;
    private Collider[] ragdollCols;
    private List<BodyPart> bodyParts;

    [Header("Ragdoll Settings")]
    [SerializeField]
    public RagdollState state;
    public LayerMask collisionMask;
    private float canRecover;
    public float recoverTime;
    public float maxForce;
    public float impactForce;
    private Vector3 hitPoint;

    //Time transitioning from ragdolled to animated
    public float ragdollToMecanimBlendTime = 0.5f;
    public float mecanimToGetUpTransitionTime = 0.05f;

    //A helper variable to store the time when we transitioned from ragdolled to blendToAnim state
    float ragdollingEndTime = -100;

    //Additional vectores for storing the pose the ragdoll ended up in.
    Vector3 ragdolledHipPosition, ragdolledHeadPosition, ragdolledFeetPosition;


    void Start()
    {
        playerController = GetComponent<PlayerController>();
        animator = transform.GetChild(0).GetComponent<Animator>();
        cp = GetComponent<CapsuleCollider>();

        ragdollRbs = transform.GetChild(0).GetComponentsInChildren<Rigidbody>();
        ragdollCols = transform.GetChild(0).GetComponentsInChildren<Collider>();
        bodyParts = new List<BodyPart>();

        state = RagdollState.Animated;
        canRecover = 0.0f;
        ActivateRagdollComponents(false);

        //For each of the transforms, create a BodyPart instance and store the transform 
        foreach (Transform t in GetComponentsInChildren(typeof(Transform)))
        {
            BodyPart bodyPart = new BodyPart();
            bodyPart.transform = t;
            bodyParts.Add(bodyPart);
        }
    }

    void Update()
    {
        canRecover += Time.deltaTime;

        //Debug.Log("pelvis velocity:" + pelvis.velocity.magnitude);
        if (state == RagdollState.Ragdolled && canRecover > recoverTime //&& playerController.grounded
            && pelvis.velocity.magnitude < 0.1f)
        {
            RagdollOut();
        }

        if (state == RagdollState.Ragdolled)
        {
            pelvis.isKinematic = true;
            pelvis.transform.localPosition = Vector3.zero;
        }
    }

    public void RagdollIn()
    {
        //Debug.Log("RagdollIn");
        canRecover = 0.0f;
        state = RagdollState.Ragdolled;
        ActivateRagdollComponents(true);

        foreach (BodyPart b in bodyParts)
        {
            b.storedRotation = b.transform.rotation;
            b.storedPosition = b.transform.position;
        }
    }

    private void RagdollOut()
    {
        //Debug.Log("RagdollOut");
        state = RagdollState.BlendToAnim;
        ActivateRagdollComponents(false);
        playerController.enabled = false;
        GetUp();

        animator.transform.parent.localRotation = Quaternion.identity;
    }

    private void GetUp()
    {
        ragdollingEndTime = Time.time;//store the state change time

        //Store the ragdolled position for blending
        foreach (BodyPart b in bodyParts)
        {
            b.storedRotation = b.transform.rotation;
            b.storedPosition = b.transform.position;
        }

        //KeyBone positions to calculate new position
        ragdolledFeetPosition = 0.5f * (animator.GetBoneTransform(HumanBodyBones.LeftToes).position + animator.GetBoneTransform(HumanBodyBones.RightToes).position);
        ragdolledHeadPosition = animator.GetBoneTransform(HumanBodyBones.Head).position;
        ragdolledHipPosition = animator.GetBoneTransform(HumanBodyBones.Hips).position;
    }

    void LateUpdate()
    {
        if (state == RagdollState.BlendToAnim)
        {
            if (Time.time <= ragdollingEndTime + mecanimToGetUpTransitionTime)
            {
                //CALCULTATE POSITION
                Vector3 animatedToRagdolled = ragdolledHipPosition - animator.GetBoneTransform(HumanBodyBones.Hips).position;
                Vector3 newRootPosition = transform.position + animatedToRagdolled;

                //Now cast a ray from the computed position downwards and find the highest hit that does not belong to the character
                RaycastHit hit;
                Physics.Raycast(pelvis.position, Vector3.down, out hit, 1f, collisionMask);
                newRootPosition.y = 0;
                if (hit.collider)
                {
                    newRootPosition.y = hit.point.y;
                }
                //Debug.Log(newRootPosition);
                hitPoint = newRootPosition;
                transform.position = newRootPosition;

                //CALCULATE ROTATION
                //Get body orientation in ground plane for both the ragdolled pose and the animated get up pose
                Vector3 ragdolledDirection = ragdolledHeadPosition - ragdolledFeetPosition;
                ragdolledDirection.y = 0;

                Vector3 meanFeetPosition = 0.5f * (animator.GetBoneTransform(HumanBodyBones.LeftFoot).position + animator.GetBoneTransform(HumanBodyBones.RightFoot).position);
                Vector3 animatedDirection = animator.GetBoneTransform(HumanBodyBones.Head).position - meanFeetPosition;
                animatedDirection.y = 0;

                transform.rotation *= Quaternion.FromToRotation(animatedDirection.normalized, ragdolledDirection.normalized);
            }
            //compute the ragdoll blend amount in the range 0...1
            float ragdollBlendAmount = 1.0f - (Time.time - ragdollingEndTime - mecanimToGetUpTransitionTime) / ragdollToMecanimBlendTime;
            ragdollBlendAmount = Mathf.Clamp01(ragdollBlendAmount);

            //In LateUpdate(), Mecanim has already updated the body pose according to the animations. 
            //To enable smooth transitioning from a ragdoll to animation, we lerp the position of the hips 
            //and slerp all the rotations towards the ones stored when ending the ragdolling
            foreach (BodyPart b in bodyParts)
            {
                if (b.transform != transform)
                { //this if is to prevent us from modifying the root of the character, only the actual body parts
                  //position is only interpolated for the hips
                    if (b.transform == animator.GetBoneTransform(HumanBodyBones.Hips))
                        b.transform.position = Vector3.Lerp(b.transform.position, b.storedPosition, ragdollBlendAmount);
                    //rotation is interpolated for all body parts
                    b.transform.rotation = Quaternion.Slerp(b.transform.rotation, b.storedRotation, ragdollBlendAmount);
                }
            }

            //if the ragdoll blend amount has decreased to zero, move to animated state
            if (ragdollBlendAmount == 0)
            {
                state = RagdollState.Animated;
                playerController.enabled = true;
            }
        }
    }

    private void ActivateRagdollComponents(bool activate)
    {
        Vector3 playerSpeed = this.GetComponent<Rigidbody>().velocity;

        //cp.isTrigger = activate;
        GetComponent<Rigidbody>().freezeRotation = !activate;
        GetComponent<Rigidbody>().isKinematic = activate;

        playerController.enabled = !activate;

        if (activate)
            ResetAnimations();
        animator.enabled = !activate;

        for (int i = 1; i < ragdollRbs.Length; i++)
            ragdollRbs[i].isKinematic = !activate;
        for (int i = 1; i < ragdollCols.Length; i++)
            ragdollCols[i].enabled = activate;

        if (activate)
            pelvis.velocity = playerSpeed * 4f;
    }

    private void ResetAnimations()
    {
        animator.SetBool("isRunning", false);
        animator.SetBool("isJumping", false);
        animator.SetBool("isDiving", false);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(hitPoint, 1f);
    }
}

public class BodyPart
{
    public Transform transform;
    public Vector3 storedPosition;
    public Quaternion storedRotation;
}


public enum RagdollState
{
    Animated,    //Mecanim is fully in control
    Ragdolled,   //Mecanim turned off, physics controls the ragdoll
    BlendToAnim  //Mecanim in control, but LateUpdate() is used to partially blend in the last ragdolled pose
}
