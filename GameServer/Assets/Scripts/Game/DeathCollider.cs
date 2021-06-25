﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathCollider : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.root.gameObject.layer.Equals(LayerMask.NameToLayer("Player")))
        {
            RoomScene.instance.PlayerRespawn(other.transform.root.GetComponent<Player>().id);
        }
    }
}
