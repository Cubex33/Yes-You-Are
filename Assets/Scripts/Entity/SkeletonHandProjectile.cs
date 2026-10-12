
using Lucy.Core;
using UnityEngine;

public class SkeletonHandProjectile : MonoBehaviour
{
    private Boss boss;
    private Transform target;

    private float damage;
    private float speed;
    private float hitRadius;
    private float lifetime;
    private float stunDuration;

    private Vector3 direction;
    private float elapsed;
    private bool initialized;
    private bool hasHit;

    public void Initialize(
        Boss source,
        Transform targetTransform,
        float damageAmount,
        float moveSpeed,
        float collisionRadius,
        float maxLifetime,
        float stunTime)
    {
        boss = source;
        target = targetTransform;

        damage = damageAmount;
        speed = Mathf.Max(0.1f, moveSpeed);
        hitRadius = Mathf.Max(0.1f, collisionRadius);
        lifetime = Mathf.Max(0.1f, maxLifetime);
        stunDuration = Mathf.Max(0.1f, stunTime);

        if (target != null)
        {
            direction =
                (target.position + Vector3.up - transform.position)
                .normalized;

            transform.rotation = Quaternion.LookRotation(direction);
        }

        initialized = true;
    }

    private void Update()
    {
        if (!initialized || hasHit || target == null || boss == null)
        {
            Destroy(gameObject);
            return;
        }

        elapsed += Time.deltaTime;

        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        // Mild homing: the hand follows movement without
        // instantly snapping onto the player's position.
        Vector3 aimPoint = target.position + Vector3.up;
        Vector3 desiredDirection =
            (aimPoint - transform.position).normalized;

        direction = Vector3.RotateTowards(
            direction,
            desiredDirection,
            1.5f * Time.deltaTime,
            0f).normalized;

        Vector3 previousPosition = transform.position;
        Vector3 nextPosition =
            previousPosition + direction * speed * Time.deltaTime;

        // Swept collision prevents the hand from passing through
        // the player between frames.
        Vector3 segment = nextPosition - previousPosition;

        float t = segment.sqrMagnitude > 0.00001f
            ? Mathf.Clamp01(Vector3.Dot(
                aimPoint - previousPosition, segment) /
                segment.sqrMagnitude)
            : 0f;

        Vector3 closestPoint = previousPosition + segment * t;

        transform.position = nextPosition;

        if (direction.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(direction);

        if (Vector3.Distance(closestPoint, aimPoint) <= hitRadius)
        {
            HitPlayer();
        }
    }

    private void HitPlayer()
    {
        if (hasHit)
            return;

        hasHit = true;

        Entity playerEntity = target.GetComponentInParent<Entity>();

        if (playerEntity == null)
            playerEntity = target.GetComponent<Entity>();

        if (playerEntity != null)
            playerEntity.TakeDamage(damage, boss);

        boss.ApplySkeletonHandStun(stunDuration);

        Destroy(gameObject);
    }
}