using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Contains all methods to control the local and remote player's ragdoll.
/// </summary>
public class RagdollController : MonoBehaviour
{
    // Components
    private PlayerController playerController;
    private Animator animator;
    private CapsuleCollider cp;
    private Rigidbody rb;
    [SerializeField]
    private Rigidbody pelvis;

    // Ragdoll components
    private Rigidbody[] ragdollRbs;
    private Collider[] ragdollCols;
    private List<BodyPart> bodyParts;

    // Ragdoll Params
    [SerializeField]
    public RagdollState State { get; private set; }
    [SerializeField]
    private LayerMask collisionMask;
    private float canRecover;
    [SerializeField]
    private float recoverTime;

    // Collision Params
    [SerializeField]
    public float Modifier { get; private set; } = 2;
    [SerializeField]
    public float ObstacleModifier { get; private set; } = 8;
    [SerializeField]
    public float BounceModifier { get; private set; } = 8;
    [SerializeField]
    public float MinForce { get; private set; } = 15;

    [Header("Transition Params")]
    [SerializeField]
    public float ragdollToMecanimBlendTime = 0.5f; // Transitioning time from ragdolled to animated
    [SerializeField]
    private float mecanimToGetUpTransitionTime = 0.05f;
    private float ragdollingEndTime = -100; // A helper variable to store the time when we transitioned from ragdolled to blendToAnim state

    // Additional vectores for storing the pose the ragdoll ended up in.
    private Vector3 ragdolledHipPosition, ragdolledHeadPosition, ragdolledFeetPosition;

    // For remotePlayers
    public bool remotePlayer;

    public void StartController()
    {
        playerController = GetComponent<PlayerController>();
        animator = transform.GetChild(0).GetComponent<Animator>();
        cp = GetComponent<CapsuleCollider>();

        if(!remotePlayer)
            rb = GetComponent<Rigidbody>();

        ragdollRbs = transform.GetChild(0).GetComponentsInChildren<Rigidbody>();
        ragdollCols = transform.GetChild(0).GetComponentsInChildren<Collider>();
        bodyParts = new List<BodyPart>();

        State = RagdollState.Animated;
        canRecover = 0.0f;
        ActivateRagdollComponents(false);

        // Create a BodyPart instance, for each of the transforms, and store it
        foreach (Transform t in GetComponentsInChildren(typeof(Transform)))
        {
            BodyPart bodyPart = new BodyPart();
            bodyPart.transform = t;
            bodyParts.Add(bodyPart);
        }
    }

    public void UpdateController()
    {
        canRecover += Time.deltaTime;

        if (State == RagdollState.Ragdolled && canRecover > recoverTime //&& playerController.grounded
            && pelvis.velocity.magnitude < 0.1f)
        {
            RagdollOut();
        }
    }

    public void FixedUpdateController()
    {
        if (State == RagdollState.BlendToAnim)
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

            // if the ragdoll blend amount has decreased to zero, move to animated state
            if (ragdollBlendAmount == 0)
            {
                State = RagdollState.Animated;
                playerController.ExitRagdoll();
                GetComponent<PlayerManager>().Ragdolled = false;
                animator.transform.localRotation = Quaternion.identity;
            }
        }
    }

    private void ActivateRagdollComponents(bool activate)
    {
        animator.enabled = !activate;

        for (int i = 1; i < ragdollRbs.Length; i++)
        {
            ragdollRbs[i].isKinematic = !activate;
        }
        for (int i = 1; i < ragdollCols.Length; i++)
            ragdollCols[i].enabled = activate;
    }

    public void RagdollIn()
    {
        canRecover = 0.0f;
        State = RagdollState.Ragdolled;
        ActivateRagdollComponents(true);

        pelvis.isKinematic = true;

        // Save positions and rotations
        foreach (BodyPart b in bodyParts)
        {
            b.storedRotation = b.transform.rotation;
            b.storedPosition = b.transform.position;
        }
    }

    public void RagdollOut()
    {
        if(!remotePlayer)
            rb.isKinematic = false;
        State = RagdollState.BlendToAnim;
        ActivateRagdollComponents(false);
        //GetUp();

        //TODOD: FOR NOW
        State = RagdollState.Animated;
        if (!remotePlayer)
        {
            GetComponent<PlayerManager>().Ragdolled = false;
            playerController.ExitRagdoll();
        }
        animator.transform.localRotation = Quaternion.identity;
        transform.rotation = Quaternion.identity;
    }

    /// <summary>Returns the player instantly to animated state.</summary>
    public void BackToAnimated()
    {
        if (!remotePlayer)
            rb.isKinematic = false;
        ActivateRagdollComponents(false);
        State = RagdollState.Animated;

        if (!remotePlayer)
        {
            GetComponent<PlayerManager>().Ragdolled = false;
            playerController.ExitRagdoll();
        }
        animator.transform.localRotation = Quaternion.identity;
        transform.rotation = Quaternion.identity;
    }

    private void GetUp()
    {
        ragdollingEndTime = Time.time;//store the state change time

        // Store the ragdolled position for blending
        foreach (BodyPart b in bodyParts)
        {
            b.storedRotation = b.transform.rotation;
            b.storedPosition = b.transform.position;
        }

        // KeyBone positions to calculate new position
        ragdolledFeetPosition = 0.5f * (animator.GetBoneTransform(HumanBodyBones.LeftToes).position + animator.GetBoneTransform(HumanBodyBones.RightToes).position);
        ragdolledHeadPosition = animator.GetBoneTransform(HumanBodyBones.Head).position;
        ragdolledHipPosition = animator.GetBoneTransform(HumanBodyBones.Hips).position;
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
    Animated,    // Mecanim is fully in control
    Ragdolled,   // Mecanim turned off, physics controls the ragdoll
    BlendToAnim  // Mecanim in control, but LateUpdate() is used to partially blend in the last ragdolled pose
}

