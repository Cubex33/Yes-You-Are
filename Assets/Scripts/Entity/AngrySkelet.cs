using System.Collections.Generic;
using Lucy.Core;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class AngrySkelet : Entity
{
    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Detection")]
    [SerializeField, Min(1f)] private float detectionRange = 25f;
    [SerializeField, Min(1f)] private float loseTargetRange = 32f;

    private bool hasDetectedPlayer;

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

    [Header("Death Explosion")]
    [SerializeField] private List<GameObject> debrisParts = new List<GameObject>();

    [SerializeField] private float explosionForce = 10f;
    [SerializeField] private float explosionRadius = 2f;
    [SerializeField] private float upwardModifier = 1.5f;
    [SerializeField] private float destroyDelay = 0.1f;
    [SerializeField] private float debrisLifetime = 8f;

    private NavMeshAgent agent;
    private Entity playerEntity;

    private float nextAttackTime;
    private float startMoveTime;

    private bool isDead;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        agent.stoppingDistance = stopDistance;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (player == null)
        {
            GameObject obj = GameObject.FindGameObjectWithTag("Player");

            if (obj != null)
                player = obj.transform;
        }

        if (player != null)
            playerEntity = player.GetComponentInParent<Entity>();

        startMoveTime = Time.time + spawnDelay;
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
        if (isDead || player == null || agent == null ||
            !agent.enabled || !agent.isOnNavMesh)
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

        // Если игрок ещё не обнаружен — проверяем радиус обнаружения.
        if (!hasDetectedPlayer)
        {
            if (distance > detectionRange)
            {
                agent.isStopped = true;
                agent.ResetPath();
                SetWalking(false);
                return;
            }

            hasDetectedPlayer = true;
        }

        // Если игрок убежал слишком далеко — скелет прекращает погоню.
        if (distance > loseTargetRange)
        {
            hasDetectedPlayer = false;
            agent.isStopped = true;
            agent.ResetPath();
            SetWalking(false);
            return;
        }

        if (distance > attackRange)
        {
            // Преследование обнаруженного игрока
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
                nextAttackTime = Time.time + attackCooldown;
            }
        }
    }


    private void SetWalking(bool walking)
    {
        if (animator == null || !animator.enabled)
            return;

        animator.SetBool("IsWalking", walking);
    }

    private void RotateToPlayer()
    {
        Vector3 direction = player.position - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion rotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            rotation,
            Time.deltaTime * 8f
        );
    }

    private void Attack()
    {
        if (isDead)
            return;

        if (animator != null && animator.enabled)
            animator.SetTrigger("Attack");

        if (!useAnimationEvent)
            DealDamage();
    }

    // Можно вызывать через Animation Event
    public void DealDamage()
    {
        if (isDead || player == null)
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

            Debug.Log("Скелет нанёс " + damage + " урона");
        }
    }

    // ==================================
    // СМЕРТЬ И РАСПАД СКЕЛЕТА
    // ==================================

    protected override void HandleDeath(object source)
    {
        if (isDead)
            return;

        isDead = true;

        Debug.Log("Скелет погиб и распался на части!");

        // Остановка движения
        if (agent != null && agent.enabled)
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            agent.enabled = false;
        }

        // Отключение анимации
        if (animator != null)
            animator.enabled = false;

        // Центр взрыва
        Vector3 explosionPosition = transform.position + Vector3.up;

        // Отделяем и разбрасываем части
        foreach (GameObject part in debrisParts)
        {
            if (part == null)
                continue;

            // Отделяем от скелета
            part.transform.SetParent(null, true);

            // Активируем деталь
            part.SetActive(true);

            // Включаем отображение
            Renderer[] renderers = part.GetComponentsInChildren<Renderer>();

            foreach (Renderer rend in renderers)
                rend.enabled = true;

            // Добавляем коллайдер при необходимости
            Collider col = part.GetComponent<Collider>();

            if (col == null)
                col = part.AddComponent<BoxCollider>();

            col.enabled = true;

            // Добавляем физику
            Rigidbody rb = part.GetComponent<Rigidbody>();

            if (rb == null)
                rb = part.AddComponent<Rigidbody>();

            rb.isKinematic = false;
            rb.useGravity = true;

            // Толчок от взрыва
            rb.AddExplosionForce(
                explosionForce,
                explosionPosition,
                explosionRadius,
                upwardModifier,
                ForceMode.Impulse
            );

            // Случайное вращение костей
            rb.AddTorque(
                Random.insideUnitSphere * explosionForce,
                ForceMode.Impulse
            );

            // Удаление деталей спустя время
            Destroy(part, debrisLifetime);
        }

        // Отключаем коллайдеры основного скелета
        Collider[] colliders = GetComponentsInChildren<Collider>();

        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        // Удаляем основного скелета
        Destroy(gameObject, destroyDelay);
    }
}