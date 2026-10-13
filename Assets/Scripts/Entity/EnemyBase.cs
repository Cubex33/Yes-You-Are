
using UnityEngine;
using UnityEngine.AI;
using Lucy.Core;
using System;

public enum EnemyState
{
    Idle,
    Patrol,
    Stun,
    Chase,
    Attack,
    Flee,
    Falling,
    Cooldown
}

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Entity))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Settings")]
    public float meleeDistance = 2.0f;
    public float rangeDistance = 5.0f;
    public float visionDistance = 10.0f;
    public float attackCooldown = 2.0f;
    public float stunDuration = 1.5f;
    public float fearDuration = 3.0f;
    public float attackDamage = 10f;

    [Header("Target")]
    public string targetTag = "Player";
    public LayerMask obstacleMask = -1;

    [Header("Ground Check")]
    public bool groundCheckEnable = true;
    public Transform groundCheck;
    public float groundCheckRadius = 0.25f;
    public LayerMask groundMask;
    public float landingVelocityThreshold = 0.5f;
    public float landingTimeThreshold = 0.12f;

    [Header("NavMesh Recovery")]
    [Tooltip("Максимальная дистанция поиска NavMesh после падения.")]
    public float navMeshRecoveryRadius = 3f;

    [Header("Debug")]
    public bool drawGizmos = false;
    public GameObject target;
    public NavMeshAgent agent;
    public Rigidbody rb;
    public Entity entity;
    public bool isGrounded = false;
    public float groundedSince = 0f;

    private EnemyState _currentState;

    public EnemyState CurrentState
    {
        get => _currentState;
        set
        {
            if (_currentState == value)
                return;

            _currentState = value;
            currentStateDisplay = value.ToString();
            OnStateChanged?.Invoke();
        }
    }

    public string currentStateDisplay;
    public float stateTimer;
    public float cooldownUntil;

    public bool IsOnCooldown => Time.time < cooldownUntil;
    public Action OnStateChanged;

    public virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        entity = GetComponent<Entity>();

        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.isKinematic = true;
    }

    public virtual void Start()
    {
        target = GameObject.FindGameObjectWithTag(targetTag);
        CurrentState = EnemyState.Idle;
    }

    public virtual void Update()
    {
        // Обрабатываем ИИ раз в пять кадров.
        if (Time.frameCount % 5 != 0)
            return;

        // Idle не требует постоянной обработки вдали от игрока.
        // Все остальные состояния должны продолжаться независимо от дистанции.
        if (CurrentState == EnemyState.Idle &&
            !IsTargetInDistance(visionDistance))
        {
            return;
        }

        HandleState();
    }

    private void FixedUpdate()
    {
        UpdateGrounded();
    }

    private void UpdateGrounded()
    {
        if (!groundCheckEnable)
        {
            isGrounded = false;
            groundedSince = 0f;
            return;
        }

        if (groundCheck == null)
        {
            Vector3 origin = transform.position + Vector3.down * 0.1f;
            isGrounded = Physics.CheckSphere(
                origin,
                groundCheckRadius,
                groundMask
            );
        }
        else
        {
            isGrounded = Physics.CheckSphere(
                groundCheck.position,
                groundCheckRadius,
                groundMask
            );
        }

        if (isGrounded)
        {
            if (groundedSince <= 0f)
                groundedSince = Time.time;
        }
        else
        {
            groundedSince = 0f;
        }
    }

    public virtual void HandleState()
    {
        switch (CurrentState)
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

    public virtual void HandleIdle()
    {
        if (target == null)
            return;
    }

    public virtual void HandleChase()
    {
        if (!agent.enabled)
        {
            if (!TryRestoreAgentOnNavMesh())
                return;
        }

        if (!agent.isOnNavMesh)
            return;

        rb.isKinematic = true;
        agent.isStopped = false;
    }

    public virtual void HandleAttack()
    {
        if (target == null)
        {
            CurrentState = EnemyState.Idle;
            return;
        }
    }

    public virtual void HandleStun()
    {
        stateTimer -= Time.deltaTime;

        if (stateTimer > 0f)
            return;

        // Скелет может оказаться за пределами NavMesh после броска.
        if (!TryRestoreAgentOnNavMesh())
        {
            // Повторяем попытку, не застревая в вечном оглушении.
            stateTimer = 0.5f;
            return;
        }

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        agent.isStopped = false;

        ResetCooldown();
        CurrentState = EnemyState.Idle;
    }

    public virtual void HandleFlee()
    {
        if (target == null)
        {
            CurrentState = EnemyState.Idle;
            return;
        }

        stateTimer -= Time.deltaTime;

        if (stateTimer <= 0f)
        {
            if (agent.enabled && agent.isOnNavMesh)
                agent.ResetPath();

            CurrentState = EnemyState.Idle;
            return;
        }

        if (!agent.enabled || !agent.isOnNavMesh)
            return;

        Vector3 dir = transform.position - target.transform.position;

        if (dir.sqrMagnitude > 0.001f)
            agent.SetDestination(transform.position + dir.normalized * 5f);
    }

    public virtual void HandleFalling()
    {
        if (!isGrounded)
            return;

        if (Mathf.Abs(rb.linearVelocity.y) > landingVelocityThreshold)
            return;

        if (groundedSince <= 0f ||
            Time.time - groundedSince < landingTimeThreshold)
        {
            return;
        }

        // Физика завершилась: останавливаем скелета и оглушаем его.
        // NavMesh восстановим после окончания оглушения.
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        stateTimer = 2f;
        CurrentState = EnemyState.Stun;
    }

    public virtual void HandleCooldown()
    {
        if (!IsOnCooldown)
            CurrentState = EnemyState.Idle;
    }

    public virtual void HandleCustomState() { }

    public void StartCooldown(float duration)
    {
        cooldownUntil = Time.time + duration;
    }

    public void ResetCooldown()
    {
        cooldownUntil = 0f;
    }

    public virtual void ApplyStun(float? duration = null)
    {
        stateTimer = duration ?? stunDuration;

        if (agent.enabled)
        {
            if (agent.isOnNavMesh)
                agent.isStopped = true;

            agent.enabled = false;
        }

        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.None;

        ResetCooldown();
        CurrentState = EnemyState.Stun;
    }

    public virtual void ApplyFlee()
    {
        stateTimer = fearDuration;

        if (!agent.enabled && !TryRestoreAgentOnNavMesh())
            return;

        if (!agent.isOnNavMesh)
            return;

        rb.isKinematic = true;
        agent.isStopped = false;

        CurrentState = EnemyState.Flee;
    }

    public virtual void ApplyFalling()
    {
        // Не сбрасываем таймер и состояние каждый кадр.
        if (CurrentState == EnemyState.Falling)
            return;

        ResetCooldown();

        groundCheckEnable = false;
        isGrounded = false;
        groundedSince = 0f;

        if (agent.enabled)
        {
            if (agent.isOnNavMesh)
                agent.isStopped = true;

            agent.enabled = false;
        }

        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.None;

        CurrentState = EnemyState.Falling;
    }

    private bool TryRestoreAgentOnNavMesh()
    {
        if (agent == null || rb == null)
            return false;

        // Если агент уже корректно находится на NavMesh, используем его.
        if (agent.enabled && agent.isOnNavMesh)
            return true;

        // Ищем ближайшую доступную точку NavMesh.
        if (!NavMesh.SamplePosition(
                transform.position,
                out NavMeshHit hit,
                navMeshRecoveryRadius,
                agent.areaMask))
        {
            return false;
        }

        // Сначала переносим корень на найденную точку,
        // затем включаем агент.
        transform.position = hit.position;

        if (!agent.enabled)
            agent.enabled = true;

        if (!agent.Warp(hit.position))
        {
            agent.enabled = false;
            return false;
        }

        return agent.isOnNavMesh;
    }

    public bool IsTargetInDistance(float distance)
    {
        if (target == null)
            return false;

        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 dir = target.transform.position - origin;

        if (dir.sqrMagnitude > distance * distance)
            return false;

        if (Physics.Raycast(
                origin,
                dir.normalized,
                out RaycastHit hit,
                distance,
                obstacleMask))
        {
            return hit.collider.transform.root.gameObject == target;
        }

        return true;
    }

    public float DistanceToTarget
    {
        get
        {
            if (target == null)
                return 0f;

            return Vector3.Distance(
                transform.position,
                target.transform.position
            );
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, visionDistance);

        if (target != null)
        {
            Vector3 direction =
                (target.transform.position - transform.position).normalized;

            Gizmos.DrawRay(transform.position, direction * rangeDistance);
            Gizmos.DrawSphere(
                transform.position + direction * rangeDistance,
                0.1f
            );

            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, direction * meleeDistance);
        }
        else
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, rangeDistance);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, meleeDistance);
        }
    }
}
