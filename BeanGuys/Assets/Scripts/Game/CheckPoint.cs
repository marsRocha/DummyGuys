using UnityEngine;

// Doesn't really affect the game, it's only used for visual purposes such as activating particle systems or audio effects
public class CheckPoint : MonoBehaviour
{
    [SerializeField]
    private int checkpointIndex;

    private void OnTriggerEnter(Collider _other)
    {
        if (_other.gameObject.layer.Equals(LayerMask.NameToLayer("Player")))
        {
            if (_other.gameObject.GetComponent<PlayerManager>().Checkpoint < checkpointIndex)
                _other.gameObject.GetComponent<PlayerManager>().SetCheckpoint(checkpointIndex);
        }
        else if (_other.gameObject.layer.Equals(LayerMask.NameToLayer("RemotePlayer")))
        {
            if(_other.gameObject.GetComponent<RemotePlayerManager>().Checkpoint < checkpointIndex)
            _other.gameObject.GetComponent<RemotePlayerManager>().SetCheckpoint(checkpointIndex);
        }
    }
}
