using System.Collections;
using System.Collections.Generic;
using Lucy.Core;
using UnityEngine;

/// <summary>Self-contained, one-shot ground slam. Spawn it at the ground impact point.</summary>
public class InfernoGroundSlam : MonoBehaviour
{
    [Header("Combat")]
    public Transform targetPlayer;
    public Entity attacker;
    [Min(0)] public float damage = 40f;
    [Min(.1f)] public float radius = 5.5f;
    [Min(0)] public float impactDelay = .15f;
    [Min(0)] public float knockbackImpulse = 9f;
    [Min(0)] public float upwardsImpulse = 2.5f;
    public bool onlyDamagePlayer = true;
    [Header("Visuals")]
    public Transform ring;
    public Transform dome;
    public Transform groundGlow;
    public ParticleSystem sparks;
    public ParticleSystem blast;
    public ParticleSystem smoke;
    public Light flashLight;
    [Min(.1f)] public float expansionTime = .55f;
    [Min(.2f)] public float effectDuration = 2.5f;

    private bool started;
    private readonly HashSet<Entity> hitEntities = new HashSet<Entity>();
    private Vector3 ringBase, domeBase, glowBase;
    private float initialLight;

    public void Configure(Transform player, Entity source, float amount, float range)
    {
        targetPlayer = player;
        attacker = source;
        damage = amount;
        radius = range;
    }

    private void Start()
    {
        if (started) return;
        started = true;
        ringBase = ring ? ring.localScale : Vector3.one;
        domeBase = dome ? dome.localScale : Vector3.one;
        glowBase = groundGlow ? groundGlow.localScale : Vector3.one;
        initialLight = flashLight ? flashLight.intensity : 0f;
        if (sparks) sparks.Play(true);
        if (smoke) smoke.Play(true);
        StartCoroutine(EffectRoutine());
    }

    private IEnumerator EffectRoutine()
    {
        float time = 0f;
        bool impacted = false;
        while (time < effectDuration)
        {
            time += Time.deltaTime;
            if (!impacted && time >= impactDelay)
            {
                impacted = true;
                if (blast) blast.Play(true);
                DealImpactDamage();
            }
            float t = Mathf.Clamp01((time - impactDelay) / Mathf.Max(.05f, expansionTime));
            float smooth = 1f - Mathf.Pow(1f - t, 3f);
            if (ring) ring.localScale = new Vector3(radius * 2f * smooth, ringBase.y, radius * 2f * smooth);
            if (dome) dome.localScale = new Vector3(radius * 2f * smooth, radius * .9f * smooth, radius * 2f * smooth);
            if (groundGlow) groundGlow.localScale = new Vector3(radius * 1.5f * smooth, glowBase.y, radius * 1.5f * smooth);
            if (flashLight) flashLight.intensity = initialLight * Mathf.Clamp01(1f - time / .35f);
            yield return null;
        }
        Destroy(gameObject);
    }

    private void DealImpactDamage()
    {
        hitEntities.Clear();
        if (onlyDamagePlayer)
        {
            if (!targetPlayer) return;
            Entity victim = targetPlayer.GetComponentInParent<Entity>();
            if (!victim) victim = targetPlayer.GetComponentInChildren<Entity>();
            if (!victim || victim == attacker) return;
            // Horizontal AoE, with a generous vertical tolerance for uneven ground.
            Vector3 delta = targetPlayer.position - transform.position;
            if (new Vector2(delta.x, delta.z).magnitude > radius || Mathf.Abs(delta.y) > 3.5f) return;
            ApplyDamage(victim, targetPlayer.GetComponentInParent<Rigidbody>());
            return;
        }
        foreach (Collider c in Physics.OverlapSphere(transform.position, radius, ~0, QueryTriggerInteraction.Ignore))
        {
            Entity victim = c.GetComponentInParent<Entity>();
            if (!victim || victim == attacker || !hitEntities.Add(victim)) continue;
            ApplyDamage(victim, c.attachedRigidbody);
        }
    }

    private void ApplyDamage(Entity victim, Rigidbody body)
    {
        // Lucy.Core method signature as used by KnockdownAttack.
        victim.TakeDamage(damage, attacker != null ? (MonoBehaviour)attacker : this);
        if (body && !body.isKinematic)
        {
            Vector3 push = body.worldCenterOfMass - transform.position;
            push.y = 0;
            push = (push.sqrMagnitude > .01f ? push.normalized : transform.forward) * knockbackImpulse + Vector3.up * upwardsImpulse;
            body.AddForce(push, ForceMode.Impulse);
        }
    }
}
