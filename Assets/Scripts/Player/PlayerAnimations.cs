#if UNITY_EDITOR
using System.Diagnostics;
using UnityEditor;
#endif
using System;
using Lucy.Core;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Entity))]
[RequireComponent(typeof(PlayerController))]
public class PlayerAnimations : MonoBehaviour
{
    public Animator animator;
    [Header("Settings")]
    public float shortDistance;
    public float middleAttackDistance;
    public float longAttackDistance;
    [Header("Debug")]
    public bool debugEnabled = false;
    public PlayerController controller;
    public Entity player;

    private void Start()
    {
        controller = GetComponent<PlayerController>();
        player = GetComponent<Entity>();
        player.OnDamageTaken += (_, _) => animator.SetTrigger("IsDamage");
        controller.gravitation.OnGrabbed += _ => animator.SetTrigger("IsGravityOn");
        controller.gravitation.OnReleased += _ => animator.SetTrigger("IsGravityOff"); 
        controller.knockdownAttack.OnAttack += OnAttack;
    }

    private void OnAttack(GameObject obj)
    {
        string attackDistance = obj == null || Vector3.Distance(obj.transform.position, transform.position) >= longAttackDistance ? "IsAttackLong" : Vector3.Distance(obj.transform.position, transform.position) >= middleAttackDistance ? "IsAttackMiddle" : Vector3.Distance(obj.transform.position, transform.position) >= shortDistance ? "IsAttackShort" : null;
        if (attackDistance != null) animator.SetTrigger(attackDistance + (!controller.gravitation.IsGrabbing && UnityEngine.Random.value > 0.5f ? "R" : "L"));
    }

    private void LateUpdate()
    {
        animator.SetBool("IsWalk", controller.movement.IsMoving);
        animator.SetBool("IsGravityUsingBool", controller.gravitationHolding);
        animator.SetBool("IsGrounded", controller.movement.Controller.isGrounded);
        animator.SetBool("IsReloading", controller.knockdownAttack.isRecovery);
        animator.SetFloat("Speed", controller.movement.currentHorizontalVelocity.magnitude);
    }

    #if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!debugEnabled) return;
        Vector3 pos = transform.position;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(pos, shortDistance);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pos, middleAttackDistance);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(pos, longAttackDistance);
    }
    #endif
}