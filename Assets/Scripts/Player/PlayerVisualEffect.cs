using UnityEngine;

public class PlayerVisualEffect : MonoBehaviour
{
    public ParticleSystem footstepEffect;
    public ParticleSystem attackEffect;
    public ParticleSystem ReloadEffect;

    public void PlayFootstepEffect()
    {
        if (footstepEffect != null) footstepEffect.Play();
    }

    public void PlayAttackEffect()
    {
        if (attackEffect != null) attackEffect.Play();
    }

    public void PlayReloadEffect()
    {
        if (ReloadEffect != null) ReloadEffect.Play();
    }
}