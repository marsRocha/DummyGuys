using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Components
    private Rigidbody rb;
    [SerializeField]
#pragma warning disable 0649
    private Transform pelvis;
#pragma warning restore 0649
    private CapsuleCollider cp;
    private LogicTimer logicTimer;
    private RagdollController ragdollController;
    private PhysicsScene physicsScene;

#pragma warning disable 0649
    [SerializeField]
    private LayerMask collisionMask;
    [SerializeField]
    private LayerMask interactionMask;
#pragma warning restore 0649

    [SerializeField]
    private bool Grabbing;

    public void Initialize(LogicTimer _logicTimer)
    {
        logicTimer = _logicTimer;

        cp = GetComponent<CapsuleCollider>();
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = Vector3.up * 0.9009846f;
        physicsScene = gameObject.scene.GetPhysicsScene();
        ragdollController = GetComponent<RagdollController>();

        Grabbing = false;
    }

    /// <summary>Verifies if player state received is valide.</summary>
    /// <param name="_clientState">player state received from the player</param>
    /// <param name="_serverState">privous valide player state</param>
    public bool ProcessState(PlayerState _clientState, PlayerState _serverState)
    {
        // If changed position, rotation verify if is not clipping through the map (Wall/Floor)
        if ((_clientState.position != _serverState.position) || (_clientState.rotation != _serverState.rotation))
        {
            if (!_clientState.ragdoll)
            {
                Vector3 direction = _clientState.position - transform.position;
                RaycastHit _hit;
                bool hit = physicsScene.CapsuleCast((transform.position + cp.center + (new Vector3(0, cp.height / 2 - cp.radius - 0.1f, 0))),
                                                (transform.position - cp.center + (new Vector3(0, cp.height / 2 + cp.radius + 0.2f, 0))),
                                                cp.radius - 0.1f, direction, out _hit, direction.magnitude, collisionMask);
                if (hit)
                {
                    //Debug.Log("Player is inside map");
                    return false;
                }
            }
        }

        return true;
    }

    #region Actions
    public Guid TryGrab()
    {
        RaycastHit hit;
        physicsScene.Raycast(pelvis.position, transform.forward, out hit, 1, interactionMask);
        if (hit.collider)
        {
            return hit.collider.transform.root.gameObject.GetComponent<Player>().Id;
        }

        return Guid.Empty;
    }

    public void Grab()
    {
        Grabbing = true;
    }

    public bool GetGrab()
    {
        return Grabbing;
    }

    public void LetGo()
    {
        Grabbing = false;
    }

    public Guid TryPush()
    {
        RaycastHit hit;
        physicsScene.Raycast(pelvis.position, transform.forward, out hit, 2, interactionMask);
        if (hit.collider)
        {
            return hit.collider.transform.root.GetComponent<Player>().Id;
        }

        return Guid.Empty;
    }
    #endregion
}
