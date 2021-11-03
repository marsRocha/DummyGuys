using UnityEngine;

// Doesn't really affect the game, it's only used for visual purposes such as activating particle systems or audio effects
public class CheckPoint : MonoBehaviour
{
#pragma warning disable 0649
    [SerializeField]
    private int checkpointIndex;
#pragma warning restore 0649

    private void OnTriggerEnter(Collider _other)
    {
        if (_other.gameObject.layer.Equals(LayerMask.NameToLayer("Player")))
        {
            if (MapController.instance.localPlayer.Checkpoint < checkpointIndex)
                MapController.instance.localPlayer.SetCheckpoint(checkpointIndex);
        }
        else if (_other.gameObject.layer.Equals(LayerMask.NameToLayer("RemotePlayer")))
        {
            if(_other.gameObject.transform.root.GetComponent<RemotePlayerManager>().Checkpoint < checkpointIndex)
            _other.gameObject.GetComponent<RemotePlayerManager>().SetCheckpoint(checkpointIndex);
        }
    }
}
