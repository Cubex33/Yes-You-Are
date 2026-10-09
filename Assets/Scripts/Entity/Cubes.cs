using UnityEngine;
using Lucy.Core;
using System.Collections;

public class Cubes : Entity
{
    [Header("Death Effects")]
    [SerializeField] private ParticleSystem deathParticles;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private float destroyDelay = 2f;

    private AudioSource audioSource;

    protected override void Awake()
    {
        base.Awake();
        audioSource = GetComponent<AudioSource>();
    }

    protected override void HandleDeath(object source)
    {
        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        if (deathParticles != null)
        {
            deathParticles.Play();
            Destroy(deathParticles.gameObject, deathParticles.main.duration);
        }

        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position);
        }

        float time = 0;
        Vector3 startScale = transform.localScale;

        while (time < 1f)
        {
            time += Time.deltaTime * 2f;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, time);
            yield return null;
        }

        yield return new WaitForSeconds(destroyDelay);

        Destroy(gameObject);
    }
}