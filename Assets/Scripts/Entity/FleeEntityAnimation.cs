
using UnityEngine;

[RequireComponent(typeof(EnemyBase))]
[RequireComponent(typeof(Animator))]
public class FleeEntityAnimation : MonoBehaviour
{
    [Header("Runtime")]
    public Animator animator;
    public EnemyBase enemyBase;

    private void Awake()
    {
        enemyBase = GetComponent<EnemyBase>();
        animator = GetComponent<Animator>();

        enemyBase.OnStateChanged += HandleStateChanged;
    }

    private void Start()
    {
        // Применяем анимацию текущего состояния после инициализации EnemyBase.
        HandleStateChanged();
    }

    private void OnDestroy()
    {
        if (enemyBase != null)
            enemyBase.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged()
    {
        if (enemyBase == null || animator == null)
            return;

        switch (enemyBase.CurrentState)
        {
            case EnemyState.Stun:
            case EnemyState.Falling:
                animator.enabled = false;
                break;

            case EnemyState.Flee:
                EnableAnimator();
                animator.SetBool("Flee", true);
                break;

            case EnemyState.Idle:
            case EnemyState.Patrol:
            case EnemyState.Chase:
            case EnemyState.Attack:
            case EnemyState.Cooldown:
            default:
                EnableAnimator();
                animator.SetBool("Flee", false);
                break;
        }
    }

    private void EnableAnimator()
    {
        animator.enabled = true;
    }
}
