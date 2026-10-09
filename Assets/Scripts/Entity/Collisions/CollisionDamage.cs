using Lucy.Core;
using UnityEngine;

public class CollisionDamage : MonoBehaviour
{
    public float damageThreshold = 5f;
    public float damageMultiplier = 1f;
    [Header("Debug")]
    public bool debugEnabled;
    public Entity entity;
    public EnemyBase enemyBase;
    public bool monoCollider;

    void Awake()
    {
        entity ??= GetComponent<Entity>();
        enemyBase ??= GetComponent<EnemyBase>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (monoCollider) HandleCollision(collision, transform);      
    }

    public void HandleCollision(Collision collision, Transform bone)
    {        
        float collisionForce = collision.relativeVelocity.magnitude;
        if (collisionForce > damageThreshold)
        {
            float damage = (collisionForce - damageThreshold) * damageMultiplier;
            if (entity != null) entity.TakeDamage(damage);
            if (enemyBase != null) enemyBase.ApplyStun();
            if (debugEnabled) Debug.Log($"Bone {bone.name} caused with {collision.gameObject.name} - {damage} damage (force {collisionForce})");
        }
    }
}