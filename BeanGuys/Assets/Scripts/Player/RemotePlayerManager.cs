using System;
using UnityEngine;

/// <summary>
/// Contains all methods to control a remote player.
/// </summary>
public class RemotePlayerManager : MonoBehaviour
{
    public Guid Id;
    public string Username;

    private Animator animator;
    public int currentAnimation { get; private set; }

    private Interpolator interpolator;
    private RagdollController ragdollController;
    public bool Ragdolled { get; private set; }

    public ParticleSystem jumpPs;

    // Start is called before the first frame update
    void Start()
    {
        animator = transform.GetChild(0).GetComponent<Animator>();
        interpolator = GetComponent<Interpolator>();
        ragdollController = GetComponent<RagdollController>();
        ragdollController.StartController();
        Ragdolled = false;

        interpolator.StartInterpolator(this);
    }

    /// <summary>Setup remote player's information.</summary>
    /// <param name="_id">The id of the player.</param>
    /// <param name="_username">The username of the player.</param>
    public void Initialize(Guid _id, string _username)
    {
        Id = _id;
        Username = _username;
    }

    /// <summary>Adds a newly received player state to the player's interpolator.</summary>
    /// <param name="_tick">Tick of the player state.</param>
    /// <param name="_position">Position of the player state.</param>
    /// <param name="_rotation">Rotation of the player state.</param>
    /// <param name="_ragdoll">If player is ragdolled or not.</param>
    public void NewPlayerState(int _tick, Vector3 _position, Quaternion _rotation, bool _ragdoll, int _animation)
    {
        interpolator.NewPlayerState(_tick, _position, _rotation, _ragdoll, _animation);
    }

    /// <summary>Activates ragdoll.</summary>
    public void EnterRagdoll()
    {
        ragdollController.RagdollIn();
        Ragdolled = true;
    }
    
    /// <summary>Deactivates ragdoll.</summary>
    public void ExitRagdoll()
    {
        ragdollController.RagdollOut();
        Ragdolled = false;
    }

    /// <summary>Sets the animation to be played by the animator.</summary>
    public void SetAnimation(int _animation)
    {
        // Change animation only if necessary, no need to re-do animation
        if(currentAnimation != _animation)
        {
            ResetAnimator();

            switch (_animation)
            {
                // idle
                case 0:
                    // Do nothing, idle animation does not require a value to be set to 'true'
                    break;
                // running
                case 1:
                    animator.SetBool("isRunning", true);
                    break;
                // jumping
                case 2:
                    animator.SetBool("isJumping", true);
                    JumpEfx();
                    break;
                // diving
                case 3:
                    animator.SetBool("isDiving", true);
                    break;
            }

            currentAnimation = _animation;
        }
    }

    /// <summary>Sets the animator to idle animation by setting all other parameters to false.</summary>
    public void ResetAnimator()
    {
        animator.SetBool("isRunning", false);
        animator.SetBool("isJumping", false);
        animator.SetBool("isDiving", false);
    }

    public void JumpEfx()
    {
        jumpPs.Play();
    }
}
