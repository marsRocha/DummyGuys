using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPlayerRagdoll : MonoBehaviour
{
    [Header("Components")]
    public NPlayerController playerController;
    public Animator animator;
    public CapsuleCollider cp;

    //Ragdoll components
    Rigidbody[] ragdollRbs;
    Collider[] ragdollCols;
    List<BodyPart> bodyParts;

    [Header("Ragdoll states")]
    [SerializeField]
    RagdollState state;
    public bool ragdollActive;
    private float canRecover;
    public float recoverTime;

    //Time transitioning from ragdolled to animated
    public float ragdollToMecanimBlendTime = 0.5f;
    public float mecanimToGetUpTransitionTime = 0.05f;

    //A helper variable to store the time when we transitioned from ragdolled to blendToAnim state
    float ragdollingEndTime = -100;

    //Additional vectores for storing the pose the ragdoll ended up in.
    Vector3 ragdolledHipPosition, ragdolledHeadPosition, ragdolledFeetPosition;


    //Impact variables
    float impactEndTime = 0;
    Rigidbody impactTarget = null;
    Vector3 impact;

    public float onAirTime = 0.0f;

    void Start()
    {
        ragdollRbs = this.gameObject.GetComponentsInChildren<Rigidbody>();
        ragdollCols = this.gameObject.GetComponentsInChildren<Collider>();
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
        if (playerController != null)
        {
            if (playerController.grounded)
                onAirTime = 0.0f;
            else onAirTime += Time.deltaTime;
        }

        canRecover += Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Z) && state == RagdollState.Animated)
        {
            RagdollIn();
            //animator.SetBool("isStandingUp", true); //GetUpFromBack
            Debug.Log("Stading?");
            canRecover = 0.0f;
        }


        if (state == RagdollState.Ragdolled && canRecover > recoverTime && GetComponentInChildren<Rigidbody>().velocity.magnitude < 0.0001f)
        {
            Debug.Log("true af");
            RagdollOut();
        }

        //verify this on collision
        /*if (state == RagdollState.Animated && onAirTime > 3f)
            RagdollIn();*/

        //CheckImpact();
    }

    public void RagdollIn()
    {
        Debug.Log("Going ragdoll");
        state = RagdollState.Ragdolled;
        ActivateRagdollComponents(true);

        foreach (BodyPart b in bodyParts)
        {
            b.storedRotation = b.transform.rotation;
            b.storedPosition = b.transform.position;
        }
    }

    private IEnumerator WaitFor(float time)
    {
        yield return new WaitForSecondsRealtime(time);
    }

        private void RagdollOut()
    {
        state = RagdollState.BlendToAnim;
        ActivateRagdollComponents(false);
        playerController.enabled = false;
        GetUp();
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

        //Remember some key positions
        ragdolledFeetPosition = 0.5f * (animator.GetBoneTransform(HumanBodyBones.LeftToes).position + animator.GetBoneTransform(HumanBodyBones.RightToes).position);
        ragdolledHeadPosition = animator.GetBoneTransform(HumanBodyBones.Head).position;
        ragdolledHipPosition = animator.GetBoneTransform(HumanBodyBones.Hips).position;
        //animator.SetBool("isStandingUp", true); //GetUpFromBack
    }

    void LateUpdate()
    {
        if (state == RagdollState.BlendToAnim)
        {
            if (Time.time <= ragdollingEndTime + mecanimToGetUpTransitionTime)
            {
                //If we are waiting for Mecanim to start playing the get up animations, update the root of the mecanim
                //character to the best match with the ragdoll
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

                //Get body orientation in ground plane for both the ragdolled pose and the animated get up pose
                Vector3 ragdolledDirection = ragdolledHeadPosition - ragdolledFeetPosition;
                ragdolledDirection.y = 0;

                Vector3 meanFeetPosition = 0.5f * (animator.GetBoneTransform(HumanBodyBones.LeftFoot).position + animator.GetBoneTransform(HumanBodyBones.RightFoot).position);
                Vector3 animatedDirection = animator.GetBoneTransform(HumanBodyBones.Head).position - meanFeetPosition;
                animatedDirection.y = 0;

                //Try to match the rotations. Note that we can only rotate around Y axis, as the animated characted must stay upright,
                //hence setting the y components of the vectors to zero. 
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
        ragdollActive = activate;
        cp.isTrigger = activate;
        this.GetComponent<Rigidbody>().isKinematic = activate;

        playerController.enabled = !activate;
        animator.enabled = !activate;


        for (int i = 1; i < ragdollRbs.Length; i++)
            ragdollRbs[i].isKinematic = !activate;
        for (int i = 1; i < ragdollCols.Length; i++)
            ragdollCols[i].enabled = activate;
    }

    private void CheckImpact()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                //check if the raycast target has a rigid body (belongs to the ragdoll)
                if (hit.rigidbody != null)
                {
                    //find the RagdollHelper component and activate ragdolling
                    RagdollIn();

                    //set the impact target to whatever the ray hit
                    impactTarget = hit.rigidbody;

                    //impact direction also according to the ray
                    impact = ray.direction * 2.0f;

                    //the impact will be reapplied for the next 250ms
                    impactEndTime = Time.time + 0.25f;
                }
            }
        }

        //Check if we need to apply an impact
        if (Time.time < impactEndTime)
            impactTarget.AddForce(impact, ForceMode.VelocityChange);
    }
}

enum RagdollState
{
    Animated,    //Mecanim is fully in control
    Ragdolled,   //Mecanim turned off, physics controls the ragdoll
    BlendToAnim  //Mecanim in control, but LateUpdate() is used to partially blend in the last ragdolled pose
}

public class BodyPart
{
    public Transform transform;
    public Vector3 storedPosition;
    public Quaternion storedRotation;
}
