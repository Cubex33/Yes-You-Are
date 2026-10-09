using System;
using System.Collections.Generic;
using UnityEngine;
using Lucy.Core;
using Lucy.Game;
using System.Collections;
using UnityEngine.UI;

/// Mission that requires a player to grab objects a certain number of times and hit other objects a certain number of times.
/// Uses object templates (inactive scene GameObjects) and instantiates runtime clones. On player death, clones are destroyed and fresh ones are spawned from templates.
[DisallowMultipleComponent]
public class PlayerGrabAndHitMission : BaseMission
{
    /// The number of times the player must grab an object.
    [Tooltip("How many times the player must grab an object")]
    public int requiredGrabs = 9;

    /// The number of times the player must hit an object.
    [Tooltip("How many times the player must hit an object")]
    public int requiredHits = 9;

    /// Optional reference to the player controller.
    [Header("Reference to the player controller")]
    [Tooltip("If null, the first player controller in the scene will be found")]
    public PlayerController playerController;
    [Header("UI Elements (optional)")]
    [Tooltip("Optional UI elements to display the current grab and hit count. If not assigned, the mission will attempt to find them by name. If not found, they will not be displayed.")]
    public PlayerGrabAndHitMission_Canvas playerGrabAndHitMission_Canvas;

    /// If true, the mission will always try to find the necessary components on the player controller, even if playerController is null.
    [Header("Options")]
    [Tooltip("If true, the mission will always try to find the necessary components on the player controller, even if playerController is null")]
    public bool alwaysFindComponents = false;

    [Header("Item Templates (inactive in scene)")]
    [Tooltip("Object templates (inactive scene GameObjects). They will be instantiated as runtime clones. On player death, clones are destroyed and fresh ones are spawned from templates.")]
    public List<GameObject> itemTemplates = new();

    // Internal counters
    private int grabCount;
    private int hitCount;

    // Runtime objects (clones)
    private List<GameObject> activeItems = new();

    // Player reference for death detection
    private Entity playerEntity;

    public override void StartMission()
    {
        if (isActive) return;
        base.StartMission();

        grabCount = 0;
        hitCount = 0;
        playerGrabAndHitMission_Canvas.canvas.gameObject.SetActive(true);
        playerGrabAndHitMission_Canvas.grabsSlider.slider.maxValue = requiredGrabs;
        playerGrabAndHitMission_Canvas.hitsSlider.slider.maxValue = requiredHits;

        // Try to find the player controller if not assigned
        if (playerController == null)
        { 
            playerController = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Exclude);
        }

        // If we found a player controller, subscribe to its events
        if (playerController != null)
        {
            SubscribeToPlayerEvents();
            
            // Subscribe to player death to reset mission
            playerEntity = playerController.GetComponentInChildren<Entity>();
            if (playerEntity != null)
            {
                playerEntity.OnDeath += OnPlayerDeath;
            }
        }
        else
        {
            Debug.LogWarning("[PlayerGrabAndHitMission] PlayerController не найден в сцене. События не будут подписаны.");
        }

        // --- Spawn items from templates ---
        SpawnItems();
        // ----------------------------------

        Debug.Log($"[PlayerGrabAndHitMission] Started. requiredGrabs={requiredGrabs}, requiredHits={requiredHits}");

