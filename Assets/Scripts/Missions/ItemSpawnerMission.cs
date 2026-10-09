using System;
using System.Collections.Generic;
using UnityEngine;
using Lucy.Core;

[DisallowMultipleComponent]
public class ItemSpawnerMission : BaseMission
{
    [Header("Templates (inactive scene GameObjects)")]
    [Tooltip("Items to activate on start/reset, LEAVE ACTIVE after complete (do not disappear)")]
    public List<GameObject> nonDisappearTemplates = new List<GameObject>();

    [Tooltip("Templates to instantiate clones from on start/reset, DESTROY CLONES after complete (disappear)")]
    public List<GameObject> disappearTemplates = new List<GameObject>();

    // Runtime
    private List<GameObject> activeNonDisappear = new List<GameObject>();
    private List<GameObject> activeDisappearClones = new List<GameObject>();

    private Entity playerEntity;

    public override void StartMission()
    {
        if (isActive) return;

        FindPlayerSubscribe();
        SpawnAll();
        base.StartMission();
    }

    private void FindPlayerSubscribe()
    {
        var pc = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Exclude);
        playerEntity = pc?.GetComponentInChildren<Entity>();
        if (playerEntity != null)
        {
            playerEntity.OnDeath += OnPlayerDeath;
        }
    }

    private void UnsubscribePlayer()
    {
        if (playerEntity != null)
        {
            try 
            {
                playerEntity.OnDeath -= OnPlayerDeath;
            }
            catch { }
            playerEntity = null;
        }
    }

    private void UnsubscribeAll()
    {
        UnsubscribePlayer();
    }

    private void OnPlayerDeath(object arg)
    {
        ResetMission();
    }

    protected virtual void OnItemsSpawned(List<GameObject> nonDisappear, List<GameObject> disappearClones)
    {
        // Override in subclasses to subscribe to item events (deaths, touches, etc.)
    }

    private void SpawnAll()
    {
        // Non-disappear: activate existing
        activeNonDisappear.Clear();
        foreach (var template in nonDisappearTemplates)
        {
            if (template != null)
            {
                template.SetActive(true);
                activeNonDisappear.Add(template);
            }
        }

        // Disappear: instantiate clones
        activeDisappearClones.Clear();
        foreach (var template in disappearTemplates)
        {
            if (template != null)
            {
                GameObject clone = Instantiate(template, template.transform.position, template.transform.rotation);
                clone.SetActive(true);
                activeDisappearClones.Add(clone);
            }
        }

        OnItemsSpawned(activeNonDisappear, activeDisappearClones);
    }

    private void CleanupAll()
    {
        DeactivateNonDisappear();
        DestroyDisappearClones();
    }

    private void CleanupDisappearOnly()
    {
        DestroyDisappearClones();
    }

    private void DeactivateNonDisappear()
    {
        foreach (var item in activeNonDisappear)
        {
            if (item != null)
            {
                item.SetActive(false);
            }
        }
        activeNonDisappear.Clear();
    }

    private void DestroyDisappearClones()
    {
        foreach (var clone in activeDisappearClones)
        {
            if (clone != null)
            {
                Destroy(clone);
            }
        }
        activeDisappearClones.Clear();
    }

    public override void ResetMission()
    {
        UnsubscribePlayer();
        CleanupAll();
        base.ResetMission();
        FindPlayerSubscribe();
        SpawnAll();
    }

    protected override void CompleteMission()
    {
        CleanupDisappearOnly();  // Destroy clones, but leave nonDisappear active
        UnsubscribeAll();
        base.CompleteMission();
    }

    protected override void FailMission()
    {
        CleanupAll();
        UnsubscribeAll();
        base.FailMission();
    }

    private void OnDisable()
    {
        CleanupAll();
        UnsubscribeAll();
    }
}

