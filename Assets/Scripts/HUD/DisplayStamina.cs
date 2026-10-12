using UnityEngine;
using UnityEngine.UI;

public class DisplayStamina : MonoBehaviour
{
    public Slider staminaBar;
    public Image barImgage;
    public KnockdownAttack knockdownAttack;

    private Image staminaBarImage;

    void Start()
    {
        // Проверяем Slider
        if (staminaBar == null)
        {
            Debug.LogError(
                $"[{gameObject.name} DisplayStamina]: Stamina Slider is not assigned."
            );
            return;
        }

        // Получаем Image самого Slider
        staminaBarImage = staminaBar.image;

        // Если отдельный Image не назначен, используем Image Slider
        if (barImgage == null)
        {
            barImgage = staminaBarImage;
        }

        // Ищем игрока, если KnockdownAttack не назначен
        if (knockdownAttack == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                knockdownAttack = player.GetComponent<KnockdownAttack>();
            }
        }

        // Проверяем компонент игрока
        if (knockdownAttack == null)
        {
            Debug.LogError(
                $"[{gameObject.name} DisplayStamina]: No KnockdownAttack component found on player."
            );
            return;
        }

        // Подписываемся на изменение выносливости
        knockdownAttack.OnStaminaChanged += UpdateStaminaDisplay;

        // Устанавливаем начальное значение
        UpdateStaminaDisplay(knockdownAttack.currentStamina);
    }

    void OnDestroy()
    {
        if (knockdownAttack != null)
        {
            knockdownAttack.OnStaminaChanged -= UpdateStaminaDisplay;
        }
    }

    private void UpdateStaminaDisplay(float currentStamina)
    {
        if (staminaBar == null || knockdownAttack == null)
            return;

        if (knockdownAttack.maxStamina <= 0f)
            return;

        // Обновляем заполнение Slider
        staminaBar.value = currentStamina / knockdownAttack.maxStamina;

        // Выбираем цвет индикатора
        Color staminaColor;

        if (knockdownAttack.IsFullStamina && knockdownAttack.isRecovery)
        {
            staminaColor = Color.green;
        }
        else if (knockdownAttack.isRecovery)
        {
            staminaColor = Color.red;
        }
        else if (currentStamina < knockdownAttack.maxStamina * 0.3f)
        {
            staminaColor = Color.yellow;
        }
        else
        {
            staminaColor = Color.white;
        }

        // Меняем цвет только у существующих Image
        if (barImgage != null)
        {
            barImgage.color = staminaColor;
        }

        if (staminaBarImage != null)
        {
            staminaBarImage.color = staminaColor;
        }
    }
}
