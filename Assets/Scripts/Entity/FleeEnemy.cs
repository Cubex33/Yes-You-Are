
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class FleeEnemy : EnemyBase
{
    [Header("Flee Settings")]
    [Tooltip("Скорость движения скелета при побеге")]
    [Min(1f)]
    public float fleeSpeed = 9f;

    [Tooltip("На какое расстояние скелет прокладывает путь от себя")]
    [Min(2f)]
    public float fleeDistance = 12f;

    [Tooltip("Как часто скелет выбирает новое направление. Меньше = чаще")]
    [Range(0.05f, 1f)]
    public float pathUpdateInterval = 0.2f;

    private float nextPathUpdate;

    public override void Start()
    {
        base.Start();

        CurrentState = EnemyState.Idle;

        if (agent == null)
            return;

        // Настраиваем движение для быстрого и плавного побега.
        agent.updateRotation = true;
        agent.updatePosition = true;

        agent.speed = fleeSpeed;
        agent.acceleration = 40f;
        agent.angularSpeed = 720f;
        agent.stoppingDistance = 0.1f;
        agent.autoBraking = false;

        // Помогаем скелетам обходить друг друга.
        agent.obstacleAvoidanceType =
            ObstacleAvoidanceType.HighQualityObstacleAvoidance;

        agent.avoidancePriority = Random.Range(10, 90);
    }

    public override void HandleIdle()
    {
        base.HandleIdle();

        if (agent == null || target == null)
            return;

        if (IsTargetInDistance(visionDistance))
        {
            CurrentState = EnemyState.Flee;
            nextPathUpdate = 0f;
        }
        else if (agent.enabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }
    }

    public override void HandleFlee()
    {
        if (agent == null || target == null)
        {
            CurrentState = EnemyState.Idle;
            return;
        }

        // Если игрок далеко, прекращаем побег.
        if (!IsTargetInDistance(visionDistance))
        {
            if (agent.enabled && agent.isOnNavMesh)
            {
                agent.ResetPath();
                agent.isStopped = true;
            }

            CurrentState = EnemyState.Idle;
            return;
        }

        // Агент должен быть активен и находиться на NavMesh.
        if (!agent.enabled || !agent.isOnNavMesh)
            return;

        rb.isKinematic = true;

        agent.isStopped = false;
        agent.speed = fleeSpeed;
        agent.autoBraking = false;

        // Не пересчитываем маршрут каждый кадр.
        // Но и не заставляем скелета долго ждать.
        if (Time.time < nextPathUpdate &&
            !agent.pathPending &&
            agent.hasPath &&
            agent.remainingDistance > 0.5f)
        {
            return;
        }

        nextPathUpdate = Time.time + pathUpdateInterval;

        // Определяем направление от игрока.
        Vector3 awayDirection =
            transform.position - target.transform.position;

        awayDirection.y = 0f;

        if (awayDirection.sqrMagnitude < 0.01f)
        {
            awayDirection = Random.insideUnitSphere;
            awayDirection.y = 0f;
        }

        awayDirection.Normalize();

        Vector3 desiredPosition =
            transform.position + awayDirection * fleeDistance;

        int areaMask = agent.areaMask;

        // Ищем подходящую точку на NavMesh.
        if (NavMesh.SamplePosition(
            desiredPosition,
            out NavMeshHit hit,
            5f,
            areaMask))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            // Если впереди стена или край NavMesh,
            // пробуем найти ближайший доступный путь.
            Vector3 shorterPosition =
                transform.position + awayDirection * (fleeDistance * 0.5f);

            if (NavMesh.SamplePosition(
                shorterPosition,
                out hit,
                8f,
                areaMask))
            {
                agent.SetDestination(hit.position);
            }
        }
    }
}
