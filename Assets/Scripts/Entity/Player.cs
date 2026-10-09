using Lucy.Core;
using UnityEngine;

public class Player : Entity
{
    public void RespawnAt(Transform point)
    {
        if (point == null) return;

        var cc = GetComponent<CharacterController>();
        var knockdownAttack = GetComponent<KnockdownAttack>();

        cc.enabled = false;
        transform.SetPositionAndRotation(point.position, point.rotation);
        cc.enabled = true;

        currentHealth = maxHealth;
        knockdownAttack.currentStamina = knockdownAttack.maxStamina;
    }

    public override void Die(object source)
    {
        OnDeath?.Invoke(source);
    }
}