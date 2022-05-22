using UnityEngine;

public class DeathCollider : MonoBehaviour
{
    private void OnTriggerEnter(Collider _other)
    {
        // This uses the player layer on the ragdoll pelvis, since the capsule from the player object is disabled
        if (_other.gameObject.layer.Equals(LayerMask.NameToLayer("Player")))
        {
            ClientSend.PlayerRespawn();
            _other.gameObject.transform.root.GetComponent<PlayerController>().Die();
        }
        else if (_other.gameObject.layer.Equals(LayerMask.NameToLayer("RemotePlayer")))
        {
            _other.gameObject.GetComponent<RemotePlayerManager>().Die();
        }
    }
}
