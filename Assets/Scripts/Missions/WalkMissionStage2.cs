using System.Collections.Generic;
using UnityEngine;
using Lucy.Core;
using System.Collections;
using System;
using UnityEngine.Assertions.Must;
using UnityEngine.UI;
using UnityEngine.Events;
using NUnit.Framework.Constraints;
/// <summary>
/// Stage 2: The mission receives a list of GameObjects (triggers) that are inactive in the inspector.
/// On mission start it sets them active, attaches MissionTouchTrigger (if not present) and waits until the player touches each of them.
/// After completion, it deactivates them back and restores any changed/added components.
/// </summary>
[DisallowMultipleComponent]
public class WalkMissionStage2 : BaseMission
{
    [Tooltip("Assign trigger GameObjects (should have Collider with isTrigger ideally). They are expected to be inactive in scene initially.")]
    public List<GameObject> triggerObjects = new List<GameObject>();
    public WalkMissionStage2_Canvas missionStage2_Canvas;
    [Header("Events")]
    public UnityEvent OnCompleted;
    public UnityEvent OnFailed;
    
    [Header("Runtime")]
    public List<MissionTouchTrigger> runtimeTriggers = new List<MissionTouchTrigger>();
    public HashSet<MissionTouchTrigger> touched = new HashSet<MissionTouchTrigger>();
    // Player reference for death detection
    public Entity playerEntity;
    // Keep records so we can fully revert changes on cleanup
    public class ObjectRecord
    {
        public GameObject go;
        public bool originalActive;
        public Collider originalCollider; // if existed before mission start
        public bool originalColliderIsTrigger;
        public bool originalColliderEnabled;
        public Collider addedCollider; // collider added by mission (if any)
        public bool addedColliderFlag => addedCollider != null;
        public MissionTouchTrigger touchComponent; // existing or added
        public bool addedTouchFlag; // true if mission created it
    }
    public readonly List<ObjectRecord> records = new List<ObjectRecord>();
    public override void StartMission()
    {
        if (isActive)
        {
            if (debugEnabled) Debug.Log("Truing to start an already active mission. Ignoring.");
            return;
        }
        base.StartMission();
        runtimeTriggers.Clear();
        touched.Clear();
        records.Clear();
        foreach (var go in triggerObjects)
        {
            if (go == null) continue;
            var rec = new ObjectRecord
            {
                go = go,
                originalActive = go.activeSelf
            };
            if (debugEnabled) Debug.Log($"[WalkMissionStage2] Processing trigger '{go.name}', initial active state: {go.activeSelf}");
            // Activate the object for the mission
            go.SetActive(true);
            // Ensure it has a trigger collider; remember original collider state if present
            var col = go.GetComponent<Collider>();
            if (col == null)
            {
                if (debugEnabled) Debug.LogWarning($"[WalkMissionStage2] Trigger object '{go.name}' has no Collider — mission will add a trigger collider (BoxCollider).");
                var box = go.AddComponent<BoxCollider>();
                box.isTrigger = true;
                rec.addedCollider = box;
            }
            else
            {
                // store original collider state before modifying
                rec.originalCollider = col;
                rec.originalColliderIsTrigger = col.isTrigger;
                rec.originalColliderEnabled = col.enabled;
                // set it to trigger for mission
                col.isTrigger = true;
                col.enabled = true;
            }
            // Ensure MissionTouchTrigger: if not present — add; remember if we added it
            // Use GetComponentInChildren with includeInactive=false to skip destroyed components
            var existingTouch = go.GetComponent<MissionTouchTrigger>();
            if (existingTouch == null || existingTouch.gameObject == null)
            {
                var t = go.AddComponent<MissionTouchTrigger>();
                t.onlyTriggerOnce = true;
                rec.touchComponent = t;
                rec.addedTouchFlag = true;
                if (debugEnabled) Debug.Log($"[WalkMissionStage2] Added new MissionTouchTrigger to '{go.name}'");
            }
            else
            {
                existingTouch.onlyTriggerOnce = true;
                existingTouch.enabled = true;
                rec.touchComponent = existingTouch;
                rec.addedTouchFlag = false;
                if (debugEnabled) Debug.Log($"[WalkMissionStage2] Using existing MissionTouchTrigger on '{go.name}'");
            }
            // Subscribe
            rec.touchComponent.OnTouched += OnTriggerTouched;
            // track runtime triggers
            runtimeTriggers.Add(rec.touchComponent);
            records.Add(rec);
        }
        if (debugEnabled) Debug.Log($"[WalkMissionStage2] Started with {runtimeTriggers.Count} triggers");
        missionStage2_Canvas.canvas.gameObject.SetActive(true);
        missionStage2_Canvas.progressBar.maxValue = runtimeTriggers.Count;
        missionStage2_Canvas.progressBar.value = 0;
        SubscribeAll();
        if (runtimeTriggers.Count == 0) CompleteMission();
        
    }
    /// <summary>
    /// Unsubscribe from all event subscriptions.
    /// </summary>
    public void UnsubscribeAll()
    {
        // Unsubscribe from player death
        if (playerEntity != null) { try { playerEntity.OnDeath -= OnPlayerDeath; } catch { } }
    }
    public void OnPlayerDeath(object arg)
    {
        if (debugEnabled) Debug.Log("[WalkMissionStage2] Player died — resetting mission.");
        // Unsubscribe from events on old player
        var oldEntity = arg as Entity;
        if (oldEntity != null) { try { oldEntity.OnDeath -= OnPlayerDeath; } catch { } }
        if (debugEnabled) Debug.Log($"[WalkMissionStage2] Before cleanup - records count: {records.Count}");
        // Reset the mission (player handles their own respawn)
        ResetMission();
    }
    public void OnTriggerTouched(MissionTouchTrigger trigger)
    {
        if (!isActive) return;
        if (trigger == null) return;
        touched.Add(trigger);
        missionStage2_Canvas.progressBar.value = touched.Count;
        if (debugEnabled) Debug.Log($"[WalkMissionStage2] Trigger touched: {trigger.gameObject.name} ({touched.Count}/{runtimeTriggers.Count})");
        if (touched.Count >= runtimeTriggers.Count) CompleteMission();
    }
    /// <summary>
    /// Cleanup: unsubscribe, remove added components, restore original collider and active state.
    /// Safe to call multiple times.
    /// </summary>
    public void CleanupRestore()
    {
        // Unsubscribe and remove added components, restore original collider state and active state.
        foreach (var rec in records)
        {
            if (rec == null || rec.go == null) continue;
            if (rec.touchComponent != null) { try { rec.touchComponent.OnTouched -= OnTriggerTouched; } catch { } }
            // If we added the MissionTouchTrigger, remove it immediately
            if (rec.addedTouchFlag && rec.touchComponent != null)
            {
                // Always destroy immediately to ensure clean state for next StartMission
#if UNITY_EDITOR
                DestroyImmediate(rec.touchComponent);
#else
                Destroy(rec.touchComponent);
#endif
            }
            // If we added a collider — destroy it
            if (rec.addedCollider != null)
            {
#if UNITY_EDITOR
                DestroyImmediate(rec.addedCollider);
#else
                Destroy(rec.addedCollider);
#endif
            }
            // If original collider existed — restore its isTrigger and enabled state
            if (rec.originalCollider != null)
            {
                try
                {
                    rec.originalCollider.isTrigger = rec.originalColliderIsTrigger;
                    rec.originalCollider.enabled = rec.originalColliderEnabled;
                }
                catch
                {
                    if (debugEnabled) Debug.Log($"[WalkMissionStage2] Restoring original collider state for {rec.go.name}: isTrigger = {rec.originalColliderIsTrigger}, enabled = {rec.originalColliderEnabled}");
                }
            }
            // Restore original active state of the GameObject
            try { rec.go.SetActive(rec.originalActive); } catch { }
        }
        runtimeTriggers.Clear();
        touched.Clear();
        records.Clear();
    }
    // Ensure cleanup happens when mission completes
    protected override void CompleteMission()
    {
        // restore everything before signaling completion
        CleanupRestore();
        UnsubscribeAll();
        missionStage2_Canvas.canvas.gameObject.SetActive(false);
        OnCompleted.Invoke();
        base.CompleteMission();
    }
    protected override void FailMission()
    {
        CleanupRestore();
        UnsubscribeAll();
        OnFailed.Invoke();
        base.FailMission();
    }
    public override void ResetMission()
    {
        if (debugEnabled) Debug.Log("[WalkMissionStage2] ResetMission called - running CleanupRestore");
        CleanupRestore();
        UnsubscribeAll();
        // Force reset isActive so StartMission can run again
        isActive = false;
        base.ResetMission();
        if (debugEnabled) Debug.Log("[WalkMissionStage2] Calling StartMission after cleanup");
        // Re-run the mission to set everything up again
        StartMission();
    }
    private void SubscribeAll()
    {
        if (FindFirstObjectByType<PlayerController>(FindObjectsInactive.Exclude) is PlayerController pc)
        {
            playerEntity = pc.GetComponentInChildren<Entity>();
            if (playerEntity != null) playerEntity.OnDeath += OnPlayerDeath;
        }
        else if (debugEnabled) Debug.LogWarning("[WalkMissionStage2] Could not find PlayerController on mission reset to subscribe for death detection.");
    }
    public void OnDisable()
    {
        // In case mission was disabled mid-way — make sure we unsubscribe and restore
        CleanupRestore();
        UnsubscribeAll();
    }
    public void OnDestroy()
    {
        CleanupRestore();
        UnsubscribeAll();
    }
}
[Serializable]
public struct WalkMissionStage2_Canvas
{
    public Canvas canvas;
    public Slider progressBar;
}