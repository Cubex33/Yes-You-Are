using UnityEngine;
using Lucy.Core;

// Real travelling projectile: swept collision, one hit, impact explosion and lifetime.
public class FireballProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 17f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private float collisionRadius = 0.32f;
    [SerializeField] private float explosionRadius = 2.0f;
    [SerializeField] private float explosionDamageMultiplier = 0.55f;
    [SerializeField] private LayerMask collisionMask = ~0;
    [SerializeField] private ParticleSystem impactPrefab;
    private Vector3 direction;
    private Transform target;
    private Entity owner;
    private float damage;
    private float tracking;
    private bool impacted;
    private float born;

    public void Initialize(Entity shooter, Transform aimTarget, Vector3 initialDirection, float amount,
        float projectileSpeed, ParticleSystem impact, float homing = 0f)
    {
        owner = shooter;
        target = aimTarget;
        direction = initialDirection.sqrMagnitude > 0.0001f ? initialDirection.normalized : transform.forward;
        damage = amount;
        speed = projectileSpeed;
        impactPrefab = impact;
        tracking = homing;
        born = Time.time;
        transform.rotation = Quaternion.LookRotation(direction);
    }

    private void Awake() { born = Time.time; }
    private void Start()
    {
        foreach (ParticleSystem ps in GetComponentsInChildren<ParticleSystem>(true)) ps.Play(true);
    }

    private void Update()
    {
        if (impacted) return;
        if (Time.time - born >= lifetime) { Explode(transform.position, null); return; }
        if (target != null && tracking > 0f)
        {
            Vector3 toward = (target.position + Vector3.up - transform.position).normalized;
            if (toward.sqrMagnitude > 0.001f)
                direction = Vector3.RotateTowards(direction, toward, tracking * Mathf.Deg2Rad * Time.deltaTime, 0f).normalized;
        }
        float step = speed * Time.deltaTime;
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, collisionRadius, direction, step, collisionMask, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (owner != null && (hit.collider.transform == owner.transform || hit.collider.transform.IsChildOf(owner.transform))) continue;
            Explode(hit.point, hit.collider);
            return;
        }
        transform.position += direction * step;
        if (direction.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(direction);
    }

    private void Explode(Vector3 point, Collider directCollider)
    {
        if (impacted) return;
        impacted = true;
        if (impactPrefab != null)
        {
            ParticleSystem fx = Instantiate(impactPrefab, point, Quaternion.identity);
            fx.Play(true);
            Destroy(fx.gameObject, 5f);
        }
        System.Collections.Generic.HashSet<Entity> damaged = new System.Collections.Generic.HashSet<Entity>();
        if (directCollider != null) DamageEntity(directCollider.GetComponentInParent<Entity>(), damage, damaged);
        Collider[] near = Physics.OverlapSphere(point, explosionRadius, collisionMask, QueryTriggerInteraction.Ignore);
        foreach (Collider col in near)
            DamageEntity(col.GetComponentInParent<Entity>(), damage * explosionDamageMultiplier, damaged);
        Destroy(gameObject);
    }

    private void DamageEntity(Entity victim, float amount, System.Collections.Generic.HashSet<Entity> damaged)
    {
        if (victim == null || victim == owner || !damaged.Add(victim)) return;
        // Don't damage summoned skeletons; restrict projectile damage to the designated target.
        if (target != null && victim.transform != target && !target.IsChildOf(victim.transform) && !victim.transform.IsChildOf(target)) return;
        victim.TakeDamage(amount, owner);
    }
}
