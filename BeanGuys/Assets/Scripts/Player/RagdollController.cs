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
    [SerializeField]
    private ConfigurableJoint cj;
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
    public float BounceModifier { get; private set; } = 60;
    [SerializeField]
    public float MinForce { get; private set; } = 15;

    [Header("Transition Params")]
    [SerializeField]
    public float ragdollToMecanimBlendTime = 1f; // Transitioning time from ragdolled to animated
    [SerializeField]
    private float mecanimToGetUpTransitionTime = 0.05f;
    private float ragdollingEndTime = -100; // A helper variable to store the time when we transitioned from ragdolled to blendToAnim state

    // Additional vectores for storing the pose the ragdoll ended up in.
    private Vector3 ragdolledHipPosition, ragdolledHeadPosition, ragdolledFeetPosition;

    private Vector3 ragdolledDirection;
    public float t;

    public void StartController()
    {
        playerController = GetComponent<PlayerController>();
        animator = transform.GetChild(0).GetComponent<Animator>();
        cp = GetComponent<CapsuleCollider>();

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
        if(State == RagdollState.Ragdolled)
        {
            canRecover += Time.deltaTime;

            if (canRecover > recoverTime && pelvis.velocity.magnitude < 0.1f)
                StartBlendAnim();
        }

        if (Input.GetKeyDown(KeyCode.Z) && State == RagdollState.Animated)
        {
            playerController.EnterRagdoll(transform.position);
        }
    }

    private void LateUpdate()
    {
        if (State == RagdollState.BlendToAnim)
        {
            t += Time.deltaTime / ragdollToMecanimBlendTime;

            // Lerp Position
            Vector3 animatedToRagdolled = ragdolledHipPosition - animator.GetBoneTransform(HumanBodyBones.Hips).position;
            Vector3 newRootPosition = transform.position + animatedToRagdolled;

            //Now cast a ray from the computed position downwards and find the highest hit that does not belong to the character
            RaycastHit hit;
            Physics.Raycast(pelvis.position, Vector3.down, out hit, 1f, collisionMask);
            newRootPosition.y = 0;
            if (hit.collider)
                newRootPosition.y = hit.point.y;
            transform.position = newRootPosition;

            // Lerp Rotation
            //Get body orientation in ground plane for both the ragdolled pose and the animated get up pose
            ragdolledDirection = ragdolledHeadPosition - ragdolledFeetPosition;
            ragdolledDirection.y = 0;
            Vector3 meanFeetPosition = 0.5f * (animator.GetBoneTransform(HumanBodyBones.LeftFoot).position + animator.GetBoneTransform(HumanBodyBones.RightFoot).position);
            Vector3 animatedDirection = animator.GetBoneTransform(HumanBodyBones.Head).position - meanFeetPosition;
            animatedDirection.y = 0;
            // Set new rotation
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(ragdolledDirection, Vector3.up), t);

            // Keep modedl object on 0,0,0,0
            transform.GetChild(0).localRotation = Quaternion.identity;

            //if the ragdoll blend amount has increased to or more than one, move to animated state
            if (t >= 1)
            {
                playerController.ExitRagdoll();
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

    /// <summary>Sets the player to ragdoll state.</summary>
    public void RagdollIn()
    {
        canRecover = 0.0f;

        cp.enabled = false;
        cj.xMotion = ConfigurableJointMotion.Locked;
        cj.yMotion = ConfigurableJointMotion.Locked;
        cj.zMotion = ConfigurableJointMotion.Locked;

        State = RagdollState.Ragdolled;
        ActivateRagdollComponents(true);

        // Save positions and rotations
        foreach (BodyPart b in bodyParts)
        {
            b.storedRotation = b.transform.rotation;
            b.storedPosition = b.transform.position;
        }
    }

    /// <summary>Returns the player to animated state.</summary>
    public void RagdollOut()
    {
        if (State != RagdollState.Animated)
        {
            // Just for safety set x and z to 0
            transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0); 

            State = RagdollState.Animated;
            cp.enabled = true;
            ActivateRagdollComponents(false);

            cj.xMotion = ConfigurableJointMotion.Free;
            cj.yMotion = ConfigurableJointMotion.Free;
            cj.zMotion = ConfigurableJointMotion.Free;
        }
    }

    /// <summary>Starts the blend transition from ragdoll to animated.</summary>
    public void StartBlendAnim()
    {
        State = RagdollState.BlendToAnim;
        ActivateRagdollComponents(false);
        playerController.SetCurrentionAnimation(-1);
        GetUp();
        t = 0;
    }

    private void GetUp()
    {
        ragdollingEndTime = Time.time; // store the state change time

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

    public void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.position, transform.position + (ragdolledDirection * 10));
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

