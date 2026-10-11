
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Lucy.Core;

[RequireComponent(typeof(NavMeshAgent))]
public class Boss : Entity
{
    [Header("Target & NavMesh")]
    [SerializeField] private Transform player;
    [SerializeField] private float speed = 3.8f;
    [SerializeField] private float spawnWait = 2f;
    [SerializeField] private float navMeshSnapRadius = 5f;
    [SerializeField] private float meleeRange = 3f;

    [Header("Damage / timings")]
    [SerializeField] private float comboHitDamage = 12f;
    [SerializeField] private float slamDamage = 38f;
    [SerializeField] private float slamRadius = 5.5f;
    [SerializeField] private float dashDamage = 26f;
    [SerializeField] private float fireballDamage = 22f;
    [SerializeField] private float projectileSpeed = 16f;
    [SerializeField] private float actionCooldown = 2f;

    [Header("Skeletons")]
    [SerializeField] private GameObject skeletonPrefab;
    [SerializeField] private int minimumSummons = 3;
    [SerializeField] private int maximumSummons = 5;
    [SerializeField] private int maxLivingSummons = 12;
    [SerializeField] private float summonInterval = 18f;
    [SerializeField] private float summonRadius = 5f;

    [Header("Fireball Height")]
    [SerializeField] private float fireballHeight = 1.7f;
    [SerializeField] private bool showFireballGizmos = true;

    [Header("VFX Prefabs")]
    [SerializeField] private FireballProjectile fireballPrefab;
    [SerializeField] private ParticleSystem fireballImpactPrefab;
    [SerializeField] private ParticleSystem spawnVFX;
    [SerializeField] private ParticleSystem comboVFX;
    [SerializeField] private ParticleSystem slamVFX;
    [SerializeField] private ParticleSystem summonVFX;
    [SerializeField] private ParticleSystem dashVFX;
    [SerializeField] private ParticleSystem rageVFX;

    [Header("Animation & Hit Reactions")]
    [SerializeField] private Animator animator;
    [SerializeField] private bool useDamageReactions = true;
    [SerializeField] private float heavyHitThreshold = 35f;
    [SerializeField] private float staggerThreshold = 80f;
    [SerializeField] private float staggerDuration = 1.2f;
    [SerializeField] private float reactionCooldown = 0.6f;

    [Header("Ground Slam")]
    [SerializeField] private InfernoGroundSlam groundSlamPrefab;
    [SerializeField] private float groundSlamForwardOffset = 1.2f;

    [Header("Ground Slam Visual Impact")]
    [SerializeField] private ParticleSystem slamImpactVFX;
    [SerializeField] private bool createFallbackShockwave = true;
    [SerializeField] private float shockwaveDuration = 0.65f;
    [SerializeField] private int shockwaveSegments = 64;

    [Header("Rage")]
    [SerializeField] private bool startEnraged;
    [SerializeField] private float rageSpeedMultiplier = 1.5f;
    [SerializeField] private float rageDamageMultiplier = 1.5f;

    [Header("Advanced Boss AI")]
    [SerializeField, Range(0f, 1f)]
    private float phaseTwoAt = 0.65f;

    [SerializeField, Range(0f, 1f)]
    private float phaseThreeAt = 0.30f;

    [SerializeField] private float strafeDistance = 7f;
    [SerializeField] private float strafeInterval = 3f;
    [SerializeField] private float dodgeCooldown = 8f;
    [SerializeField] private float dodgeDistance = 4f;
    [SerializeField] private bool debugBoss;

    // ==========================================
    // NEW: DISABLE BOSS AT 10% HEALTH
    // ==========================================

    [Header("Low Health - Disable Boss")]
    [SerializeField, Range(0f, 1f)]
    private float disableAtHealth = 0.10f;

    private bool isDown = false;

    // ==========================================

    private float trackedHealth;
    private int phase = 1;

    private float nextStrafe;
    private float nextDodge;

