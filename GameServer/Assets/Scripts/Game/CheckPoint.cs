﻿using UnityEngine;

public class CheckPoint : MonoBehaviour
{
    private RoomScene roomScene;

#pragma warning disable 0649
    [SerializeField]
    private int checkpointIndex;
#pragma warning restore 0649

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
        if (other.transform.root.gameObject.layer.Equals(LayerMask.NameToLayer("Player")))
        {
            roomScene.SetCheckPoint(other.transform.root.GetComponent<Player>().Id, checkpointIndex);
        }
    }
}
