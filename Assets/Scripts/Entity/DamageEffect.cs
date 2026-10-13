using System;
using Lucy.Core;
using UnityEngine;

[RequireComponent(typeof(Entity))]
public class DamageEffect : MonoBehaviour
{
    [Header("Debug")]
    public ParticleSystem damageParticles;
    public AudioSource damageSound;
    public Entity entity;

    private void Awake()
    {
        entity = GetComponent<Entity>();
        damageParticles ??= GetComponent<ParticleSystem>();
        if (TryGetComponent<AudioSource>(out var audioSource))
            damageSound = audioSource;
        entity.OnDamageTaken += PlayEffect;
    }

    private void OnDestroy()
    {
        entity.OnDamageTaken -= PlayEffect;
    }


    public void PlayEffect(float arg1, object arg2)
    {
        Debug.Log(
            $"[DamageEffect] Объект: {name} | Получен урон: {arg1} | Источник: {arg2}",
            this
        );

        if (damageParticles != null)
        {
            damageParticles.transform.position = entity.transform.position;
            damageParticles.Play();
        }

        if (damageSound != null)
        {
            Debug.Log(
                $"[DamageEffect] Запускаю AudioSource: {damageSound.name} | Clip: " +
                $"{(damageSound.clip != null ? damageSound.clip.name : "НЕ НАЗНАЧЕН")}",
                damageSound
            );

            damageSound.transform.position = entity.transform.position;
            damageSound.Play();
        }
    }

}