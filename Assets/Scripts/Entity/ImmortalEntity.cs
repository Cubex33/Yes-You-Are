using Lucy.Core;
using System.Collections.Generic;
using UnityEngine;

public class ImmortalEntity : Entity
{
    [Header("Immortality Links")]
    [Tooltip("Immortal while any linked entity is alive. Subscribes to their OnDeath events.")]
    public List<Entity> linkedEntities = new List<Entity>();

    protected override void Awake()
    {
        base.Awake();
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
        SetImmortal(hasAliveLinks);
    }
}
