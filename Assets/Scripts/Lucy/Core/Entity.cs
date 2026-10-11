// Core layer
// Base object
using UnityEngine;
using System;

namespace Lucy.Core
{
    public class Entity : MonoBehaviour
    {
        [Header("Entity Settings")]
        [Tooltip("The maximum health of the entity")] [SerializeField] protected float maxHealth = 100f;
        [Tooltip("The current health of the entity")] [SerializeField] protected float currentHealth;
        [Tooltip("Is the entity immortal?")] [SerializeField] protected bool immortal = false;

        public float Health => currentHealth;
        public float MaxHealth => maxHealth;
        public bool Immortal => immortal;

        public Action<float, object> OnDamageTaken;
        public Action<float, object> OnHealed;
        public Action<object> OnDeath;

        public bool IsDead => currentHealth <= 0;

        [Header("Debug Settings")]
        [Tooltip("Show the direction the entity is looking")] [SerializeField] private bool showLookDirection = false;
        [Tooltip("The color of the look direction line")] [SerializeField] private Color lookLineColor = Color.cyan;
        [Tooltip("The length of the look direction line")] [SerializeField] private float lookLineLength = 2.0f;
        [Tooltip("The height of the look direction line")] [SerializeField] private float lookLineHeight = 1f;
        protected virtual void Awake()
        {
            currentHealth = maxHealth;
        }

        public virtual void SetMaxHealth(float value) => maxHealth = value;
        public virtual void SetHealth(float value) => currentHealth = Mathf.Clamp(value, 0, maxHealth);
        public virtual void SetImmortal(bool value) => immortal = value;

        public virtual void TakeDamage(float amount, object source = null)
        {
            //Debug.Log($"{gameObject.name} took {amount} IsDead {IsDead}, {Immortal} damage from {source} CurrentHealth {currentHealth}/{maxHealth}");
            if (IsDead || amount <= 0 || Immortal) return;

            GetComponent<DeferredPhysicsActivation>()?.ActivatePhysics();

            currentHealth -= amount;
            currentHealth = Mathf.Max(currentHealth, 0);

            OnDamageTaken?.Invoke(amount, source);

            if (currentHealth <= 0) Die(source);
        }

        public virtual void Heal(float amount, object source = null)
        {
            if (IsDead || amount <= 0) return;

            currentHealth += amount;
            currentHealth = Mathf.Min(currentHealth, maxHealth);

            OnHealed?.Invoke(amount, source);
        }

        public virtual void SetMaxHealth(float newMaxHealth, bool fullHeal)
        {
            maxHealth = newMaxHealth;

            if (fullHeal)
                currentHealth = maxHealth;
            else
                currentHealth = Mathf.Min(currentHealth, maxHealth);
        }


        public virtual void Die(object source = null)
        {
            OnDeath?.Invoke(this);
            HandleDeath(source);
        }

        protected virtual void HandleDeath(object source)
        {
            Destroy(gameObject);
        }

        private void OnDrawGizmos()
        {
            if (!showLookDirection) return;
            Gizmos.color = lookLineColor;
            Vector3 origin = transform.position + Vector3.up * lookLineHeight;
            Vector3 forward = transform.forward * lookLineLength;
            Gizmos.DrawLine(origin, origin + forward);
            Gizmos.DrawSphere(origin + forward, 0.05f);
        }
    }
}