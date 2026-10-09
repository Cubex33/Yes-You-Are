using UnityEngine;

public class PlayerAudioEffects : MonoBehaviour
{
    [Header("Settings")]
    public AudioSource audioSource;
    [Header("Reloading")]
    public AudioClip reloadingClip;
    [Header("Foot Steps")]
    public AudioClip[] footstepClips;
    [Header("Debug")]
    public bool debugEnabled;
    void PlayFootstep()
    {
        if (debugEnabled) Debug.Log("Playing footstep sound");
        if (footstepClips.Length == 0) return;
        int index = Random.Range(0, footstepClips.Length);
        audioSource.PlayOneShot(footstepClips[index]);
    }

    void PlayReloading()
    {
        if (reloadingClip == null) return;
        audioSource.PlayOneShot(reloadingClip);
    }

}