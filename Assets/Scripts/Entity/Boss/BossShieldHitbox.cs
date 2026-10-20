
using UnityEngine;

/// <summary>
/// Определяет попадание запущенного гравимагией предмета в щит босса.
/// </summary>
public class BossShieldHitbox : MonoBehaviour
{
    private Boss owner;

    public void Initialize(Boss boss)
    {
        owner = boss;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (owner == null || other == null)
            return;

        ThrownObjectDamage projectile =
            other.GetComponentInParent<ThrownObjectDamage>();

        if (projectile == null || !projectile.IsArmed)
            return;

        owner.TryBreakShield(projectile);
    }
}
