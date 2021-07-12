using UnityEngine;

public class DeathCollider : MonoBehaviour
{
    private void OnTriggerEnter(Collider _other)
    {
        if (_other.gameObject.layer.Equals(LayerMask.NameToLayer("Player")))
        {
            _other.gameObject.GetComponent<PlayerController>().Die();
        }
        else if (_other.gameObject.layer.Equals(LayerMask.NameToLayer("RemotePlayer")))
        {
            _other.gameObject.GetComponent<RemotePlayerManager>().Die();
        }
    }
}