    private Vector3 strafePoint;
    private bool hasStrafePoint;

    private NavMeshAgent agent;
    private Entity playerEntity;

    private readonly List<GameObject> summons =
        new List<GameObject>();

    private readonly List<ParticleSystem> persistentVfx =
        new List<ParticleSystem>();

    private bool acting;
    private bool enraged;
    private bool defeated;
    private bool stunned;

    private float nextReactionTime;

    private Coroutine staggerRoutine;

    private float availableAt;
    private float nextAction;
    private float nextSummon;
    private float retryNav;

    // ==========================================
    // ENTITY EVENTS
    // ==========================================

    private void OnEnable()
    {
        OnDamageTaken += HandleBossDamage;
        OnDeath += HandleBossDeath;
    }

    private void OnDisable()
    {
        OnDamageTaken -= HandleBossDamage;
        OnDeath -= HandleBossDeath;
    }

    private void HandleBossDamage(float amount, object source)
    {
        NotifyDamageTaken(amount);
    }

    private void HandleBossDeath(object source)
    {
        NotifyBossDeath();
    }

    // ==========================================
    // START
    // ==========================================

    private void Start()
    {
        trackedHealth = Health;

        agent = GetComponent<NavMeshAgent>();

        agent.speed = speed;
        agent.acceleration = 12f;
        agent.angularSpeed = 480f;

        agent.stoppingDistance =
            Mathf.Max(0.5f, meleeRange - 0.35f);

        agent.updatePosition = true;
        agent.updateRotation = true;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        FindPlayer();
        SnapToNavMesh();

        availableAt = Time.time + Mathf.Max(0f, spawnWait);

        nextAction = availableAt + 0.5f;
        nextSummon = availableAt + summonInterval;

        if (agent.isOnNavMesh)
            agent.isStopped = true;

        PlayOnce(spawnVFX, transform.position);

        Animate("Spawn");

        if (startEnraged)
            ActivateRage();

        Animate("Stand");

        CheckLowHealth();
    }

    // ==========================================
    // NEW: LOW HEALTH
    // ==========================================

    private void CheckLowHealth()
    {
        if (isDown || defeated)
            return;

        if (Health > 0f &&
            Health <= MaxHealth * disableAtHealth)
        {
            DisableBoss();
        }
    }

    private void DisableBoss()
    {
        if (isDown)
            return;

        isDown = true;

        // Stop every current attack
        StopAllCoroutines();

        acting = false;
        stunned = false;
        staggerRoutine = null;

        // Disable navigation
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (agent != null && agent.enabled)
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            agent.enabled = false;
        }

        // Disable animations.
        // Existing ragdoll system is left untouched.
        if (animator != null)
        {
            animator.enabled = false;
        }