        // If the mission is already completed by config, complete it
        if (requiredGrabs <= 0 && requiredHits <= 0)
        {
            CompleteMission();
        }
    }

    public override void ResetMission()
    {
        UnsubscribeAll();
        base.ResetMission();
        grabCount = 0;
        hitCount = 0;

        // Re-subscribe to player death for the new player instance
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Exclude);
        }
        
        if (playerController != null)
        {
            playerEntity = playerController.GetComponentInChildren<Entity>();
            if (playerEntity != null)
            {
                playerEntity.OnDeath += OnPlayerDeath;
            }
            SubscribeToPlayerEvents();
        }
        
        // Restart the mission
        SpawnItems();
    }

    private void OnDisable()
    {
        UnsubscribeAll();
    }

    protected override void CompleteMission()
    {
        UnsubscribeAll();
        base.CompleteMission();
    }

    protected override void FailMission()
    {
        UnsubscribeAll();
        base.FailMission();
    }

    #region Item spawning

    /// Spawn items from templates.
    private void SpawnItems()
    {
        // Clear previous
        foreach (var g in activeItems) if (g != null) Destroy(g);
        activeItems.Clear();

        foreach (var tpl in itemTemplates)
        {
            if (tpl == null) continue;

            // Instantiate clone at template's transform
            GameObject inst = Instantiate(tpl, tpl.transform.position, tpl.transform.rotation);
            inst.SetActive(true);
            activeItems.Add(inst);
        }

        Debug.Log($"[PlayerGrabAndHitMission] Spawned {activeItems.Count} items.");
    }

    #endregion

    #region Event handling

    /// Called when the player dies.
    private void OnPlayerDeath(object arg)
    {
        Debug.Log("[PlayerGrabAndHitMission] Player died — resetting mission.");

        // Unsubscribe from events on old player
        var oldEntity = arg as Entity;
        if (oldEntity != null)
        {
            try { oldEntity.OnDeath -= OnPlayerDeath; } catch { }
        }

        // Reset the mission (player handles their own respawn)
        ResetMission();
    }

    /// Subscribe to the player controller's events.
    private void SubscribeToPlayerEvents()
    {
        if (playerController == null) return;

        var grav = playerController.gravitation;
        var kd = playerController.knockdownAttack;

        if (grav != null)
        {
            try { grav.OnGrabbed -= OnGrabbed; } catch { }
            grav.OnGrabbed += OnGrabbed;
        }
        else
        {
            Debug.LogWarning("[PlayerGrabAndHitMission] Gravitation компонент на PlayerController не найден.");
        }

        if (kd != null)
        {
            try { kd.OnAttack -= OnObjectHit; } catch { }
            kd.OnAttack += OnObjectHit;
        }
        else
        {
            Debug.LogWarning("[PlayerGrabAndHitMission] KnockdownAttack компонент на PlayerController не найден.");
        }
    }

    /// Unsubscribe from all event subscriptions.
    private void UnsubscribeAll()
    {
        if (playerController == null) return;

        var grav = playerController.gravitation;
        var kd = playerController.knockdownAttack;

        if (grav != null)
        {
            try { grav.OnGrabbed -= OnGrabbed; } catch { }
        }
        if (kd != null)
        {
            try { kd.OnAttack -= OnObjectHit; } catch { }
        }
        
        // Unsubscribe from player death
        if (playerEntity != null)
        {
            try { playerEntity.OnDeath -= OnPlayerDeath; } catch { }
        }
    }

    /// Called when the player grabs an object.
    private void OnGrabbed(GameObject obj)
    {
        if (requiredGrabs <= 0) return;

        grabCount = Mathf.Min(requiredGrabs, grabCount + 1);
        Debug.Log($"[PlayerGrabAndHitMission] Grab #{grabCount} (obj: {obj?.name ?? "null"})");
        CheckCompletion();
    }

    /// Called when the player hits an object.
    private void OnObjectHit(GameObject obj)
    {
        if (obj == null) return;
        if (requiredHits <= 0) return;

        hitCount = Mathf.Min(requiredHits, hitCount + 1);
        Debug.Log($"[PlayerGrabAndHitMission] Hit #{hitCount} (obj: {obj?.name ?? "null"})");
        CheckCompletion();
    }

    /// Check if the mission is completed.
    private void CheckCompletion()
    {
        bool grabsDone = requiredGrabs <= 0 || grabCount >= requiredGrabs;
        bool hitsDone = requiredHits <= 0 || hitCount >= requiredHits;

        playerGrabAndHitMission_Canvas.grabsSlider.slider.value = grabCount;
        playerGrabAndHitMission_Canvas.hitsSlider.slider.value = hitCount;
        if (grabsDone) playerGrabAndHitMission_Canvas.grabsSlider.background.color = Color.green;
        if (hitsDone) playerGrabAndHitMission_Canvas.hitsSlider.background.color = Color.green;

        if (grabsDone && hitsDone)
        {
            Debug.Log("[PlayerGrabAndHitMission] Условия выполнены — миссия завершена.");
            UnsubscribeAll();
            CompleteMission();
            playerGrabAndHitMission_Canvas.canvas.gameObject.SetActive(false);
        }
    }

    #endregion
}

[Serializable]
public struct PlayerGrabAndHitMission_Canvas
{
    public Canvas canvas;
    public Slider_Data grabsSlider;
    public Slider_Data hitsSlider;
}