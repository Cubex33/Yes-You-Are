using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    [Header("Healing")]
    [SerializeField] private float healAmount = 30f;

    private bool collected;

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;

        // Ищем Player на объекте коллайдера или выше по иерархии.
        Player player = other.GetComponentInParent<Player>();

        if (player == null)
        {
            Debug.Log($"HealthPickup: Player не найден у {other.name}", this);
            return;
        }

        if (player.IsDead || player.Health >= player.MaxHealth)
        {
            Debug.Log("HealthPickup: здоровье полное или игрок мёртв.", this);
            return;
        }

        float oldHealth = player.Health;

        player.Heal(healAmount, this);

        // Удаляем предмет только если здоровье действительно восстановилось.
        if (player.Health > oldHealth)
        {
            collected = true;

            Debug.Log(
                $"Хилка подобрана! HP: {oldHealth} → {player.Health}/{player.MaxHealth}",
                this
            );

            Destroy(gameObject);
        }
    }
}
