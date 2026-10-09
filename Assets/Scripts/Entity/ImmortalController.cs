using UnityEngine;
using System.Collections.Generic;
using Lucy.Core;

[RequireComponent(typeof(Entity))]
public class ImmortalController : MonoBehaviour
{
    [Header("Immortality Links")]
    [Tooltip("This entity's immortality is controlled by these links. Immortal while any alive.")]
    public List<Entity> linkedEntities = new List<Entity>();

    private Entity targetEntity;

    private void Awake()
    {
        targetEntity = GetComponent<Entity>();
        if (targetEntity == null)
        {
            Debug.LogError($"{gameObject.name} ImmortalController requires Entity component.", this);
            enabled = false;
            return;
        }
        SubscribeToLinks();
        UpdateImmortalState();
    }

    private void OnValidate()
    {
        UnsubscribeFromLinks();
        SubscribeToLinks();
        UpdateImmortalState();
    }

    private void OnDestroy()
    {
        UnsubscribeFromLinks();
    }

    private void SubscribeToLinks()
    {
        foreach (var link in linkedEntities)
        {
            if (link != null)
            {
                link.OnDeath += OnLinkDied;
            }
        }
    }

    private void UnsubscribeFromLinks()
    {
        foreach (var link in linkedEntities)
        {
            if (link != null)
            {
                link.OnDeath -= OnLinkDied;
            }
        }
    }

    private void OnLinkDied(object deadEntity)
    {
        UpdateImmortalState();
    }

    private void UpdateImmortalState()
    {
        bool hasAliveLinks = false;
        foreach (var link in linkedEntities)
        {
            if (link != null && !link.IsDead)
            {
                hasAliveLinks = true;
                break;
            }
        }
        targetEntity.SetImmortal(hasAliveLinks);
    }
}
