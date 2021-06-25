using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckPoint : MonoBehaviour
{
    [SerializeField]
    private int checkpointIndex;

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.root.gameObject.layer.Equals(LayerMask.NameToLayer("Player")))
        {
            MapController.instance.SetCheckPoint(checkpointIndex);
        }
    }
}
