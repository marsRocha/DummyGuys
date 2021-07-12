using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QualifyPlayer : MonoBehaviour
{
    private RoomScene roomScene;

    private void Start()
    {
        roomScene = gameObject.scene.GetRootGameObjects()[0].GetComponent<RoomScene>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            roomScene.FinishRacePlayer(other.gameObject.GetComponent<Player>().id);
        }
    }
}
