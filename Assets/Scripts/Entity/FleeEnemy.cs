using System.ComponentModel;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class FleeEnemy : EnemyBase
{
    [Header("Flee settings")]
    public float fleeDistance = 8f;
    public float fleeUpdateInterval = 0.4f;
    public float fleeSpeed = 6f;
    public float minEscapeDistance = 1.5f;

    [Header("Panic settings")]
    public float randomDirectionStrength = 2.5f;
    public float separationRadius = 2f;
    public float separationStrength = 3f;

    float nextFleeUpdate = 0f;

    public override void Start()
    {
        base.Start();
        CurrentState = EnemyState.Idle;

        if (agent != null)
        {
            agent.updateRotation = true;
            agent.updatePosition = true;
            agent.speed = fleeSpeed;

            // 🔥 Улучшенное избегание
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            agent.avoidancePriority = Random.Range(10, 90);
        }
    }

    public override void HandleIdle()
    {
        base.HandleIdle();

        if (IsTargetInDistance(visionDistance))
        {
            CurrentState = EnemyState.Flee;

            if (!agent.enabled) agent.enabled = true;
            agent.isStopped = false;
            agent.speed = fleeSpeed;
        }
        else
        {
            if (agent != null && agent.enabled)
            {
                agent.ResetPath();
                agent.isStopped = true;
            }
        }
    }

    public override void HandleFlee()
    {
        if (target == null)
        {
            CurrentState = EnemyState.Idle;
            return;
        }

        // Если цель потеряна из виду — возвращаемся в Idle (стоим)
        if (!IsTargetInDistance(visionDistance))
        {
            agent.ResetPath();
            agent.isStopped = true;
            CurrentState = EnemyState.Idle;
            return;
        }

        // Подготовка агента
        if (!agent.enabled) agent.enabled = true;
        rb.isKinematic = true;
        agent.isStopped = false;
        agent.speed = fleeSpeed;

        // Реже пересчитываем destination, чтобы не трясти путь каждый кадр
        if (Time.time < nextFleeUpdate)
        {
            // если дошли до текущей цели и нужно пересчитать
            if (!agent.pathPending && agent.remainingDistance <= 0.5f)
            {
                nextFleeUpdate = 0f; // форсируем пересчет
            }
            else
            {
                return;
            }
        }

        nextFleeUpdate = Time.time + fleeUpdateInterval;

        // направление от цели к нам (по горизонтали)
        Vector3 awayDir = (transform.position - target.transform.position);
        awayDir.y = 0f;

        // если слишком близко и awayDir почти ноль — выбираем случайное напр.
        if (awayDir.sqrMagnitude < 0.01f)
        {
            Vector3 rnd = Random.onUnitSphere;
            rnd.y = 0f;
            awayDir = rnd.normalized;
        }

        float wantedDistance = fleeDistance;
        // если очень близко — убегаем дальше
        if (Vector3.Distance(transform.position, target.transform.position) < minEscapeDistance)
            wantedDistance = Mathf.Max(fleeDistance, minEscapeDistance + 1.5f);

        Vector3 rawTargetPos = transform.position + awayDir.normalized * wantedDistance;

        // Пробуем найти ближайшую точку на NavMesh
        NavMeshHit hit;
        // agent.areaMask можно использовать, чтобы учитывать доступные области
        int mask = (agent != null) ? agent.areaMask : NavMesh.AllAreas;
        if (NavMesh.SamplePosition(rawTargetPos, out hit, 3.0f, mask))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            // fallback: попробуем ближе к текущей позиции
            if (NavMesh.SamplePosition(transform.position + awayDir.normalized * (wantedDistance * 0.5f), out hit, 6.0f, mask))
            {
                agent.SetDestination(hit.position);
            }
            else
            {
                // если совсем ничего — просто стоим (без крэша)
                agent.ResetPath();
            }
        }
    }

    


}
