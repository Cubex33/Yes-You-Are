
using UnityEngine;
using Lucy.Core;

public class HealthPickup : MonoBehaviour
{
    [Header("Healing")]
    [SerializeField] private float healAmount = 30f;

    private bool collected;

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;

        Entity entity = other.GetComponentInParent<Entity>();

        if (entity == null) return;

        // Подбирает только игрок с тегом Player.
        if (!entity.CompareTag("Player")) return;

        // Не расходуем хилку, если здоровье уже полное.
        if (entity.IsDead || entity.Health >= entity.MaxHealth)
            return;

        collected = true;

        entity.Heal(healAmount, this);

        Destroy(gameObject);
    }
}