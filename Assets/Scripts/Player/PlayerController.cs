using System;
using Lucy.Core;
using Lucy.Game;
using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(KnockdownAttack))]
[RequireComponent(typeof(Gravitation))]
public class PlayerController : FPPController
{
    [Header("Runtime")]
    public KnockdownAttack knockdownAttack;
    public Gravitation gravitation;
    public bool gravitationHolding = false;
    protected override void Awake()
    {
        base.Awake();
        knockdownAttack = GetComponent<KnockdownAttack>();
        gravitation = GetComponent<Gravitation>();
        InputProvider.Global.Controls.Player.Attack.performed += _ => knockdownAttack.Attack();
        InputProvider.Global.Controls.Player.Grab.performed += _ => gravitationHolding = true;
        InputProvider.Global.Controls.Player.Grab.canceled += _ => gravitationHolding = false;
        InputProvider.Global.Controls.Player.Reload.performed += _ => knockdownAttack.isRecovery = true;
        InputProvider.Global.Controls.Player.Reload.canceled += _ => knockdownAttack.isRecovery = false;
        InputProvider.Global.Controls.Player.Scroll.performed += AdjustGrabDistance;
    }

    void FixedUpdate()
    {
        if (gravitationHolding)
        {
            gravitation.Grab();
        } else
        {
            gravitation.Release();
        }
    }

    private void AdjustGrabDistance(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    { 
        float scrollDelta = ctx.ReadValue<Vector2>().y; 
        gravitation.AdjustDistance(scrollDelta * gravitation.distanceStep); 
    }

    void Start()
    {
        var camera = FindAnyObjectByType<CinemachineCamera>(FindObjectsInactive.Include);
        camera.Target = new CameraTarget { LookAtTarget = headSocket.transform, TrackingTarget = headSocket.transform};
    }
}