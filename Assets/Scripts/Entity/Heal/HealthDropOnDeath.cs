
using UnityEngine;
using Lucy.Core;

public class HealthDropOnDeath : MonoBehaviour
{
    [Header("Health Drop")]
    [SerializeField] private GameObject healthPickupPrefab;

    [Range(0f, 1f)]
    [SerializeField] private float dropChance = 0.25f;

    [Header("Spawn Position")]
    [SerializeField] private float spawnHeight = 0.5f;

    private Entity entity;
    private bool hasDropped;

    private void Awake()
    {
        entity = GetComponent<Entity>();

        if (entity == null)
        {
            Debug.LogError(
                $"{name}: HealthDropOnDeath requires Entity."
            );
        }
    }

    private void OnEnable()
    {
        if (entity == null)
            entity = GetComponent<Entity>();

        if (entity != null)
            entity.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (entity != null)
            entity.OnDeath -= HandleDeath;
    }

    private void HandleDeath(object source)
    {
        if (hasDropped) return;
        hasDropped = true;

        if (healthPickupPrefab == null)
        {
            Debug.LogWarning(
                $"{name}: Health Pickup Prefab is not assigned."
            );
            return;
        }

        if (Random.value > dropChance) return;

        Vector3 spawnPosition =
            transform.position + Vector3.up * spawnHeight;

        Instantiate(
            healthPickupPrefab,
            spawnPosition,
            Quaternion.identity
        );
    }
}