using System;
using System.Collections.Generic;
using UnityEngine;
using Lucy.Core;
using UnityEngine.UI;
using System.Collections;

public class KillEmAllMission : BaseMission
{
    public int scorePerKill = 10;
    public float finishDelay = 5f;
    public KillEmAll_Canvas killEmAllCanvas;
    private List<Entity> entities = new List<Entity>();
    private Dictionary<Entity, Vector3> originalPositions = new Dictionary<Entity, Vector3>();
    private Dictionary<Entity, Quaternion> originalRotations = new Dictionary<Entity, Quaternion>();

    // Player reference for death detection
    private Entity playerEntity;

    public override void StartMission()
    {
        base.StartMission();

        Entity[] foundEntities = FindObjectsByType<Entity>(FindObjectsSortMode.None);
        entities.Clear();
        
        foreach (var entity in foundEntities)
        {
            if (entity.gameObject.tag == "Player") continue; 
            entities.Add(entity);
        }

        foreach (var entity in entities)
        {
            originalPositions[entity] = entity.transform.position;
            originalRotations[entity] = entity.transform.rotation;

            entity.OnDeath += OnEntityDeath;
        }
        killEmAllCanvas.progressBar.maxValue = entities.Count;
        killEmAllCanvas.progressBar.value = 0;

        // Subscribe to player death to reset mission
        var pc = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Exclude);
        if (pc != null)
        {
            playerEntity = pc.GetComponentInChildren<Entity>();
            if (playerEntity != null)
            {
                playerEntity.OnDeath += OnPlayerDeath;
            }
        }

        Debug.Log($"Mission started: {missionName}, {entities.Count} entities to kill.");
    }

    /// <summary>
    /// Unsubscribe from all event subscriptions.
    /// </summary>
    private void UnsubscribeAll()
    {
        // Unsubscribe from all entity deaths
        foreach (var entity in entities)
        {
            if (entity != null)
            {
                try { entity.OnDeath -= OnEntityDeath; } catch { }
            }
        }

        // Unsubscribe from player death
        if (playerEntity != null)
        {
            try { playerEntity.OnDeath -= OnPlayerDeath; } catch { }
        }
    }

    private void OnPlayerDeath(object arg)
    {
        Debug.Log("[KillEmAllMission] Player died — resetting mission.");

        // Unsubscribe from events on old player
        var oldEntity = arg as Entity;
        if (oldEntity != null)
        {
            try { oldEntity.OnDeath -= OnPlayerDeath; } catch { }
        }

        // Reset the mission (player handles their own respawn)
        ResetMission();
    }

    private void OnEntityDeath(object source)
    {
        Entity deadEntity = source as Entity;

        if (deadEntity != null)
        {
            ScoreSystem.Add(scorePerKill);
            deadEntity.OnDeath -= OnEntityDeath;
            entities.Remove(deadEntity);
            killEmAllCanvas.progressBar.value = killEmAllCanvas.progressBar.maxValue - entities.Count;

            Debug.Log($"Entity killed: {deadEntity.name}, {entities.Count} remaining.");

            if (entities.Count == 0)
            {
                Debug.Log("All entities killed!");
                StartCoroutine(DelayedCompleteMission());
            }
        }
    }

    protected override void CompleteMission()
    {
        UnsubscribeAll();
        base.CompleteMission();
        CleanupEntities();
    }

    protected override void FailMission()
    {
        UnsubscribeAll();
        base.FailMission();
        RestoreEntities();
    }

    public override void ResetMission()
    {
        UnsubscribeAll();
        base.ResetMission();
        
        // Clear entity tracking
        entities.Clear();
        originalPositions.Clear();
        originalRotations.Clear();
        
        // Re-subscribe to player death for the new player instance
        var pc = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Exclude);
        if (pc != null)
        {
            playerEntity = pc.GetComponentInChildren<Entity>();
            if (playerEntity != null)
            {
                playerEntity.OnDeath += OnPlayerDeath;
            }
        }
        
        // Restart the mission
        StartMission();
    }

    private void CleanupEntities()
    {
        foreach (var entity in entities)
        {
            if (entity != null)
            {
                entity.OnDeath -= OnEntityDeath;
                Destroy(entity.gameObject);
            }
        }
        entities.Clear();
    }

    private void RestoreEntities()
    {
        foreach (var kvp in originalPositions)
        {
            Entity entity = kvp.Key;
            if (entity != null)
            {
                entity.OnDeath -= OnEntityDeath;

                entity.transform.position = kvp.Value;
                entity.transform.rotation = originalRotations[entity];

                entity.SetHealth(entity.MaxHealth);
            }
        }

        entities.Clear();
    }

    void Reset()
    {
        missionName = "Kill Em All";
        missionDescription = "You must destroy all the enemies on the map to complete the mission.";
    }

    private void OnDisable()
    {
        UnsubscribeAll();
    }

    private IEnumerator DelayedCompleteMission()
    {
        yield return new WaitForSeconds(finishDelay);
        CompleteMission();
    }
}


[Serializable]
public struct KillEmAll_Canvas
{
    public Canvas canvas;
    public Slider progressBar;
}