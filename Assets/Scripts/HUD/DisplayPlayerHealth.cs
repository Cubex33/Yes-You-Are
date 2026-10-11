using UnityEngine;
using UnityEngine.UI;
using Lucy.Core;

public class DisplayPlayerHealth : MonoBehaviour
{
    [Header("Player")]
    public Player player;

    [Header("Health Bar")]
    public Slider healthBar;

    private Entity entity;

    private void Start()
    {
        if (healthBar == null)
        {
            Debug.LogError(
                "DisplayPlayerHealth: Health Bar не назначен!"
            );
            return;
        }

        // Если игрок не назначен вручную, ищем его.
        if (player == null)
            player = FindAnyObjectByType<Player>();

        if (player == null)
        {
            Debug.LogError(
                "DisplayPlayerHealth: Player не найден!"
            );
            return;
        }

        // Player наследуется от Entity.
        entity = player;

        // Slider работает в диапазоне 0–1.
        healthBar.minValue = 0f;
        healthBar.maxValue = 1f;
        healthBar.wholeNumbers = false;

        // Подписываемся на изменение здоровья.
        entity.OnDamageTaken += OnPlayerDamaged;
        entity.OnHealed += OnPlayerHealed;

        // Обновляем полоску при запуске.
        UpdateBar();

        Debug.Log(
            $"Player HUD подключён. HP игрока: " +
            $"{entity.Health}/{entity.MaxHealth}"
        );
    }

    private void OnDestroy()
    {
        if (entity == null) return;

        entity.OnDamageTaken -= OnPlayerDamaged;
        entity.OnHealed -= OnPlayerHealed;
    }

    private void OnPlayerDamaged(float damage, object source)
    {
        Debug.Log(
            $"HUD получил урон: {damage}. " +
            $"HP игрока: {entity.Health}/{entity.MaxHealth}"
        );

        UpdateBar();
    }

    private void OnPlayerHealed(float amount, object source)
    {
        Debug.Log(
            $"HUD получил лечение: {amount}. " +
            $"HP игрока: {entity.Health}/{entity.MaxHealth}"
        );

        UpdateBar();
    }

    private void UpdateBar()
    {
        if (entity == null || healthBar == null) return;

        float maxHP = Mathf.Max(1f, entity.MaxHealth);
        float currentHP = Mathf.Clamp(
            entity.Health, 0f, maxHP
        );

        healthBar.value = currentHP / maxHP;
    }
}
