using System;
using Lucy.Core;
using Lucy.Game;
using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(KnockdownAttack))]
[RequireComponent(typeof(Gravitation))]
public class PlayerController : FPPController
{
    public static event Action<PlayerController> GravityGrabUsed;
    public static event Action<PlayerController> GrabDistanceAdjusted;
    public static event Action<PlayerController> StaminaChargeStarted;
    public static event Action<PlayerController> SuperAttackUsed;
    [Header("Runtime")]
    public KnockdownAttack knockdownAttack;
    public Gravitation gravitation;
    public bool gravitationHolding = false;
    private bool chargingInputWasHeld;
    private bool gravityLaunchLocked;
    protected override void Awake()
    {
        base.Awake();
        knockdownAttack = GetComponent<KnockdownAttack>();
        gravitation = GetComponent<Gravitation>();
        InputProvider.Global.Controls.Player.Attack.performed += _ =>
        {
            bool wasHoldingObject = gravitation.IsGrabbing;

            bool wasSuperAttack =
                knockdownAttack.isRecovery && knockdownAttack.IsFullStamina;

            knockdownAttack.Attack();

            // Если ЛКМ запустила удерживаемый предмет,
            // запрещаем повторный захват до отпускания ПКМ.
            if (wasHoldingObject && !gravitation.IsGrabbing)
                gravityLaunchLocked = true;

            if (wasSuperAttack && !knockdownAttack.IsFullStamina)
                SuperAttackUsed?.Invoke(this);
        };
        InputProvider.Global.Controls.Player.Grab.performed += _ =>
        {
            gravitationHolding = true;
        };

        InputProvider.Global.Controls.Player.Grab.canceled += _ =>
        {
            gravitationHolding = false;
            gravityLaunchLocked = false;
        };
        InputProvider.Global.Controls.Player.Scroll.performed += AdjustGrabDistance;
    }

    private void LateUpdate()
    {
        // Polling the held action makes charging reliable even when the input system
        // misses a performed/canceled callback during focus changes or scene startup.
        var reload = InputProvider.Global.Controls.Player.Reload;
        bool isHeld = reload.IsPressed();
        knockdownAttack.isRecovery = isHeld;
        if (isHeld && !chargingInputWasHeld)
            StaminaChargeStarted?.Invoke(this);
        chargingInputWasHeld = isHeld;
    }

    void FixedUpdate()
    {
        if (gravityLaunchLocked)
            return;

        if (gravitationHolding && !gravitation.IsGrabbing)
        {
            bool wasGrabbing = gravitation.IsGrabbing;
            gravitation.Grab();

            if (!wasGrabbing && gravitation.IsGrabbing)
                GravityGrabUsed?.Invoke(this);
        }
        else if (!gravitationHolding && gravitation.IsGrabbing)
        {
            gravitation.Release();
        }
    }

    private void AdjustGrabDistance(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    { 
        float scrollDelta = ctx.ReadValue<Vector2>().y; 
        gravitation.AdjustDistance(scrollDelta * gravitation.distanceStep); 
        if (Mathf.Abs(scrollDelta) > 0f && gravitation.IsGrabbing)
            GrabDistanceAdjusted?.Invoke(this);
    }

    void Start()
    {
        var camera = FindAnyObjectByType<CinemachineCamera>(FindObjectsInactive.Include);
        camera.Target = new CameraTarget { LookAtTarget = headSocket.transform, TrackingTarget = headSocket.transform};
    }
}