        if (debugBoss)
        {
            Debug.Log(
                "Boss reached 10% HP. " +
                "Animator, movement and attacks disabled."
            );
        }
    }

    // ==========================================
    // FIND PLAYER
    // ==========================================

    private void FindPlayer()
    {
        if (player == null)
        {
            GameObject obj =
                GameObject.FindGameObjectWithTag("Player");

            if (obj != null)
                player = obj.transform;
        }

        if (player != null)
            playerEntity = player.GetComponentInParent<Entity>();
    }

    private bool SnapToNavMesh()
    {
        if (agent == null || !agent.enabled)
            return false;

        if (agent.isOnNavMesh)
            return true;

        NavMeshHit hit;

        if (NavMesh.SamplePosition(
            transform.position,
            out hit,
            navMeshSnapRadius,
            agent.areaMask))
        {
            return agent.Warp(hit.position);
        }

        return false;
    }

    // ==========================================
    // UPDATE
    // ==========================================

    private void Update()
    {
        if (defeated || isDown)
            return;

        CheckLowHealth();

        if (isDown)
            return;

        if (player == null)
            FindPlayer();

        if (player == null)
            return;

        if (agent == null || !agent.enabled)
            return;

        if (!agent.isOnNavMesh)
        {
            if (Time.time >= retryNav)
            {
                retryNav = Time.time + 2f;
                SnapToNavMesh();
            }

            return;
        }

        if (Time.time < availableAt || acting || stunned)
        {
            StopAgent();
            return;
        }

        float playerDistance =
            Vector3.Distance(transform.position, player.position);

        // Dodge
        if (Time.time >= nextDodge &&
            playerDistance < 3.5f &&
            Random.value < 0.008f)
        {
            nextDodge = Time.time + dodgeCooldown;

            StartCoroutine(EvasiveStep());
            return;
        }

        // Movement speed based on phase
        agent.speed =
            speed *
            (enraged ? rageSpeedMultiplier : 1f) *
            (phase >= 3 ? 1.2f : 1f);

        summons.RemoveAll(s => s == null);

        // Summoning
        if (Time.time >= nextSummon &&
            skeletonPrefab != null &&
            summons.Count < maxLivingSummons)
        {
            nextSummon = Time.time + summonInterval;

            StartCoroutine(SummonAttack());
            return;
        }

        // Attack selection
        if (Time.time >= nextAction)
        {
            if (playerDistance > 6f &&
                playerDistance <= 25f &&
                fireballPrefab != null)
            {
                StartCoroutine(FireballVolley());
            }
            else if (playerDistance > meleeRange &&
                     playerDistance <= 11f &&
                     Random.value < 0.5f)
            {
                StartCoroutine(DashAttack());
            }
            else if (playerDistance <= slamRadius &&
                     Random.value <
                     (phase >= 2 ? 0.56f : 0.38f))
            {
                StartCoroutine(SlamAttack());
            }
            else if (playerDistance <= meleeRange)
            {
                StartCoroutine(ComboAttack());
            }
            else
            {
                Chase();
            }
        }
        else if (playerDistance > meleeRange)
        {
            if (playerDistance <= strafeDistance &&
                phase >= 2)
            {
                StrafeOrChase();
            }
            else
            {
                Chase();
            }
        }
        else
        {
            StopAgent();
            FaceTarget();
        }
    }

    // ==========================================
    // MOVEMENT
    // ==========================================

    private void StrafeOrChase()
    {
        if (isDown)
            return;

        if (Time.time >= nextStrafe ||
            !hasStrafePoint ||
            Vector3.Distance(transform.position, strafePoint) < 1f)
        {
            nextStrafe = Time.time + strafeInterval;

            Vector3 fromPlayer =
                transform.position - player.position;

            fromPlayer.y = 0f;

            Vector3 tangent =
                Vector3.Cross(
                    Vector3.up,
                    fromPlayer.normalized
                ) *
                (Random.value < 0.5f ? 1f : -1f);

            Vector3 candidate =
                player.position +
                fromPlayer.normalized *
                Mathf.Min(5f, strafeDistance) +
                tangent * 4f;

            NavMeshHit nav;

            hasStrafePoint =
                NavMesh.SamplePosition(
                    candidate,
                    out nav,
                    3f,
                    agent.areaMask
                );

            if (hasStrafePoint)
                strafePoint = nav.position;
        }

        if (!hasStrafePoint)
        {
            Chase();
            return;
        }

        agent.isStopped = false;
        agent.SetDestination(strafePoint);

        SetWalking(true);
    }

    private IEnumerator EvasiveStep()
    {
        if (isDown)
            yield break;

        acting = true;

        StopAgent();
        Animate("Dodge");

        Vector3 toward =
            player.position - transform.position;

        toward.y = 0f;

        Vector3 sideways =
            Vector3.Cross(Vector3.up, toward.normalized) *
            (Random.value < 0.5f ? 1f : -1f);

        NavMeshHit hit;

        Vector3 goal =
            transform.position + sideways * dodgeDistance;

        if (NavMesh.SamplePosition(
            goal,
            out hit,
            2f,
            agent.areaMask))
        {
            PlayOnce(dashVFX, transform.position);
            agent.Warp(hit.position);
        }

        yield return new WaitForSeconds(0.32f);

        MarkRecovery(0.5f);
    }

    private void Chase()
    {
        if (isDown || !agent.enabled || !agent.isOnNavMesh)
            return;

        agent.isStopped = false;
        agent.SetDestination(player.position);

        SetWalking(true);
    }

    private void StopAgent()
    {
        if (agent != null &&
            agent.enabled &&
            agent.isOnNavMesh)
        {
            agent.isStopped = true;

            if (agent.hasPath)
                agent.ResetPath();
        }

        SetWalking(false);
    }

    // ==========================================
    // ANIMATION
    // ==========================================

    private void SetWalking(bool walking)
    {
        if (isDown)
            return;

        if (animator != null &&
            animator.enabled &&
            HasParameter(
                "IsWalking",
                AnimatorControllerParameterType.Bool))
        {
            animator.SetBool("IsWalking", walking);
        }
    }

    private bool HasParameter(
        string parameter,
        AnimatorControllerParameterType type)
    {
        if (animator == null ||
            !animator.enabled ||
            animator.runtimeAnimatorController == null)
        {
            return false;
        }

        foreach (AnimatorControllerParameter p
                 in animator.parameters)
        {
            if (p.name == parameter && p.type == type)
                return true;
        }

        return false;
    }

    private void Animate(string parameter)
    {
        if (isDown)
            return;

        if (HasParameter(
            parameter,
            AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger(parameter);
        }
    }

    private void FaceTarget()
    {
        if (isDown || player == null)
            return;

        Vector3 flat =
            player.position - transform.position;

        flat.y = 0f;

        if (flat.sqrMagnitude > 0.001f)
        {
            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(flat),
                    Time.deltaTime * 10f
                );
        }
    }

    // ==========================================
    // DAMAGE
    // ==========================================

    private float Damage(float baseDamage)
    {
        return baseDamage *
            (enraged ? rageDamageMultiplier : 1f);
    }

    private void HitPlayer(
        float range,
        float damage,
        bool requireFront)
    {
        if (isDown || defeated || player == null)
            return;

        Vector3 flat =
            player.position - transform.position;

        flat.y = 0f;

        if (flat.magnitude > range)
            return;

        if (requireFront &&
            flat.sqrMagnitude > 0.001f &&
            Vector3.Angle(transform.forward, flat) > 80f)
        {
            return;
        }

        if (playerEntity == null)
            playerEntity = player.GetComponentInParent<Entity>();

        if (playerEntity != null)
        {
            playerEntity.TakeDamage(
                Damage(damage),
                this
            );
        }
    }

    // ==========================================
    // VFX
    // ==========================================

    private ParticleSystem PlayOnce(
        ParticleSystem prefab,
        Vector3 position)
    {
        if (prefab == null)
            return null;

        ParticleSystem effect =
            Instantiate(
                prefab,
                position,
                Quaternion.identity
            );

        effect.Play(true);

        Destroy(effect.gameObject, 7f);

        return effect;
    }

    private void MarkRecovery(float duration)
    {
        if (isDown)
            return;

        nextAction =
            Time.time +
            duration *
            (phase >= 3 ? 0.48f :
             phase == 2 ? 0.78f : 1f) *
            (enraged ? 0.8f : 1f);

        acting = false;
    }

    // ==========================================
    // ATTACK 1: COMBO
    // ==========================================

    private IEnumerator ComboAttack()
    {
        if (isDown)
            yield break;

        acting = true;

        StopAgent();
        Animate("Combo");

        for (int i = 0; i < 3; i++)
        {
            FaceTarget();

            yield return new WaitForSeconds(0.32f);

            if (defeated || isDown)
                yield break;

            PlayOnce(
                comboVFX,
                transform.position +
                transform.forward * 1.8f +
                Vector3.up * 1.1f
            );

            HitPlayer(
                meleeRange + 0.4f,
                comboHitDamage,
                true
            );

            yield return new WaitForSeconds(0.15f);
        }

        MarkRecovery(actionCooldown);
    }

    // ==========================================
    // ATTACK 2: SLAM
    // ==========================================

    private IEnumerator SlamAttack()
    {
        if (isDown)
            yield break;

        acting = true;

        StopAgent();
        FaceTarget();

        Animate("Slam");

        Vector3 impactOrigin =
            transform.position +
            transform.forward * groundSlamForwardOffset;

        Vector3 impactPoint =
            GroundPoint(impactOrigin);

        PlayOnce(summonVFX, impactPoint);

        yield return new WaitForSeconds(1.25f);

        if (defeated || isDown)
            yield break;

        if (groundSlamPrefab != null)
        {
            Instantiate(
                groundSlamPrefab,
                impactPoint,
                Quaternion.identity
            );
        }
        else
        {
            PlayOnce(
                slamImpactVFX != null ?
                slamImpactVFX : slamVFX,
                impactPoint
            );

            if (createFallbackShockwave)
            {
                StartCoroutine(
                    ShockwaveRing(
                        impactPoint + Vector3.up * 0.06f,
                        slamRadius
                    )
                );
            }

            HitPlayer(
                slamRadius,
                slamDamage,
                false
            );

            if (debugBoss)
            {
                Debug.LogWarning(
                    "Boss: Ground Slam prefab missing."
                );
            }
        }

        yield return new WaitForSeconds(0.35f);

        MarkRecovery(actionCooldown + 0.65f);
    }

    // ==========================================
    // ATTACK 3: FIREBALL
    // ==========================================

    private IEnumerator FireballVolley()
    {
        if (isDown)
            yield break;

        acting = true;

        StopAgent();
        Animate("Fireball");

        yield return new WaitForSeconds(0.65f);

        int spreadCount =
            phase >= 3 ? 5 : 3;

        for (int i = 0; i < spreadCount; i++)
        {
            if (defeated || isDown || player == null)
                yield break;

            FaceTarget();

            Vector3 origin =
                transform.position +
                Vector3.up * fireballHeight;

            Vector3 aim =
                (player.position - origin).normalized;

            Vector3 spread =
                Quaternion.AngleAxis(
                    (i - (spreadCount - 1) * 0.5f) * 12f,
                    Vector3.up
                ) * aim;

            FireballProjectile projectile =
                Instantiate(
                    fireballPrefab,
                    origin,
                    Quaternion.LookRotation(spread)
                );

            projectile.Initialize(
                this,
                player,
                spread,
                Damage(fireballDamage),
                projectileSpeed,
                fireballImpactPrefab,
                enraged ? 12f : 0f
            );

            yield return new WaitForSeconds(0.18f);
        }

        MarkRecovery(actionCooldown + 0.5f);
    }

    // ==========================================
    // ATTACK 4: DASH
    // ==========================================

    private IEnumerator DashAttack()
    {
        if (isDown)
            yield break;

        acting = true;

        StopAgent();
        Animate("Dash");

        FaceTarget();

        yield return new WaitForSeconds(0.6f);

        if (defeated || isDown || player == null)
            yield break;

        Vector3 heading =
            player.position - transform.position;

        heading.y = 0f;
        heading.Normalize();

        HashSet<Entity> hit =
            new HashSet<Entity>();

        float elapsed = 0f;

        while (elapsed < 0.42f &&
               agent.enabled &&
               agent.isOnNavMesh &&
               !isDown)
        {
            float dt = Time.deltaTime;

            elapsed += dt;

            agent.Move(heading * 13f * dt);

            if (Random.value < 0.4f)
                PlayOnce(dashVFX, transform.position);

            if (playerEntity == null && player != null)
                playerEntity =
                    player.GetComponentInParent<Entity>();

            if (playerEntity != null &&
                !hit.Contains(playerEntity) &&
                Vector3.Distance(
                    transform.position,
                    player.position) < 2f)
            {
                hit.Add(playerEntity);

                playerEntity.TakeDamage(
                    Damage(dashDamage),
                    this
                );
            }

            yield return null;
        }

        MarkRecovery(actionCooldown);
    }

    // ==========================================
    // ATTACK 5: SUMMON
    // ==========================================

    private IEnumerator SummonAttack()
    {
        if (isDown)
            yield break;

        acting = true;

        StopAgent();
        Animate("Summon");

        int slots =
            Mathf.Max(
                0,
                maxLivingSummons - summons.Count
            );

        int minCount =
            Mathf.Min(minimumSummons, maximumSummons);

        int maxCount =
            Mathf.Max(minimumSummons, maximumSummons);

        int count =
            Mathf.Min(
                Random.Range(minCount, maxCount + 1),
                slots
            );

        yield return new WaitForSeconds(0.5f);

        if (isDown || defeated)
            yield break;

        for (int i = 0; i < count; i++)
        {
            float a =
                Mathf.PI * 2f * i /
                Mathf.Max(1, count);

            Vector3 desired =
                transform.position +
                new Vector3(
                    Mathf.Cos(a),
                    0f,
                    Mathf.Sin(a)
                ) * summonRadius;

            NavMeshHit navHit;

            if (NavMesh.SamplePosition(
                desired,
                out navHit,
                3f,
                agent.areaMask))
            {
                PlayOnce(summonVFX, navHit.position);

                yield return new WaitForSeconds(0.4f);

                if (defeated || isDown)
                    yield break;

                GameObject skeleton =
                    Instantiate(
                        skeletonPrefab,
                        navHit.position,
                        Quaternion.identity
                    );

                summons.Add(skeleton);
            }
        }

        MarkRecovery(actionCooldown + 1f);
    }

    // ==========================================
    // HEALTH / PHASES
    // ==========================================

    public void NotifyDamageTaken(float amount)
    {
        if (defeated || isDown || amount <= 0f)
            return;

        trackedHealth = Health;

        // NEW: 10% health check
        CheckLowHealth();

        if (isDown)
            return;

        if (trackedHealth > 0f &&
            useDamageReactions &&
            Time.time >= nextReactionTime)
        {
            nextReactionTime =
                Time.time + reactionCooldown;

            if (amount >= staggerThreshold &&
                !stunned)
            {
                if (staggerRoutine != null)
                    StopCoroutine(staggerRoutine);

                staggerRoutine =
                    StartCoroutine(StaggerRoutine());
            }
            else if (!acting && !stunned)
            {
                Animate(
                    amount >= heavyHitThreshold ?
                    "DamageHeavy" :
                    "DamageLight"
                );
            }
        }

        float ratio =
            trackedHealth / Mathf.Max(1f, MaxHealth);

        if (ratio <= phaseThreeAt && phase < 3)
        {
            phase = 3;

            ActivateRage();

            nextAction = Time.time + 0.8f;

            if (debugBoss)
                Debug.Log("Inferno Boss: PHASE 3");
        }
        else if (ratio <= phaseTwoAt && phase < 2)
        {
            phase = 2;

            nextAction = Time.time + 0.8f;

            PlayOnce(rageVFX, transform.position);

            if (debugBoss)
                Debug.Log("Inferno Boss: PHASE 2");
        }

        if (trackedHealth <= 0f)
            NotifyBossDeath();
    }

    // ==========================================
    // RAGE
    // ==========================================

    public void ActivateRage()
    {
        if (enraged || defeated || isDown)
            return;

        enraged = true;

        Animate("Rage");

        if (rageVFX != null)
        {
            ParticleSystem aura =
                Instantiate(
                    rageVFX,
                    transform.position,
                    Quaternion.identity,
                    transform
                );

            aura.Play(true);

            persistentVfx.Add(aura);
        }
    }

    // ==========================================
    // DEATH
    // ==========================================

    public void NotifyBossDeath()
    {
        if (defeated)
            return;

        defeated = true;

        stunned = false;
        acting = false;

        StopAllCoroutines();

        StopAgent();

        Animate("Die");

        foreach (ParticleSystem ps in persistentVfx)
        {
            if (ps != null)
                Destroy(ps.gameObject);
        }
    }

    // ==========================================
    // STAGGER
    // ==========================================

    private IEnumerator StaggerRoutine()
    {
        if (isDown)
            yield break;

        stunned = true;

        StopAgent();
        Animate("Stagger");

        yield return new WaitForSeconds(
            Mathf.Max(0.1f, staggerDuration)
        );

        stunned = false;
        staggerRoutine = null;
    }

    public void PlayKnockdownAnimation()
    {
        if (defeated || isDown)
            return;

        Animate("Knockdown");
    }

    public void PlayGetUpAnimation()
    {
        if (defeated || isDown)
            return;

        Animate("GetUp");
    }

    // ==========================================
    // GROUND POINT
    // ==========================================

    private Vector3 GroundPoint(Vector3 origin)
    {
        if (Physics.Raycast(
            origin + Vector3.up * 2f,
            Vector3.down,
            out RaycastHit groundHit,
            15f,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore))
        {
            return groundHit.point;
        }

        if (NavMesh.SamplePosition(
            origin,
            out NavMeshHit navHit,
            3f,
            agent.areaMask))
        {
            return navHit.position;
        }

        return origin;
    }

    // ==========================================
    // SHOCKWAVE
    // ==========================================

    private IEnumerator ShockwaveRing(
        Vector3 origin,
        float maximumRadius)
    {
        GameObject ring =
            new GameObject(
                "Boss_GroundSlam_Shockwave"
            );

        ring.transform.position = origin;

        LineRenderer line =
            ring.AddComponent<LineRenderer>();

        line.useWorldSpace = false;
        line.loop = true;

        line.positionCount =
            Mathf.Max(16, shockwaveSegments);

        line.startWidth = 0.35f;
        line.endWidth = 0.35f;
        line.numCornerVertices = 2;

        Material material = null;

        Shader shader =
            Shader.Find(
                "Universal Render Pipeline/Unlit"
            );

        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        if (shader != null)
        {
            material = new Material(shader);

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor(
                    "_BaseColor",
                    new Color(1f, 0.25f, 0.015f, 1f)
                );
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor(
                    "_Color",
                    new Color(1f, 0.25f, 0.015f, 1f)
                );
            }

            line.material = material;
        }

        float length =
            Mathf.Max(0.05f, shockwaveDuration);

        float time = 0f;

        while (time < length &&
               !defeated &&
               !isDown)
        {
            time += Time.deltaTime;

            float t =
                Mathf.Clamp01(time / length);

            float radius =
                Mathf.Lerp(
                    0.25f,
                    maximumRadius,
                    t
                );

            line.widthMultiplier =
                Mathf.Lerp(1f, 0.02f, t);

            int count = line.positionCount;

            for (int i = 0; i < count; i++)
            {
                float angle =
                    i * 2f * Mathf.PI / count;

                line.SetPosition(
                    i,
                    new Vector3(
                        Mathf.Cos(angle) * radius,
                        0f,
                        Mathf.Sin(angle) * radius
                    )
                );
            }

            yield return null;
        }

        Destroy(ring);

        if (material != null)
            Destroy(material);
    }

    // ==========================================
    // GIZMOS
    // ==========================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            meleeRange
        );

        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            slamRadius
        );

        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(
            transform.position,
            summonRadius
        );

        if (showFireballGizmos)
        {
            Vector3 origin =
                transform.position +
                Vector3.up * fireballHeight;

            Gizmos.DrawLine(
                origin,
                origin + transform.forward * 2f
            );
        }
    }
}
