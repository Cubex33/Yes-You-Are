using System;
using UnityEngine;
using UnityEngine.UI;
using Lucy.Core;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class DestroySingleEntityMission : BaseMission
{
    [Header("Target Entity to destroy")]
    [Tooltip("Single existing scene Entity to destroy. Mission remembers its state, waits for player to destroy it. On reset, restores to original state.")]
    public Entity targetEntity;

    [Header("UI elements to update")]
    public DestroySingleEntity_Canvas missionCanvas;

    [Header("Events")]
    public UnityEvent OnCompleted;
    public UnityEvent OnFailed;

    // Runtime state
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool originalActiveState;

    private Entity playerEntity;
    private bool isDestroyed = false;

    public override void StartMission()
    {
        if (isActive) return;

        FindPlayerSubscribe();

        if (targetEntity == null)
        {
            Debug.LogWarning("[DestroySingleEntityMission] No targetEntity assigned!");
            return;
        }

        // Capture original state FIRST
        var targetGO = targetEntity.gameObject;
        originalPosition = targetGO.transform.position;
        originalRotation = targetGO.transform.rotation;
        originalActiveState = targetGO.activeSelf;

        // Subscribe early
        targetEntity.OnDeath += OnTargetDestroyed;

        // NOW restore to original state + full health
        targetGO.transform.position = originalPosition;
        targetGO.transform.rotation = originalRotation;
        targetGO.SetActive(originalActiveState);
        targetEntity.SetMaxHealth(targetEntity.MaxHealth, true);  // Full heal to original

        isDestroyed = false;
        ActivateCanvas();
        if (missionCanvas.progressBar != null) targetEntity.OnDamageTaken += (_, _) => missionCanvas.progressBar.value = targetEntity.Health;

        base.StartMission();

        CheckProgress();
    }

    private void ActivateCanvas()
    {
        if (missionCanvas.canvas == null) return;
        missionCanvas.canvas.gameObject.SetActive(true);
        missionCanvas.progressBar.maxValue = targetEntity.MaxHealth;
        missionCanvas.progressBar.value = targetEntity.Health;
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

    private void UnsubscribeTarget()
    {
        if (targetEntity != null)
        {
            try 
            {
                targetEntity.OnDeath -= OnTargetDestroyed;
            }
            catch { }
        }
    }

    private void OnPlayerDeath(object arg)
    {
        ResetMission();
    }

    private void OnTargetDestroyed(object arg)
    {
        isDestroyed = true;
        Debug.Log("[DestroySingleEntityMission] Target entity destroyed.");

        CheckProgress();
    }

    private void CheckProgress()
    {
        if (isDestroyed)
        {
            CompleteMission();
        }
    }

    public override void ResetMission()
    {
        UnsubscribePlayer();
        UnsubscribeTarget();
        ActivateCanvas();
        ResetItems();
        Debug.Log("[DestroySingleEntityMission] Mission reset.");
        OnFailed.Invoke();
        base.ResetMission();
        FindPlayerSubscribe();
    }

    private void ResetItems()
    {
        if (targetEntity != null)
        {
            var targetGO = targetEntity.gameObject;
            if (targetGO != null)
            {
                targetGO.transform.position = originalPosition;
                targetGO.transform.rotation = originalRotation;
                targetGO.SetActive(originalActiveState);
            }
            targetEntity.SetMaxHealth(targetEntity.MaxHealth, true);
        }
    }

    protected override void CompleteMission()
    {
        UnsubscribePlayer();
        UnsubscribeTarget();
        if (missionCanvas.canvas != null)
            missionCanvas.canvas.gameObject.SetActive(false);
        OnCompleted.Invoke();
        base.CompleteMission();
    }

    protected override void FailMission()
    {
        UnsubscribePlayer();
        UnsubscribeTarget();
        if (missionCanvas.canvas != null)
            missionCanvas.canvas.gameObject.SetActive(false);
        base.FailMission();
    }

    private void OnDisable()
    {
        UnsubscribePlayer();
        UnsubscribeTarget();
        if (missionCanvas.canvas != null)
            missionCanvas.canvas.gameObject.SetActive(false);
    }
}

[Serializable]
public struct DestroySingleEntity_Canvas
{
    public Canvas canvas;
    public Slider progressBar;
}
