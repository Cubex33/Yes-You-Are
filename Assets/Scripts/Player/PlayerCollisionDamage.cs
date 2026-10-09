using Lucy.Core;
using UnityEngine;

public class PlayerCollisionDamage : MonoBehaviour
{
    public float damageThreshold = 5f;
    public float damageMultiplier = 1f;
    [Header("Debug")]
    public bool debugEnabled = false;
    private CharacterController controller;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        float speed = controller.velocity.magnitude;

        if (speed < damageThreshold)
            return;

        float damage = (speed - damageThreshold) * damageMultiplier;

        var entity = hit.collider.GetComponent<Entity>();
        if (entity != null)
            entity.TakeDamage(damage);

        var enemyBase = hit.collider.GetComponent<EnemyBase>();
        if (enemyBase != null)
            enemyBase.ApplyStun();

        if (debugEnabled) Debug.Log($"Hit {hit.collider.name} for {damage} damage, speed={speed}");
    }
}