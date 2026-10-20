using System;
using System.Collections.Generic;
using UnityEngine;
using Lucy.Core;
using UnityEngine.Events;

public class KnockdownAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    public float damage = 20f;
    public float knockbackForce = 15f;
    public float attackRange = 5f;

    [Tooltip("Critical hit chance (0-1)")]
    [Range(0f, 1f)]
    public float criticalHitChance = 0.1f;
    public float criticalHitMultiplier = 2f;
    public Camera mainCamera;
    public LayerMask enemyLayer;

    [Header("Special Attack")]
    public float specialRadius = 3f;
    public float specialDamageMultiplier = 1.5f;

    [Header("Stamina Settings (Knockdown specific)")]
    public float maxStamina = 100f;
    public float staminaCostPerAttack = 18f;
    public float staminaRecoveryPerSecond = 12f;
    public float staminaRecoveryExhausted = 6f;

    [Header("Stamina Runtime")]
    public float currentStamina;

    [Header("Debug")]
    public bool debugEnabled;
    public bool drawGizmos = false;
    public bool isRecovery;

    [Header("Events")]
    public UnityEvent OnSuperAttack;

    public Action<float> OnStaminaChanged;
    public Action<GameObject> OnAttack;

    public bool IsFullStamina => currentStamina >= maxStamina;


    void Awake()
    {
        currentStamina = maxStamina;
        isRecovery = false;
    }

    void Update()
    {
        RecoverStamina();
    }


    public void Attack()
    {
        if (mainCamera == null)
            return;

        // Если гравипушка удерживает объект, ЛКМ запускает его.
        // В остальных случаях атака работает по старым правилам.
        PlayerController playerController = GetComponent<PlayerController>();

        if (playerController != null &&
            playerController.gravitation != null &&
            playerController.gravitation.IsGrabbing)
        {
            GameObject launchedObject =
                playerController.gravitation.LaunchHeldObject(
                    mainCamera.transform.forward
                );

            if (launchedObject != null)
                OnAttack?.Invoke(launchedObject);

            return;
        }

        if (isRecovery && currentStamina >= maxStamina)
        {
            SpecialAreaAttack();
        }
        else if (!isRecovery && currentStamina >= staminaCostPerAttack)
        {
            PerformAttack();
        }
    }

    void SpendStamina()
    {
        currentStamina -= staminaCostPerAttack;
        if (currentStamina < 0f) currentStamina = 0f;
        // if (currentStamina <= 0f) isRecovery = true;
        OnStaminaChanged?.Invoke(currentStamina);
    }

    void RecoverStamina()
    {
        if (currentStamina >= maxStamina)
        {
            currentStamina = maxStamina;
            OnStaminaChanged?.Invoke(currentStamina);
            return;
        }

        float recoveryRate = isRecovery ? staminaRecoveryExhausted : staminaRecoveryPerSecond;
        currentStamina += recoveryRate * Time.deltaTime;

        if (currentStamina > maxStamina)
        {
            currentStamina = maxStamina;
        }

        OnStaminaChanged?.Invoke(currentStamina);
    }

    void PerformAttack()
    {
        if (!Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out RaycastHit hit, attackRange, enemyLayer))
        {
            OnAttack?.Invoke(null);
            return;
        }
        GameObject hitObject = hit.collider.gameObject;
        OnAttack?.Invoke(hitObject);
        ApplyHitEffects(hit.collider, mainCamera.transform.forward.normalized, damage);
        SpendStamina();
    }

    void SpecialAreaAttack()
    {
        Vector3 origin = transform.position;
        Collider[] hits = Physics.OverlapSphere(origin, specialRadius, enemyLayer);
        HashSet<Collider> processed = new HashSet<Collider>();
        foreach (var hit in hits)
        {
            if (hit == null || processed.Contains(hit)) continue;
            processed.Add(hit);
            Vector3 direction = (hit.transform.position - origin).normalized;
            if (direction.sqrMagnitude < 0.0001f) direction = mainCamera != null ? mainCamera.transform.forward.normalized : transform.forward.normalized;
            ApplyHitEffects(hit, direction, damage * specialDamageMultiplier);
        }

        OnSuperAttack?.Invoke();
        currentStamina = 0f;
        OnStaminaChanged?.Invoke(currentStamina);
    }

    void ApplyHitEffects(Collider hitCollider, Vector3 knockbackDirection, float finalDamage)
    {
        if (hitCollider == null)
            return;

        Entity entity = hitCollider.GetComponentInParent<Entity>();
        if (entity != null)
        {
            float roll = UnityEngine.Random.Range(0f, 1f);
            float damageToApply = roll <= criticalHitChance ? finalDamage * criticalHitMultiplier : finalDamage;
            entity.TakeDamage(damageToApply, this);
        }

        EnemyBase enemy = hitCollider.GetComponentInParent<EnemyBase>();
        if (enemy != null)
        {
            enemy.ApplyFalling();
        }

        ApplyKnockbackToRagdoll(hitCollider, knockbackDirection);
    }
    void ApplyKnockbackToRagdoll(Collider hitCollider, Vector3 knockbackDirection)
    {
        if (debugEnabled) Debug.Log($"Applying knockback to {hitCollider.name} with direction {knockbackDirection} and force {knockbackForce}");
        if (hitCollider == null) return;
        Entity entity = hitCollider.GetComponentInParent<Entity>();
        if (debugEnabled) Debug.Log($"Entity found: {entity?.name ?? "None"}");
        if (entity != null)
        {
            Transform rootTransform = entity.transform;
            Rigidbody[] allRigidbodies = rootTransform.GetComponentsInChildren<Rigidbody>(true);
            Rigidbody directRb = hitCollider.attachedRigidbody ?? hitCollider.GetComponent<Rigidbody>();
            HashSet<Rigidbody> processedRbs = new();
            if (debugEnabled) Debug.Log($"Total rigidbodies found: {allRigidbodies.Length}, Direct Rigidbody: {(directRb != null ? directRb.name : "None")}");
            if (directRb != null && !directRb.isKinematic)
            {
                directRb.AddForce(knockbackDirection * knockbackForce * 1.5f, ForceMode.Impulse);
                processedRbs.Add(directRb);
            }
            foreach (Rigidbody rb in allRigidbodies)
            {
                if (rb == null || rb.isKinematic || processedRbs.Contains(rb)) continue;
                processedRbs.Add(rb);
                rb.AddForce(knockbackDirection * knockbackForce, ForceMode.Impulse);
            }
        }
        else
        {
            Rigidbody rb = hitCollider.attachedRigidbody;
            Transform current = hitCollider.transform;
            while (rb == null && current.parent != null)
            {
                current = current.parent;
                rb = current.GetComponent<Rigidbody>();
            }
            if (rb != null && !rb.isKinematic) rb.AddForce(knockbackDirection * knockbackForce * 1.5f, ForceMode.Impulse);
            
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
            return;

        Gizmos.color = Color.red;

        if (mainCamera != null)
        {
            Gizmos.DrawLine( mainCamera.transform.position, mainCamera.transform.position + mainCamera.transform.forward * attackRange );
        }

        Gizmos.DrawWireSphere(transform.position, specialRadius);
    }
}