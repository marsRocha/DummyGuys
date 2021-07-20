using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckPoint : MonoBehaviour
{
    private RoomScene roomScene;

    [SerializeField]
    private int checkpointIndex;

    private void Start()
    {
        roomScene = gameObject.scene.GetRootGameObjects()[0].GetComponent<RoomScene>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.root.gameObject.layer.Equals(LayerMask.NameToLayer("Player")))
        {
            roomScene.SetCheckPoint(other.transform.root.GetComponent<Player>().Id, checkpointIndex);
        }
    }
}
