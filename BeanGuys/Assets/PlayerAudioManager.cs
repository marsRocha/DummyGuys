using System;
using UnityEngine;

public class PlayerAudioManager : MonoBehaviour
{
    [SerializeField]
    private AudioClip[] sounds;
    [SerializeField]
    private AudioSource movementAudio;
    [SerializeField]
    private AudioSource impactAudio;
    [SerializeField]
    private AudioSource effectstAudio;

    #region Movement Audio
    // Jump = 0, Dive = 1
    public void PlayMovement(int _index)
    {
        movementAudio.clip = sounds[_index];
        movementAudio.Play();
    }
    
    public void StopMovement()
    {
        movementAudio.Stop();
    }
    #endregion

    #region Impact Audio
    public void PlayImpact(int _index)
    {
        impactAudio.clip = sounds[_index];
        impactAudio.Play();
    }

    public void StopImpact()
    {
        impactAudio.Stop();
    }
    #endregion

    #region Effects Audio
    // Checkpoint = 4, Die = 5, Respawn = 6
    public void PlayEffect(int _index)
    {
        effectstAudio.clip = sounds[_index];
        effectstAudio.Play();
    }

    public void StopEffect()
    {
        effectstAudio.Stop();
    }
    #endregion
}
