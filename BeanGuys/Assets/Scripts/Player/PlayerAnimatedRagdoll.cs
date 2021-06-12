using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimatedRagdoll : MonoBehaviour
{
    [Header("Components")]
    public PlayerController playerController;
    public Animator animator;
    public CapsuleCollider cp;
    public Rigidbody rb;

    [Header("Ragdoll Settings")]
    [SerializeField]
    public RagdollState state;
    public bool ragdollActive;
    private float canRecover;
    public float recoverTime;
    public LayerMask ragdollMask;
    public float maxForce;
    public float impactForce;

    //Time transitioning from ragdolled to animated
    public float ragdollToMecanimBlendTime = 0.5f;
    public float mecanimToGetUpTransitionTime = 0.05f;
    //A helper variable to store the time when we transitioned from ragdolled to blendToAnim state
    float ragdollingEndTime = -100;
    //Additional vectores for storing the pose the ragdoll ended up in.
    Vector3 ragdolledHipPosition, ragdolledHeadPosition, ragdolledFeetPosition;

    void Start()
    {
        Rigidbody[] ragdollRbs = this.gameObject.GetComponentsInChildren<Rigidbody>();
        Collider[] ragdollCols = this.gameObject.GetComponentsInChildren<Collider>();

        for (int i = 1; i < ragdollRbs.Length; i++)
            ragdollRbs[i].isKinematic = true;
        for (int i = 1; i < ragdollCols.Length; i++)
            ragdollCols[i].enabled = false;

        state = RagdollState.Animated;
        canRecover = 0.0f;
        ActivateRagdollComponents(false);
    }

    // Update is called once per frame
    void Update()
    {
        canRecover += Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Z) && state == RagdollState.Animated)
        {
            RagdollIn();
        }

        //Debug.Log("pelvis velocity:" + rb.velocity.magnitude);

        if (state == RagdollState.Ragdolled && canRecover > recoverTime //&& playerController.grounded
            && rb.velocity.magnitude < 2f)
        {
            RagdollOut();
        }
    }

    public void RagdollIn()
    {
        Debug.Log("RagdollIn");
        canRecover = 0.0f;
        state = RagdollState.Ragdolled;
        ActivateRagdollComponents(true);
    }

    private void RagdollOut()
    {
        Debug.Log("RagdollOut");
        state = RagdollState.BlendToAnim;
        ActivateRagdollComponents(false);
        GetUp();
    }


    private void GetUp()
    {
        ragdollingEndTime = Time.time;//store the state change time

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
                RaycastHit[] hits = Physics.RaycastAll(new Ray(newRootPosition, Vector3.down));
                newRootPosition.y = 0;
                foreach (RaycastHit hit in hits)
                {
                    if (!hit.transform.IsChildOf(transform))
                    {
                        newRootPosition.y = Mathf.Max(newRootPosition.y, hit.point.y);
                    }
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

            //if the ragdoll blend amount has decreased to zero, move to animated state
            if (ragdollBlendAmount == 0)
            {
                state = RagdollState.Animated;
                //TODO: UNCOMMENT THIS
                //playerController.ragdolled = false;
            }
        }
    }


    private void ActivateRagdollComponents(bool activate)
    {
        ragdollActive = activate;

        if (activate)
            rb.constraints = RigidbodyConstraints.None;
        else
            rb.constraints = RigidbodyConstraints.FreezeRotation;

        //TODO: UNCOMMENT both of THese lines
        //playerController.camera.GetComponent<PlayerCamera>().followRagdoll = activate;
        //playerController.ragdolled = activate;
        animator.SetBool("isRagdolled", activate);
    }
}
