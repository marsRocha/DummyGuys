using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QualifyPlayer : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            //finish game for him

            //appear UI

            //go to random player camera
        }
    }
}
