using UnityEngine;
using UnityEngine.UI;

public class DisplayStamina : MonoBehaviour
{
    public Slider staminaBar;
    public Image barImgage;
    public KnockdownAttack knockdownAttack;

    void Start()
    {
        if (staminaBar == null)
        {
            Debug.LogError("DisplayStamina: No TextMeshPro text component assigned, stamina will not be displayed.");
            return;
        }

        if (knockdownAttack == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                knockdownAttack = player.GetComponent<KnockdownAttack>();
            }
        }

        if (knockdownAttack == null)
        {
            Debug.LogError($"[{gameObject.name} DisplayStamina]: No KnockdownAttack component found on player.");
            return;
        }

        knockdownAttack.OnStaminaChanged += UpdateStaminaDisplay;
        
        UpdateStaminaDisplay(knockdownAttack.currentStamina);
    }

    void OnDestroy()
    {
        if (knockdownAttack != null) knockdownAttack.OnStaminaChanged -= UpdateStaminaDisplay;
    }

    private void UpdateStaminaDisplay(float currentStamina)
    {
        if (staminaBar == null) return;
        staminaBar.value = currentStamina / knockdownAttack.maxStamina;
        if (knockdownAttack.IsFullStamina && knockdownAttack.isRecovery) barImgage.color = staminaBar.image.color = Color.green;
        else if (knockdownAttack.isRecovery) barImgage.color = staminaBar.image.color = Color.red;
        else if (currentStamina < knockdownAttack.maxStamina * 0.3f) barImgage.color = staminaBar.image.color = Color.yellow;
        else barImgage.color = staminaBar.image.color = Color.white;
    }
}

