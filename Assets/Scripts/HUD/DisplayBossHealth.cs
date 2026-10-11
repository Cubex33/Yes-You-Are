using UnityEngine;
using UnityEngine.UI;
using Lucy.Core;

public class DisplayBossHealth : MonoBehaviour
{
    [Header("Boss")]
    public Boss boss;

    [Header("Health Bar")]
    public Slider healthBar;

    private Entity entity;

    private void Start()
    {
        // Проверяем Slider.
        if (healthBar == null)
        {
            Debug.LogError("DisplayBossHealth: Health Bar не назначен!");
            return;
        }

        // Если Boss не назначен вручную, ищем его.
        if (boss == null)
            boss = FindAnyObjectByType<Boss>();

        if (boss == null)
        {
            Debug.LogError("DisplayBossHealth: Boss не найден!");
            return;
        }

        // Boss наследуется от Entity.
        entity = boss;

        // Slider работает в диапазоне 0–1.
        healthBar.minValue = 0f;
        healthBar.maxValue = 1f;
        healthBar.wholeNumbers = false;

        // Подписываемся на изменение HP.
        entity.OnDamageTaken += OnBossDamaged;
        entity.OnHealed += OnBossHealed;

        // Обновляем полоску при запуске.
        UpdateBar();

        Debug.Log(
            $"Boss HUD подключён. HP босса: {entity.Health}/{entity.MaxHealth}"
        );
    }

    private void OnDestroy()
    {
        if (entity == null) return;

        entity.OnDamageTaken -= OnBossDamaged;
        entity.OnHealed -= OnBossHealed;
    }

    private void OnBossDamaged(float damage, object source)
    {
        Debug.Log(
            $"HUD получил событие урона: {damage}. " +
            $"HP босса: {entity.Health}/{entity.MaxHealth}"
        );

        UpdateBar();
    }

    private void OnBossHealed(float amount, object source)
    {
        UpdateBar();
    }

    private void UpdateBar()
    {
        if (entity == null || healthBar == null) return;

        float maxHP = Mathf.Max(1f, entity.MaxHealth);
        float currentHP = Mathf.Clamp(entity.Health, 0f, maxHP);

        healthBar.value = currentHP / maxHP;
    }
}