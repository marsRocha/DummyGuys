using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Contains all methods to control the remote player's ragdoll.
/// </summary>
public class RemoteRagdollController : MonoBehaviour
{
    // Components
    private Animator animator;
    private CapsuleCollider cp;
    private Rigidbody rb;
#pragma warning disable 0649
    [SerializeField]
    private ConfigurableJoint cj;
    [SerializeField]
    private Rigidbody pelvis;
#pragma warning restore 0649

    // Ragdoll components
    private Rigidbody[] ragdollRbs;
    private Collider[] ragdollCols;
    private List<BodyPart> bodyParts;

#pragma warning disable 0649
    // Ragdoll Params
    [SerializeField]
    public RagdollState State { get; private set; }
    [SerializeField]
    private LayerMask collisionMask;
    [SerializeField]
    private float recoverTime;
#pragma warning restore 0649

    // Collision Params
    [SerializeField]
    public float Modifier { get; private set; } = 2;
    [SerializeField]
    public float ObstacleModifier { get; private set; } = 8;
    [SerializeField]
    public float BounceModifier { get; private set; } = 20;
    [SerializeField]
    public float MinForce { get; private set; } = 15;

#pragma warning disable 0649
    [Header("Transition Params")]
    [SerializeField]
    public float ragdollToMecanimBlendTime = 1f; // Transitioning time from ragdolled to animated
#pragma warning restore 0649
    private float ragdollingEndTime = -100; // A helper variable to store the time when we transitioned from ragdolled to blendToAnim state

    // Additional vectores for storing the pose the ragdoll ended up in.
    private Vector3 ragdolledHipPosition, ragdolledHeadPosition, ragdolledFeetPosition;

    public void StartController()
    {
        animator = transform.GetChild(0).GetComponent<Animator>();
        cp = GetComponent<CapsuleCollider>();

        ragdollRbs = transform.GetChild(0).GetComponentsInChildren<Rigidbody>();
        ragdollCols = transform.GetChild(0).GetComponentsInChildren<Collider>();
        bodyParts = new List<BodyPart>();

        State = RagdollState.Animated;
        ActivateRagdollComponents(false);

        // Create a BodyPart instance, for each of the transforms, and store it
        foreach (Transform t in GetComponentsInChildren(typeof(Transform)))
        {
            BodyPart bodyPart = new BodyPart();
            bodyPart.transform = t;
            bodyParts.Add(bodyPart);
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
        State = RagdollState.Animated;
        ActivateRagdollComponents(false);

        cp.enabled = true;
        cj.xMotion = ConfigurableJointMotion.Free;
        cj.yMotion = ConfigurableJointMotion.Free;
        cj.zMotion = ConfigurableJointMotion.Free;
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
}

