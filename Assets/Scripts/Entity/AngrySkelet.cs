
using Lucy.Core;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class AngrySkelet : Entity
{
    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Movement")]
    [SerializeField] private float stopDistance = 2f;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnDelay = 2f;

    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 2.5f;
    [SerializeField] private float damage = 15f;
    [SerializeField] private float attackCooldown = 1.5f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private bool useAnimationEvent = false;

    private NavMeshAgent agent;
    private Entity playerEntity;

    private float nextAttackTime;
    private float startMoveTime;
    private bool isAttacking;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        agent.stoppingDistance = stopDistance;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (player == null)
        {
            GameObject obj =
                GameObject.FindGameObjectWithTag("Player");

            if (obj != null)
                player = obj.transform;
        }

        if (player != null)
            playerEntity = player.GetComponentInParent<Entity>();

        // Задержка после появления
        startMoveTime = Time.time + spawnDelay;

        // Первая атака возможна после появления
        nextAttackTime = startMoveTime;

        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        SetWalking(false);
    }

    private void Update()
    {
        if (player == null || !agent.isOnNavMesh)
            return;

        // Ожидание после появления
        if (Time.time < startMoveTime)
        {
            agent.isStopped = true;
            SetWalking(false);
            return;
        }

        float distance = Vector3.Distance(
            transform.position,
            player.position
        );

        if (distance > attackRange)
        {
            // Преследование игрока
            agent.isStopped = false;
            agent.SetDestination(player.position);

            SetWalking(true);
        }
        else
        {
            // Остановка возле игрока
            agent.isStopped = true;
            agent.ResetPath();

            SetWalking(false);

            RotateToPlayer();

            if (Time.time >= nextAttackTime)
            {
                Attack();

                nextAttackTime =
                    Time.time + attackCooldown;
            }
        }
    }

    private void SetWalking(bool walking)
    {
        if (animator == null)
            return;

        // Добавь Bool "IsWalking" в Animator
        animator.SetBool("IsWalking", walking);
    }

    private void RotateToPlayer()
    {
        Vector3 direction =
            player.position - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion rotation =
            Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            rotation,
            Time.deltaTime * 8f
        );
    }

    private void Attack()
    {
        if (animator != null)
            animator.SetTrigger("Attack");

        if (!useAnimationEvent)
            DealDamage();
    }

    // Вызывается через Animation Event,
    // если Use Animation Event включён
    public void DealDamage()
    {
        if (player == null)
            return;

        float distance = Vector3.Distance(
            transform.position,
            player.position
        );

        if (distance > attackRange)
            return;

        if (playerEntity == null)
            playerEntity = player.GetComponentInParent<Entity>();

        if (playerEntity != null)
        {
            playerEntity.TakeDamage(damage, this);

            Debug.Log(
                "Скелет нанёс " + damage + " урона"
            );
        }
    }
}
