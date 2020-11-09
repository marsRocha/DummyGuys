using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QualifyPlayer : MonoBehaviour
{
    public ParticleSystem confetti;

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            confetti.Play();

            //finish game for him
            other.gameObject.SetActive(false);

            //appear UI
            GameManager.instance.FinishRaceForPlayer();
        }
    }
}
