using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QualifyPlayer : MonoBehaviour
{
    private RoomScene roomScene;

    // Start is called before the first frame update
    private void Start()
    {
        foreach (GameObject obj in gameObject.scene.GetRootGameObjects())
        {
            if (obj.GetComponent<RoomScene>())
            {
                roomScene = obj.GetComponent<RoomScene>();
                break;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            roomScene.FinishRacePlayer(other.gameObject.GetComponent<Player>().Id);
        }
    }
}
