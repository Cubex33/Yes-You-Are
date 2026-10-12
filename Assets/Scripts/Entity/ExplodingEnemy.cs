using IDosGames;
using Lucy.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Lucy.Enemy
{
    public class ExplodingEnemy : Entity
    {
        [Header("Death Debris Settings")]
        [Tooltip("List of objects that will scatter when the enemy dies")]
        [SerializeField] private List<GameObject> debrisParts;

        [Tooltip("Explosion force")]
        [SerializeField] private float explosionForce = 10f;

        [Tooltip("Explosion radius")]
        [SerializeField] private float explosionRadius = 2f;

        [Tooltip("Upward force modifier")]
        [SerializeField] private float upwardModifier = 1.5f;

        [Tooltip("Delay before destroying the main object")]
        [SerializeField] private float destroyDelay = 0.1f;

        protected override void HandleDeath(object source)
        {
            ClaimRewardSystem.ClaimCoinReward(10, 0);
            foreach (var part in debrisParts)
            {
                if (part == null) continue;

                part.transform.SetParent(null);

                part.SetActive(true);

                if (part.TryGetComponent<Rigidbody>(out Rigidbody rb))
                {
                    rb.AddExplosionForce(explosionForce, transform.position, explosionRadius, upwardModifier, ForceMode.Impulse );
                    rb.AddTorque(Random.insideUnitSphere * explosionForce, ForceMode.Impulse);
                }
            }

            DisableMainEntity();

            Destroy(gameObject, destroyDelay);
        }

        private void DisableMainEntity()
        {
            if (TryGetComponent<Collider>(out Collider col)) col.enabled = false;
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers) r.enabled = false;
        }
    }
}