using System;
using Lucy.Core;
using UnityEngine;

[RequireComponent(typeof(EnemyBase))]
[RequireComponent(typeof(Animator))]
public class FleeEntityAnimation : MonoBehaviour
{
    [Header("Runtime")]
    public Animator animator;
    public EnemyBase enemyBase;
    void Awake()
    {
        enemyBase = GetComponent<EnemyBase>();
        animator = GetComponent<Animator>();
        enemyBase.OnStateChanged += handleStateChanged;
    }

    private void handleStateChanged()
    {
        switch (enemyBase.CurrentState)
        {
            case EnemyState.Idle:
                HandleIdle();
                break;
            case EnemyState.Chase:
                HandleChase();
                break;
            case EnemyState.Attack:
                HandleAttack();
                break;
            case EnemyState.Stun:
                HandleStun();
                break;
            case EnemyState.Flee:
                HandleFlee();
                break;
            case EnemyState.Falling:
                HandleFalling();
                break;
            case EnemyState.Cooldown:
                HandleCooldown();
                break;
            default:
                HandleCustomState();
                break;
        }
    }

    private void HandleCustomState()
    {
        throw new NotImplementedException();
    }

    private void HandleCooldown()
    {
        throw new NotImplementedException();
    }

    private void HandleFalling()
    {
        animator.enabled = false;
    }

    private void HandleFlee()
    {
        animator.enabled = true;
        animator.SetBool("Flee", true);
    }

    private void HandleStun()
    {
        animator.enabled = false;
    }

    private void HandleAttack()
    {
        throw new NotImplementedException();
    }

    private void HandleChase()
    {
        throw new NotImplementedException();
    }

    private void HandleIdle()
    {
        animator.enabled = true;
        animator.SetBool("Flee", false);
    }
}