using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Lucy.Core;

[DisallowMultipleComponent]
public class DestroyItemsMission : BaseMission
{
    [Header("Target Items (existing scene GameObjects to destroy)")]
    [Tooltip("Existing scene objects (active or inactive). Mission remembers their state, waits for player to destroy ALL of them. On reset, restores to original state.")]
    public List<GameObject> targetItems = new List<GameObject>();

    [Header("UI elements to update")]
    public DestroyItems_Canvas missionCanvas;

    // Runtime state
    private List<Vector3> originalPositions = new List<Vector3>();
    private List<Quaternion> originalRotations = new List<Quaternion>();
    private List<bool> originalActiveStates = new List<bool>();
    private List<Entity> targetEntities = new List<Entity>();

    private Entity playerEntity;
    private int destroyedCount = 0;

    public override void StartMission()
    {
        if (isActive) return;

        FindPlayerSubscribe();

// Capture original state FIRST
        originalPositions.Clear();
        originalRotations.Clear();
        originalActiveStates.Clear();
        targetEntities.Clear();

        for (int i = 0; i < targetItems.Count; i++)
        {
            var item = targetItems[i];
            if (item == null) 
            {
                originalPositions.Add(Vector3.zero);
                originalRotations.Add(Quaternion.identity);
                originalActiveStates.Add(false);
                targetEntities.Add(null);
                continue;
            }
            originalPositions.Add(item.transform.position);
            originalRotations.Add(item.transform.rotation);
            originalActiveStates.Add(item.activeSelf);
            var entity = item.GetComponentInChildren<Entity>();
            targetEntities.Add(entity);
            
            // Subscribe early to catch any immediate deaths
            if (entity != null)
            {
                entity.OnDeath += OnTargetDestroyed;
            }
        }

        // NOW restore ALL to original state (incl. possibly inactive) + full health
        for (int i = 0; i < targetItems.Count; i++)
        {
            var item = targetItems[i];
            if (item == null) continue;
            item.transform.position = originalPositions[i];
            item.transform.rotation = originalRotations[i];
            item.SetActive(originalActiveStates[i]);

            var entity = targetEntities[i];
            if (entity != null)
            {
                entity.SetMaxHealth(entity.MaxHealth, true);  // Full heal to original
            }
        }

        destroyedCount = 0;
        missionCanvas.canvas.gameObject.SetActive(true);
        missionCanvas.progressBar.maxValue = targetItems.Count;
        missionCanvas.progressBar.value = 0;

        base.StartMission();

        CheckProgress();
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

    private void UnsubscribeTargets()
    {
        foreach (var entity in targetEntities)
        {
            if (entity != null)
            {
                try 
                {
                    entity.OnDeath -= OnTargetDestroyed;
                }
                catch { }
            }
        }
    }

    private void OnPlayerDeath(object arg)
    {
        ResetMission();
    }

    private void OnTargetDestroyed(object arg)
    {
        destroyedCount++;
        missionCanvas.progressBar.value = destroyedCount;
        Debug.Log($"[DestroyItemsMission] Item destroyed. Progress: {destroyedCount}/{targetItems.Count}");

        CheckProgress();
    }

    private void CheckProgress()
    {
        if (destroyedCount >= targetItems.Count)
        {
            CompleteMission();
        }
    }

    public override void ResetMission()
    {
        UnsubscribePlayer();
        UnsubscribeTargets();
        missionCanvas.canvas.gameObject.SetActive(false);
        ResetItems();
        base.ResetMission();
        FindPlayerSubscribe();
    }

    private void ResetItems()
    {
        for (int i = 0; i < targetItems.Count; i++)
        {
            var item = targetItems[i];
            if (item != null)
            {
                item.transform.position = originalPositions[i];
                item.transform.rotation = originalRotations[i];
                item.SetActive(originalActiveStates[i]);

                var entity = targetEntities[i];
                if (entity != null)
                {
                    entity.SetMaxHealth(entity.MaxHealth, true);
                }
            }
        }
    }

    protected override void CompleteMission()
    {
        UnsubscribePlayer();
        UnsubscribeTargets();
        missionCanvas.canvas.gameObject.SetActive(false);
        base.CompleteMission();
    }

    protected override void FailMission()
    {
        UnsubscribePlayer();
        UnsubscribeTargets();
        missionCanvas.canvas.gameObject.SetActive(false);
        base.FailMission();
    }

    private void OnDisable()
    {
        UnsubscribePlayer();
        UnsubscribeTargets();
        if (missionCanvas.canvas != null)
            missionCanvas.canvas.gameObject.SetActive(false);
    }
}

[Serializable]
public struct DestroyItems_Canvas
{
    public Canvas canvas;
    public Slider progressBar;
}
