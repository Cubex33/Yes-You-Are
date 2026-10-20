
using System.Collections.Generic;
using Lucy.Core;
using UnityEngine;

/// <summary>
/// Обрабатывает урон от предмета, запущенного гравипушкой.
/// Компонент автоматически добавляется при запуске предмета.
/// </summary>
public class ThrownObjectDamage : MonoBehaviour
{
    [Header("Impact Damage")]
    [Min(0f)]
    public float minimumImpactSpeed = 3f;

    [Min(0f)]
    public float damagePerSpeed = 4f;

    [Header("Enemy Reaction")]
    [Min(0f)]
    public float fallingSpeedThreshold = 8f;

    [Min(0f)]
    public float knockbackForce = 5f;

    private readonly HashSet<Entity> hitEntities = new HashSet<Entity>();

    private Rigidbody projectileBody;
    private bool isArmed;
    public bool IsArmed => isArmed;

    private void Awake()
    {
        projectileBody = GetComponent<Rigidbody>();
    }

    public void Arm()
    {
        if (projectileBody == null)
            projectileBody = GetComponent<Rigidbody>();

        hitEntities.Clear();
        isArmed = projectileBody != null;
    }

    public void Disarm()
    {
        isArmed = false;
        hitEntities.Clear();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isArmed || collision == null)
            return;

        if (collision.collider == null)
            return;

        Entity targetEntity =
            collision.collider.GetComponentInParent<Entity>();

        if (targetEntity == null || targetEntity.IsDead)
            return;

        // Не наносим урон самому себе.
        Entity ownEntity = GetComponentInParent<Entity>();

        if (ownEntity != null && ownEntity == targetEntity)
            return;

        // Одна цель получает урон не более одного раза за запуск предмета.
        if (hitEntities.Contains(targetEntity))
            return;

        float impactSpeed = collision.relativeVelocity.magnitude;

        if (impactSpeed < minimumImpactSpeed)
            return;

        hitEntities.Add(targetEntity);

        float damage = (impactSpeed - minimumImpactSpeed) * damagePerSpeed;

        if (damage > 0f)
            targetEntity.TakeDamage(damage, this);

        EnemyBase enemy = collision.collider.GetComponentInParent<EnemyBase>();

        if (enemy == null || impactSpeed < fallingSpeedThreshold)
            return;

        enemy.ApplyFalling();

        // Добавляем импульс только после включения физики врага.
        Rigidbody enemyBody = enemy.GetComponent<Rigidbody>();

        if (enemyBody != null && !enemyBody.isKinematic)
        {
            Vector3 direction = projectileBody != null
                ? projectileBody.linearVelocity.normalized
                : -collision.GetContact(0).normal;

            if (direction.sqrMagnitude > 0.001f)
                enemyBody.AddForce(direction * knockbackForce, ForceMode.Impulse);
        }
    }
}
