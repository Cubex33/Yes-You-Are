#region Walk Mission Stage 1 - learn to walk / look / jump
using Lucy.Core;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;

[DisallowMultipleComponent]
public class WalkMissionStage1 : BaseMission
{
    [Header("References")]
    public Camera playerCamera;
    [Tooltip("Player root transform")] public Transform playerTransform; 
    public WalkMissionStage1_Canvas missionCanvas;

    [Header("Requirements")]
    [Tooltip("Total accumulated horizontal camera rotation (degrees) required to satisfy 'look around' objective")] public float requiredYawDegrees = 90f;
    [Tooltip("Number of jumps required")] public int requiredJumps = 1;
    [Tooltip("Distance (world units) player must travel from the start position")]
    public float requiredDistance = 10f;

    [Header("Runtime")]
    private float accumulatedYaw = 0f;
    private float lastCameraYaw = 0f;
    private int jumpsDone = 0;
    private Vector3 startPos;

    // Player reference for death detection
    private Entity playerEntity;

    protected void Awake()
    {
        if (playerCamera == null && Camera.main != null) playerCamera = Camera.main;
        missionCanvas.distanceSlider.slider.maxValue = requiredDistance;
        missionCanvas.yawSlider.slider.maxValue = requiredYawDegrees;
        missionCanvas.jumpsSlider.isOn= false;
    }

    public override void StartMission()
    {
        if (isActive) return;

        if (playerTransform == null)
        {
            if (debugEnabled) Debug.LogError($"[WalkMissionStage1] playerTransform is null on '{missionName}'");
            FailMission();
            return;
        }

        base.StartMission();

        // init
        startPos = playerTransform.position;
        lastCameraYaw = playerCamera != null ? playerCamera.transform.eulerAngles.y : 0f;
        accumulatedYaw = 0f;
        jumpsDone = 0;

        // subscribe to jump input
        InputProvider.Global.Controls.Player.Jump.performed += OnJumpPerformed;

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

        // show canvas
        missionCanvas.canvas.gameObject.SetActive(true);

        // optionally log
        if (debugEnabled) Debug.Log($"[WalkMissionStage1] Started: look {requiredYawDegrees}°, jumps {requiredJumps}, distance {requiredDistance}");
    }
    
    private void UnsubscribeAll()
    {
        // Unsubscribe from jump input
        if (InputProvider.instance != null)
        {
            try { InputProvider.Global.Controls.Player.Jump.performed -= OnJumpPerformed; } catch { }
        }
        
        // Unsubscribe from player death
        if (playerEntity != null)
        {
            try { playerEntity.OnDeath -= OnPlayerDeath; } catch { }
        }
    }

    private void OnPlayerDeath(object arg)
    {
        if (debugEnabled) Debug.Log("[WalkMissionStage1] Player died — resetting mission.");

        // Unsubscribe from events on old player
        var oldEntity = arg as Entity;
        if (oldEntity != null)
        {
            try { oldEntity.OnDeath -= OnPlayerDeath; } catch { }
        }

        // Reset the mission (player handles their own respawn)
        ResetMission();
    }

    private void OnJumpPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        if (!isActive) return;
        jumpsDone++;
        if (debugEnabled) Debug.Log($"[WalkMissionStage1] Jump #{jumpsDone}");
        CheckCompletion();
    }

    private void Update()
    {
        if (!isActive) return;
        
        // Check if playerTransform has been destroyed
        if (playerTransform == null) return;

        // Accumulate camera yaw (horizontal look)
        if (playerCamera != null)
        {
            float yaw = playerCamera.transform.eulerAngles.y;
            float delta = Mathf.Abs(Mathf.DeltaAngle(lastCameraYaw, yaw));
            accumulatedYaw += delta;
            lastCameraYaw = yaw;
        }

        // Check distance moved
        float dist = Vector3.Distance(startPos, playerTransform.position);
        if (debugEnabled && accumulatedYaw < requiredYawDegrees) Debug.Log($"yaw:{accumulatedYaw} jumps:{jumpsDone} dist:{dist}");

        CheckCompletion();
    }

    private void CheckCompletion()
    {
        if (!isActive) return;
        
        // Check if playerTransform has been destroyed
        if (playerTransform == null) return;
        
        float dist = Vector3.Distance(startPos, playerTransform.position);
        missionCanvas.yawSlider.slider.value = accumulatedYaw;
        if (accumulatedYaw >= requiredYawDegrees)
        {
            missionCanvas.yawSlider.background.color = Color.green;
        }
        if (dist >= requiredDistance)
        {
            missionCanvas.distanceSlider.background.color = Color.green;
        }
        missionCanvas.jumpsSlider.isOn = jumpsDone >= requiredJumps;
        missionCanvas.distanceSlider.slider.value = dist;
        if (accumulatedYaw >= requiredYawDegrees && jumpsDone >= requiredJumps && dist >= requiredDistance)
        {
            CompleteMission();
        }
    }

    public override void ResetMission()
    {
        UnsubscribeAll();
        base.ResetMission();

        accumulatedYaw = 0f;
        jumpsDone = 0;
        
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
        
        // Subscribe to jump input
        InputProvider.Global.Controls.Player.Jump.performed += OnJumpPerformed;
    }

    private void OnDisable()
    {
        UnsubscribeAll();
    }

    protected override void CompleteMission()
    {
        UnsubscribeAll();
        missionCanvas.canvas.gameObject.SetActive(false);
        base.CompleteMission();
    }

    protected override void FailMission()
    {
        UnsubscribeAll();
        base.FailMission();
    }
}
#endregion

[Serializable]
public struct WalkMissionStage1_Canvas
{
    public Canvas canvas;
    public Slider_Data yawSlider;
    public Toggle jumpsSlider;
    public Slider_Data distanceSlider;
}

[Serializable]
public struct Slider_Data
{
    public Image background;
    public Image fill;
    public Slider slider;
}