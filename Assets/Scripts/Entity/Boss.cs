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
    [SerializeField] private float speed = 5.4f;
    [SerializeField] private float spawnWait = 2f;
    [SerializeField] private float navMeshSnapRadius = 5f;
    [SerializeField] private float meleeRange = 3f;

    [Header("Damage / timings")]
    [SerializeField] private float comboHitDamage = 19f;
    [SerializeField] private float slamDamage = 48f;
    [SerializeField] private float slamRadius = 6.5f;
    [SerializeField] private float dashDamage = 34f;
    [SerializeField] private float fireballDamage = 28f;
    [SerializeField] private float projectileSpeed = 24f;
    [SerializeField] private float actionCooldown = 0.9f;

    [Header("Skeletons")]
    [SerializeField] private GameObject skeletonPrefab;
    [SerializeField] private int minimumSummons = 3;
    [SerializeField] private int maximumSummons = 5;
    [SerializeField] private int maxLivingSummons = 12;
    [SerializeField] private float summonInterval = 13f;
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
    [SerializeField] private float staggerDuration = 0.55f;
    [SerializeField] private float reactionCooldown = 2.5f;

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

    [Header("HARDCORE - Difficulty")]
    [SerializeField] private bool applyHardcoreStatsAtRuntime = true;

    [Header("HARDCORE - Anti Air / Predictive AI")]
    [SerializeField] private bool enableAntiAir = true;
    [SerializeField] private float airborneHeight = 1.35f;
    [SerializeField] private float antiAirRange = 22f;
    [SerializeField] private float antiAirDamage = 42f;
    [SerializeField] private float antiAirRadius = 3.2f;
    [SerializeField] private float antiAirWarning = 0.48f;
    [SerializeField] private float antiAirCooldown = 3.7f;
    [SerializeField] private float predictionSeconds = 0.45f;
    [SerializeField] private float maxPredictedSpeed = 13f;
    [SerializeField] private float dashSpeed = 23f;
    [SerializeField] private float dashSteering = 9f;

    [Header("DEATH BEAMS - Sky to Ground")]
    [SerializeField] private bool enableDeathBeams = true;
    [SerializeField] private int beamCount = 2;
    [SerializeField] private int rageBeamCount = 5;
    [SerializeField] private float beamCooldown = 18f;
    [SerializeField] private float beamDamage = 17f;
    [SerializeField] private float beamHeight = 24f;
    [SerializeField] private float beamRadius = 0.85f;
    [SerializeField] private float beamDuration = 5f;
    [SerializeField] private float beamWarning = 0.8f;
    [SerializeField] private float beamFollowSpeed = 5.5f;
    [SerializeField] private float beamTickInterval = 0.65f;
    [SerializeField] private float beamSpread = 3.2f;
    [SerializeField] private Color beamColor = new Color(1f, 0.06f, 0.02f, 1f);

    [Header("DEATH BEAMS - Homing Explosions")]
    [SerializeField] private bool enableBeamExplosions = true;
    [SerializeField] private float beamExplosionInterval = 1.6f;
    [SerializeField] private float beamExplosionSpeed = 12f;
    [SerializeField] private float beamExplosionDamage = 27f;
    [SerializeField] private float beamExplosionRadius = 2.7f;
    [SerializeField] private float beamExplosionLifetime = 4f;
    [SerializeField] private float beamExplosionSize = 0.7f;
    [SerializeField] private float beamExplosionSteering = 7f;

    [Header("FIREBALL RING - Radial Barrage")]
    [SerializeField] private bool enableFireballRing = true;
    [SerializeField] private float fireballRingCooldown = 8f;
    [SerializeField] private int ringFireballCount = 12;
    [SerializeField] private int rageRingFireballCount = 22;
    [SerializeField] private float ringFireballSpeed = 22f;
    [SerializeField] private float ringSpawnRadius = 2.5f;
    [SerializeField] private float ringWindup = 0.7f;

    [Header("RAGE CHAOS - Homing Knives")]
    [SerializeField] private bool enableRageKnifeStorm = true;
    [Tooltip("Optional knife model prefab. Its forward (blue Z) points toward the tip.")]
    [SerializeField] private GameObject knifePrefab;
    [SerializeField] private float rageKnifeInterval = 2.2f;
    [SerializeField] private int rageKnivesPerVolley = 8;
    [SerializeField] private float rageKnifeSpeed = 23f;
    [SerializeField] private float rageKnifeDamage = 15f;
    [SerializeField] private float rageKnifeLifetime = 4.5f;
    [SerializeField] private float rageKnifeTurnSpeed = 8f;
    [SerializeField] private float rageKnifeHitRadius = 0.9f;
    [SerializeField] private float rageKnifeLaunchDelay = 0.33f;
    [SerializeField] private float rageKnifeSpreadRadius = 3f;
    [Header("RAGE CHAOS - More Pressure")]
    [SerializeField] private float rageBeamCooldownMultiplier = 0.8f;
    [SerializeField] private float rageRingCooldownMultiplier = 0.5f;
    [SerializeField] private float rageSummonIntervalMultiplier = 0.65f;
    private float nextKnifeStorm;
    private readonly List<GameObject> activeKnifeVisuals = new List<GameObject>();

    [Header("RANDOM SKY BEAMS - Chase Player")]
    [SerializeField] private float randomBeamSpawnMin = 4f;
    [SerializeField] private float randomBeamSpawnMax = 13f;
    [SerializeField] private float randomBeamChaseDelay = 0.55f;
    [SerializeField] private float rageBeamFollowMultiplier = 1.7f;

    [Header("LASER CIRCLE - Adjustable Radius")]
    [SerializeField, Min(1f)] private float laserCircleRadius = 9f;
    [SerializeField, Range(0f, 1f)] private float laserCircleRandomness = 0.15f;
    [SerializeField] private bool laserCircleCenteredOnPlayer = true;

    [Header("KNIVES - Player Height")]
    [SerializeField] private float knifeHeightOffset = 0.85f;
    [SerializeField] private bool knifeFollowPlayerHeightDuringWindup = true;

    [Header("GROUND EXPLOSIONS - Large Radius")]
    [SerializeField] private bool enableGroundExplosions = true;
    [SerializeField, Min(1f)] private float groundExplosionRadius = 6f;
    [SerializeField] private float groundExplosionDamage = 48f;
    [SerializeField] private float groundExplosionCooldown = 9f;
    [SerializeField] private float groundExplosionWarning = 1.15f;
    [SerializeField] private int groundExplosionRageWaves = 3;
    [SerializeField] private float groundExplosionWaveDelay = 0.8f;
    [SerializeField] private float groundExplosionMaxRange = 32f;
    [SerializeField] private float groundExplosionVerticalReach = 9f;
    private float nextGroundExplosion;

    [Header("SKELETON HAND - Phase 2+")]
    [SerializeField] private bool enableSkeletonHand = true;
    [SerializeField] private SkeletonHandProjectile skeletonHandPrefab;
    [SerializeField] private float skeletonHandCooldown = 14f;
    [SerializeField] private float skeletonHandMinRange = 6f;
    [SerializeField] private float skeletonHandMaxRange = 28f;
    [SerializeField] private float skeletonHandWindup = 0.9f;
    [SerializeField] private float skeletonHandSpeed = 18f;
    [SerializeField] private float skeletonHandDamage = 20f;
    [SerializeField] private float skeletonHandHitRadius = 1.5f;
    [SerializeField] private float skeletonHandLifetime = 3f;
    [SerializeField] private float playerStunDuration = 1.5f;

    private float nextSkeletonHand;
    private Coroutine playerStunRoutine;

    [Header("HELL TRAP - Simultaneous Ultimates")]
    [SerializeField] private bool enableHellTraps = true;
    [SerializeField, Range(1, 5)] private int hellTrapCount = 2;
    [SerializeField, Range(1, 6)] private int rageHellTrapCount = 3;
    [SerializeField, Range(4, 16)] private int hellTrapPillars = 8;
    [SerializeField, Min(3f)] private float hellTrapRadius = 11f;
    [SerializeField, Min(1f)] private float hellTrapSpacing = 7f;
    [SerializeField, Min(1f)] private float hellTrapPillarHeight = 15f;

    [Header("HELL TRAP - Spear Model")]
    [Tooltip("Префаб копья для столбов ловушки. Если пусто, используются старые красные лучи.")]
    [SerializeField] private GameObject hellTrapSpearPrefab;
    [Tooltip("Дополнительный поворот модели копья.")]
    [SerializeField] private Vector3 hellTrapSpearRotationOffset = Vector3.zero;
    [Tooltip("Смещение модели копья вверх или вниз относительно поверхности пола.")]
    [SerializeField] private float hellTrapSpearVerticalOffset = 0f;

    [SerializeField] private float hellTrapShrinkTime = 3.5f;
    [SerializeField] private float hellTrapWarning = 1.2f;
    [SerializeField] private float hellTrapCooldown = 28f;
    [SerializeField] private float hellTrapPillarDamage = 12f;
    [SerializeField] private float hellTrapFinalDamage = 65f;
    [SerializeField] private float hellTrapFinalRadius = 5.5f;
    [SerializeField] private int hellTrapMeteorCount = 5;
    [SerializeField] private int rageHellTrapMeteorCount = 9;
    [SerializeField] private float hellTrapMeteorRadius = 2.3f;
    [SerializeField] private float hellTrapMeteorDamage = 20f;
    [SerializeField] private float hellTrapMeteorWarning = .75f;
    private float nextHellTrap;

    // Fair rotation: attacks that have not been used recently have higher priority.
    private readonly float[] attackLastUsed = new float[6];
    private int lastAttackType = -1;
    private float nextBeamAttack;
    private float nextRingAttack;

    // Король может двигаться, пока активны гигантские лучи.
    private bool allowMovementDuringDeathBeams;
    private readonly List<GameObject> beamVisuals = new List<GameObject>();

    private Vector3 previousPlayerPosition;
    private Vector3 playerVelocity;
    private bool hasPlayerSample;
    private float nextAntiAir;

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


    [Header("SKELETON HAND - Player Stun")]
    [SerializeField] private MonoBehaviour[] playerMovementBehaviours;

    private Coroutine playerStunCoroutine;
    private bool[] previousMovementStates;

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
        ClearBeamVisuals();
        ClearKnifeVisuals();
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

        // Overrides previous Inspector-serialized values on existing prefabs.
        // Disable this checkbox to tune every stat manually instead.
        if (applyHardcoreStatsAtRuntime)
        {
            speed = 5.4f;
            comboHitDamage = 19f;
            slamDamage = 48f;
            slamRadius = 6.5f;
            dashDamage = 34f;
            fireballDamage = 28f;
            projectileSpeed = 24f;
            actionCooldown = 0.9f;
            summonInterval = 13f;
            staggerDuration = 0.55f;
            reactionCooldown = 2.5f;
            minimumSummons = 4;
            maximumSummons = 6;
            rageSpeedMultiplier = 1.65f;
            rageDamageMultiplier = 1.6f;
        }

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
        if (player != null)
        {
            previousPlayerPosition = player.position;
            hasPlayerSample = true;
        }
        SnapToNavMesh();

        availableAt = Time.time + Mathf.Max(0f, spawnWait);

        nextAction = availableAt + 0.5f;
        nextSummon = availableAt + summonInterval;
        nextKnifeStorm = availableAt + 1.4f;
        nextBeamAttack = availableAt + 10f;
        for (int i = 0; i < attackLastUsed.Length; i++)
            attackLastUsed[i] = -100f;
        nextRingAttack = availableAt + 2.5f;
        nextGroundExplosion = availableAt + 5f;
        nextHellTrap = availableAt + 16f;
        nextSkeletonHand = availableAt + 6f;

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
        ClearBeamVisuals();
        ClearKnifeVisuals();

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

    public void ApplySkeletonHandStun(float duration)
    {
        if (isDown || defeated)
            return;

        // Если оглушение уже действует, не запускаем его повторно.
        if (playerStunCoroutine != null)
            StopCoroutine(playerStunCoroutine);

        playerStunCoroutine = StartCoroutine(PlayerStunRoutine(duration));
    }

    private IEnumerator PlayerStunRoutine(float duration)
    {
        if (playerMovementBehaviours == null ||
            playerMovementBehaviours.Length == 0)
        {
            playerStunCoroutine = null;
            yield break;
        }

        previousMovementStates =
            new bool[playerMovementBehaviours.Length];

        for (int i = 0; i < playerMovementBehaviours.Length; i++)
        {
            MonoBehaviour behaviour = playerMovementBehaviours[i];

            if (behaviour == null)
                continue;

            previousMovementStates[i] = behaviour.enabled;
            behaviour.enabled = false;
        }

        yield return new WaitForSeconds(Mathf.Max(0.1f, duration));

        RestorePlayerMovement();
    }

    private void RestorePlayerMovement()
    {
        if (playerMovementBehaviours != null &&
            previousMovementStates != null)
        {
            for (int i = 0;
                 i < playerMovementBehaviours.Length;
                 i++)
            {
                MonoBehaviour behaviour = playerMovementBehaviours[i];

                if (behaviour == null)
                    continue;

                behaviour.enabled = previousMovementStates[i];
            }
        }

        previousMovementStates = null;
        playerStunCoroutine = null;
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

        SamplePlayerMotion();

        // Knife storms are independent of the main attack animation.
        // They continue while the boss casts beams or other attacks.
        if (enraged && enableRageKnifeStorm &&
            Time.time >= nextKnifeStorm &&
            FlatDistance(transform.position, player.position) <= 38f)
        {
            nextKnifeStorm = Time.time + Mathf.Max(.6f, rageKnifeInterval);
            StartCoroutine(RageKnifeStorm());
        }

        // Multiple hell traps spawn together, without freezing the main combat AI.
        if (enableHellTraps && Time.time >= nextHellTrap &&
            FlatDistance(transform.position, player.position) <= 34f)
        {
            nextHellTrap = Time.time + Mathf.Max(12f, hellTrapCooldown * (enraged ? .8f : 1f));
            StartCoroutine(SimultaneousHellTraps());
        }

        // Ground explosions are independent; no interruption of other boss abilities.
        if (enableGroundExplosions && Time.time >= nextGroundExplosion &&
            FlatDistance(transform.position, player.position) <= groundExplosionMaxRange)
        {
            nextGroundExplosion = Time.time + Mathf.Max(3f, groundExplosionCooldown * (enraged ? .75f : 1f));
            StartCoroutine(GroundExplosionAttack());
        }

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

        // Во время гигантских лучей разрешаем королю преследовать игрока.
        // Остальные атаки по-прежнему блокируют движение.
        if (allowMovementDuringDeathBeams && !stunned)
        {
            agent.speed =
                speed *
                (enraged ? rageSpeedMultiplier : 1f) *
                (phase >= 3 ? 1.2f : 1f);

            Chase();
            return;
        }

        if (Time.time < availableAt || acting || stunned)
        {
            StopAgent();
            return;
        }

        float playerDistance = FlatDistance(transform.position, player.position);

        // SKELETON HAND — Phase 2+
        if (enableSkeletonHand &&
            phase >= 2 &&
            skeletonHandPrefab != null &&
            Time.time >= nextSkeletonHand &&
            playerDistance >= skeletonHandMinRange &&
            playerDistance <= skeletonHandMaxRange)
        {
            nextSkeletonHand = Time.time + skeletonHandCooldown;
            StartCoroutine(SkeletonHandAttack());
            return;
        }

        // Repeated jumping is dangerous: an air burst targets the predicted landing area.
        if (enableAntiAir && IsPlayerAirborne() &&
            playerDistance <= antiAirRange && Time.time >= nextAntiAir &&
            Time.time >= nextAction && Random.value < 0.28f)
        {
            nextAntiAir = Time.time + antiAirCooldown;
            StartCoroutine(AntiAirAttack());
            return;
        }

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
            nextSummon = Time.time + summonInterval * (enraged ? rageSummonIntervalMultiplier : 1f);

            StartCoroutine(SummonAttack());
            return;
        }

        // Balanced rotation of ALL abilities. Beams cannot dominate the fight.
        if (Time.time >= nextAction)
        {
            if (!TryBalancedAttack(playerDistance))
                Chase();
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

    // IDs: 0 beams, 1 fire ring, 2 volley, 3 dash, 4 slam, 5 combo.
    private bool TryBalancedAttack(float distance)
    {
        List<int> options = new List<int>();
        if (enableDeathBeams && distance <= 35f && Time.time >= nextBeamAttack)
            options.Add(0);
        if (enableFireballRing && fireballPrefab != null && distance <= 22f &&
            Time.time >= nextRingAttack)
            options.Add(1);
        if (fireballPrefab != null && distance <= 32f && distance >= 3.5f)
            options.Add(2);
        if (distance > meleeRange * .7f && distance <= 17f)
            options.Add(3);
        if (distance <= slamRadius + 1.5f)
            options.Add(4);
        if (distance <= meleeRange + 1f)
            options.Add(5);

        if (options.Count == 0) return false;

        // Prefer a move not used recently, but keep a little unpredictability.
        int picked = -1;
        float highestScore = float.NegativeInfinity;
        foreach (int type in options)
        {
            float age = Mathf.Clamp(Time.time - attackLastUsed[type], 0f, 18f);
            float score = age + Random.Range(0f, 3f);
            if (type == lastAttackType && options.Count > 1) score -= 30f;
            // Fireball volley and mobility moves should remain frequent.
            if (type == 2 || type == 3) score += 1.5f;
            if (score > highestScore) { highestScore = score; picked = type; }
        }

        if (picked < 0) return false;
        lastAttackType = picked;
        attackLastUsed[picked] = Time.time;

        switch (picked)
        {
            case 0:
                nextBeamAttack = Time.time + Mathf.Max(13f,
                    beamCooldown * (enraged ? rageBeamCooldownMultiplier : 1f));
                StartCoroutine(DeathBeamAttack());
                break;
            case 1:
                nextRingAttack = Time.time + fireballRingCooldown *
                    (enraged ? rageRingCooldownMultiplier : 1f);
                StartCoroutine(FireballRingAttack());
                break;
            case 2: StartCoroutine(FireballVolley()); break;
            case 3: StartCoroutine(DashAttack()); break;
            case 4: StartCoroutine(SlamAttack()); break;
            case 5: StartCoroutine(ComboAttack()); break;
        }
        return true;
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
    // HARDCORE: PLAYER TRACKING + ANTI-AIR
    // ==========================================

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void SamplePlayerMotion()
    {
        if (player == null) return;
        if (!hasPlayerSample)
        {
            previousPlayerPosition = player.position;
            hasPlayerSample = true;
            return;
        }
        float dt = Mathf.Max(0.001f, Time.deltaTime);
        Vector3 measured = (player.position - previousPlayerPosition) / dt;
        measured = Vector3.ClampMagnitude(measured, maxPredictedSpeed);
        playerVelocity = Vector3.Lerp(playerVelocity, measured,
            Mathf.Clamp01(dt * 12f));
        previousPlayerPosition = player.position;
    }

    private Vector3 PredictPlayer(float seconds)
    {
        if (player == null) return transform.position;
        Vector3 travel = playerVelocity * Mathf.Clamp(seconds, 0f, 1.25f);
        travel = Vector3.ClampMagnitude(travel, maxPredictedSpeed * 1.25f);
        return player.position + travel;
    }

    private bool IsPlayerAirborne()
    {
        if (player == null) return false;
        // Check floor under feet and ignore colliders belonging to the player.
        Vector3 from = player.position + Vector3.up * 0.15f;
        RaycastHit[] hits = Physics.RaycastAll(from, Vector3.down,
            airborneHeight + 0.15f, Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
        {
            if (hit.transform == player || hit.transform.IsChildOf(player)) continue;
            if (hit.distance <= airborneHeight + 0.15f) return false;
        }
        return true;
    }

    private IEnumerator AntiAirAttack()
    {
        acting = true;
        StopAgent();
        Animate("Fireball");

        // Lock target early: player has time to change direction and evade.
        Vector3 target = PredictPlayer(predictionSeconds);
        target = GroundPoint(target);
        PlayOnce(summonVFX, target);
        yield return new WaitForSeconds(Mathf.Max(0.15f, antiAirWarning));

        if (defeated || isDown || player == null) yield break;
        PlayOnce(slamImpactVFX != null ? slamImpactVFX : slamVFX, target);
        if (createFallbackShockwave)
            StartCoroutine(ShockwaveRing(target + Vector3.up * 0.08f, antiAirRadius));

        if (FlatDistance(player.position, target) <= antiAirRadius &&
            Mathf.Abs(player.position.y - target.y) <= 7f)
        {
            if (playerEntity == null) playerEntity = player.GetComponentInParent<Entity>();
            if (playerEntity != null)
                playerEntity.TakeDamage(Damage(antiAirDamage), this);
        }
        MarkRecovery(0.45f);
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

        if (flat.magnitude > range ||
            Mathf.Abs(player.position.y - transform.position.y) > 5f)
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

        for (int i = 0; i < (phase >= 3 ? 5 : phase == 2 ? 4 : 3); i++)
        {
            FaceTarget();

            yield return new WaitForSeconds(phase >= 2 ? 0.20f : 0.27f);

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

        yield return new WaitForSeconds(phase >= 3 ? 0.65f : 0.9f);

        if (defeated || isDown)
            yield break;

        if (groundSlamPrefab != null)
        {
            // Ground-slam prefab handles ground hits; add aerial impact against jump spam.
            if (IsPlayerAirborne() &&
                FlatDistance(impactPoint, player.position) <= slamRadius &&
                Mathf.Abs(player.position.y - impactPoint.y) <= 5f)
                HitPlayer(slamRadius, slamDamage * 0.75f, false);

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

    private IEnumerator SkeletonHandAttack()
    {
        if (isDown || defeated || player == null ||
            skeletonHandPrefab == null)
            yield break;

        acting = true;
        StopAgent();
        FaceTarget();

        // Телеграф атаки: босс предупреждает игрока.
        Animate("Slam");

        yield return new WaitForSeconds(
            Mathf.Max(0.2f, skeletonHandWindup));

        if (isDown || defeated || player == null)
        {
            acting = false;
            yield break;
        }

        FaceTarget();

        Vector3 spawnPosition =
            transform.position +
            transform.forward * 1.8f +
            Vector3.up * 1.2f;

        Vector3 direction =
            (player.position + Vector3.up - spawnPosition).normalized;

        SkeletonHandProjectile hand = Instantiate(
            skeletonHandPrefab,
            spawnPosition,
            Quaternion.LookRotation(direction));

        hand.Initialize(
            this,
            player,
            Damage(skeletonHandDamage),
            skeletonHandSpeed,
            skeletonHandHitRadius,
            skeletonHandLifetime,
            playerStunDuration);

        MarkRecovery(actionCooldown + 0.8f);
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

        yield return new WaitForSeconds(phase >= 2 ? 0.32f : 0.46f);

        int spreadCount =
            phase >= 3 ? 7 : phase == 2 ? 5 : 4;

        for (int i = 0; i < spreadCount; i++)
        {
            if (defeated || isDown || player == null)
                yield break;

            FaceTarget();

            Vector3 origin =
                transform.position +
                Vector3.up * fireballHeight;

            float travel = Vector3.Distance(origin, player.position) /
                Mathf.Max(1f, projectileSpeed);
            Vector3 predicted = PredictPlayer(Mathf.Min(1.1f, travel));
            Vector3 aim = (predicted + Vector3.up * 0.9f - origin).normalized;

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

            yield return new WaitForSeconds(phase >= 3 ? 0.10f : 0.14f);
        }

        MarkRecovery(actionCooldown + 0.5f);
    }

    // ==========================================
    // DEATH BEAMS: independent tracking pillars from the sky
    // ==========================================

    private Material MakeBeamMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) return null;
        Material mat = new Material(shader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
        return mat;
    }

    private GameObject CreateBeamVisual(string name, Color color, float width)
    {
        GameObject obj = new GameObject(name);
        beamVisuals.Add(obj);
        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.numCapVertices = 8;
        lr.material = MakeBeamMaterial(color);
        return obj;
    }

    private void RemoveBeamVisual(GameObject obj)
    {
        if (obj == null) return;
        beamVisuals.Remove(obj);
        LineRenderer lr = obj.GetComponent<LineRenderer>();
        if (lr != null && lr.material != null) Destroy(lr.material);
        Destroy(obj);
    }

    private void ClearBeamVisuals()
    {
        foreach (GameObject obj in beamVisuals)
        {
            if (obj == null) continue;
            LineRenderer lr = obj.GetComponent<LineRenderer>();
            if (lr != null && lr.material != null) Destroy(lr.material);
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null && lr == null && renderer.material != null) Destroy(renderer.material);
            Destroy(obj);
        }
        beamVisuals.Clear();
    }

    private IEnumerator DeathBeamAttack()
    {
        acting = true;

        // Не останавливаем NavMeshAgent: король продолжает преследовать игрока.
        allowMovementDuringDeathBeams = true;

        Animate("Slam");
        int count = enraged ? rageBeamCount : beamCount;
        count = Mathf.Clamp(count, 1, 12);
        Vector3[] centers = new Vector3[count];
        Vector3[] offsets = new Vector3[count];
        GameObject[] pillars = new GameObject[count];
        GameObject[] warnings = new GameObject[count];
        float[] nextHit = new float[count];

        // Evenly spaced circle around the player (or the boss), adjustable in Inspector.
        // A small angular variation keeps attacks less predictable.
        Vector3 circleOrigin = laserCircleCenteredOnPlayer ? PredictPlayer(0.25f) : transform.position;
        float circleRotation = Random.Range(0f, Mathf.PI * 2f);
        for (int i = 0; i < count; i++)
        {
            float angle = circleRotation + i * Mathf.PI * 2f / count +
                Random.Range(-laserCircleRandomness, laserCircleRandomness);
            float radius = Mathf.Max(1f, laserCircleRadius);
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            centers[i] = GroundPoint(circleOrigin + direction * radius);
            float targetAngle = angle + Mathf.PI;
            offsets[i] = new Vector3(Mathf.Cos(targetAngle), 0f,
                Mathf.Sin(targetAngle)) * (count == 2 ? 1.3f : beamSpread * .5f);
            warnings[i] = CreateBeamVisual("DeathBeam_Warning", new Color(1f, .55f, .03f), .18f);
            LineRenderer warningLine = warnings[i].GetComponent<LineRenderer>();
            warningLine.SetPosition(0, centers[i] + Vector3.up * .04f);
            warningLine.SetPosition(1, centers[i] + Vector3.up * beamHeight);
        }
        yield return new WaitForSeconds(Mathf.Max(.15f, beamWarning));
        for (int i = 0; i < count; i++) RemoveBeamVisual(warnings[i]);
        if (isDown || defeated || player == null)
        {
            allowMovementDuringDeathBeams = false;
            yield break;
        }

        for (int i = 0; i < count; i++)
        {
            pillars[i] = CreateBeamVisual("DEATH_BEAM_" + i, beamColor, beamRadius * 2f);
            PlayOnce(slamImpactVFX != null ? slamImpactVFX : slamVFX, centers[i]);
        }

        float elapsed = 0f;
        float nextExplosion = Time.time + 0.65f;
        while (elapsed < beamDuration && !defeated && !isDown && player != null)
        {
            float dt = Time.deltaTime;
            elapsed += dt;
            Vector3 predicted = PredictPlayer(.22f);
            for (int i = 0; i < count; i++)
            {
                Vector3 destination = GroundPoint(predicted + offsets[i]);
                if (elapsed >= randomBeamChaseDelay)
                    centers[i] = Vector3.MoveTowards(centers[i], destination,
                        beamFollowSpeed * (enraged ? rageBeamFollowMultiplier : 1f) * dt);
                LineRenderer line = pillars[i].GetComponent<LineRenderer>();
                Vector3 basePoint = centers[i] + Vector3.up * .12f;
                line.SetPosition(0, basePoint);
                line.SetPosition(1, basePoint + Vector3.up * beamHeight);

                // Horizontal collision works even when the player jumps.
                if (Time.time >= nextHit[i] &&
                    FlatDistance(player.position, centers[i]) <= beamRadius + .5f &&
                    player.position.y >= centers[i].y - 1f &&
                    player.position.y <= centers[i].y + beamHeight)
                {
                    if (playerEntity == null) playerEntity = player.GetComponentInParent<Entity>();
                    if (playerEntity != null)
                        playerEntity.TakeDamage(Damage(beamDamage), this);
                    nextHit[i] = Time.time + Mathf.Max(.15f, beamTickInterval);
                }
            }
            if (enableBeamExplosions && Time.time >= nextExplosion)
            {
                nextExplosion = Time.time + Mathf.Max(0.4f,
                    beamExplosionInterval * (enraged ? 0.65f : 1f));
                for (int i = 0; i < count; i++)
                    StartCoroutine(HomingBeamExplosion(centers[i] + Vector3.up * 1.5f));
            }
            yield return null;
        }
        for (int i = 0; i < count; i++) RemoveBeamVisual(pillars[i]);

        allowMovementDuringDeathBeams = false;
        MarkRecovery(.45f);
    }

    // Flying energy explosions released from each active death beam.
    // The visual has no collider; damage is handled here so jump dodging works.
    private IEnumerator HomingBeamExplosion(Vector3 spawnPosition)
    {
        GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.name = "DeathBeam_Homing_Explosion";
        Collider orbCollider = orb.GetComponent<Collider>();
        if (orbCollider != null) Destroy(orbCollider);
        orb.transform.position = spawnPosition;
        orb.transform.localScale = Vector3.one * Mathf.Max(.2f, beamExplosionSize);
        Renderer orbRenderer = orb.GetComponent<Renderer>();
        if (orbRenderer != null)
            orbRenderer.material = MakeBeamMaterial(new Color(1f, .15f, .025f, 1f));
        beamVisuals.Add(orb);

        Vector3 direction = transform.forward;
        float life = 0f;
        bool detonated = false;
        while (life < beamExplosionLifetime && !defeated && !isDown && player != null)
        {
            float dt = Time.deltaTime;
            life += dt;
            Vector3 target = player.position + Vector3.up * 1.1f + playerVelocity * .12f;
            Vector3 desired = target - orb.transform.position;
            if (desired.sqrMagnitude > .0001f)
                direction = Vector3.Slerp(direction, desired.normalized,
                    Mathf.Clamp01(beamExplosionSteering * dt)).normalized;
            orb.transform.position += direction * beamExplosionSpeed *
                (enraged ? 1.2f : 1f) * dt;
            orb.transform.Rotate(90f * dt, 140f * dt, 0f);

            // Direct proximity hit; a flying/jumping player can still be hit.
            if (Vector3.Distance(orb.transform.position, target) <=
                Mathf.Max(.5f, beamExplosionRadius * .5f))
            {
                detonated = true;
                break;
            }
            yield return null;
        }

        if (!isDown && !defeated && detonated && player != null)
        {
            Vector3 blast = orb.transform.position;
            PlayOnce(fireballImpactPrefab != null ? fireballImpactPrefab :
                (slamImpactVFX != null ? slamImpactVFX : slamVFX), blast);
            if (Vector3.Distance(player.position + Vector3.up, blast) <= beamExplosionRadius)
            {
                if (playerEntity == null) playerEntity = player.GetComponentInParent<Entity>();
                if (playerEntity != null)
                    playerEntity.TakeDamage(Damage(beamExplosionDamage), this);
            }
        }
        RemoveBeamVisual(orb);
    }

    // ==========================================
    // AREA EXPLOSIONS: lock in the target when the warning appears.
    // They are dodgeable but repeated during rage.
    private IEnumerator GroundExplosionAttack()
    {
        int waves = enraged ? Mathf.Clamp(groundExplosionRageWaves, 1, 8) : 1;
        for (int wave = 0; wave < waves && !isDown && !defeated && player != null; wave++)
        {
            Vector3 center = GroundPoint(PredictPlayer(enraged ? .25f : .15f));
            GameObject warning = CreateExplosionCircle(center, groundExplosionRadius,
                new Color(1f, .55f, .07f, 1f), "Ground_Explosion_Warning");
            float elapsed = 0f;
            float delay = Mathf.Max(.35f, groundExplosionWarning * (enraged ? .8f : 1f));
            while (elapsed < delay && !isDown && !defeated)
            {
                elapsed += Time.deltaTime;
                if (warning != null)
                {
                    LineRenderer lr = warning.GetComponent<LineRenderer>();
                    lr.widthMultiplier = 1f + Mathf.PingPong(elapsed * 4f, .6f);
                }
                yield return null;
            }
            RemoveBeamVisual(warning);
            if (isDown || defeated || player == null) yield break;

            PlayOnce(slamImpactVFX != null ? slamImpactVFX : slamVFX, center);
            GameObject flash = CreateExplosionCircle(center, groundExplosionRadius,
                new Color(1f, .12f, .015f, 1f), "GROUND_EXPLOSION");
            // Horizontal blast area reaches jumping players too.
            if (FlatDistance(player.position, center) <= groundExplosionRadius &&
                player.position.y >= center.y - 1.5f &&
                player.position.y <= center.y + groundExplosionVerticalReach)
            {
                if (playerEntity == null) playerEntity = player.GetComponentInParent<Entity>();
                if (playerEntity != null) playerEntity.TakeDamage(Damage(groundExplosionDamage), this);
            }
            yield return new WaitForSeconds(.22f);
            RemoveBeamVisual(flash);
            if (wave + 1 < waves)
                yield return new WaitForSeconds(Mathf.Max(.1f, groundExplosionWaveDelay));
        }
    }

    private GameObject CreateExplosionCircle(Vector3 center, float radius, Color color, string objectName)
    {
        GameObject obj = new GameObject(objectName);
        beamVisuals.Add(obj);
        LineRenderer line = obj.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.loop = true;
        line.positionCount = 72;
        line.startWidth = .22f;
        line.endWidth = .22f;
        line.material = MakeBeamMaterial(color);
        for (int i = 0; i < line.positionCount; i++)
        {
            float a = i * Mathf.PI * 2f / line.positionCount;
            line.SetPosition(i, center + new Vector3(Mathf.Cos(a) * radius, .14f, Mathf.Sin(a) * radius));
        }
        return obj;
    }

    private GameObject CreateHellTrapPillarVisual(string objectName, Vector3 position)
    {
        // Если модель копья не назначена, используем старый красный луч.
        if (hellTrapSpearPrefab == null)
        {
            return CreateBeamVisual(
                objectName,
                new Color(1f, .08f, .01f, 1f),
                .65f
            );
        }

        GameObject spear = Instantiate(
            hellTrapSpearPrefab,
            position + Vector3.up * hellTrapSpearVerticalOffset,
            Quaternion.Euler(hellTrapSpearRotationOffset)
        );

        spear.name = objectName;
        beamVisuals.Add(spear);
        return spear;
    }

    // ULTIMATE: several traps run at the SAME time, not one after another.
    // Each trap has shrinking sky pillars, meteor strikes, and a final blast.
    private IEnumerator SimultaneousHellTraps()
    {
        if (player == null || isDown || defeated) yield break;
        int count = Mathf.Clamp(enraged ? rageHellTrapCount : hellTrapCount, 1, 6);
        Vector3 focus = GroundPoint(PredictPlayer(.3f));
        float angleOffset = Random.Range(0f, Mathf.PI * 2f);
        float spread = Mathf.Max(1f, hellTrapSpacing);
        for (int n = 0; n < count; n++)
        {
            float angle = angleOffset + 2f * Mathf.PI * n / count;
            // Keep one trap focused on the player; others surround the escape routes.
            Vector3 offset = n == 0 ? Vector3.zero :
                new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * spread;
            StartCoroutine(RunHellTrap(GroundPoint(focus + offset), n));
        }
        yield break;
    }

    private IEnumerator RunHellTrap(Vector3 center, int index)
    {
        int count = Mathf.Clamp(hellTrapPillars, 4, 16);
        GameObject[] pillars = new GameObject[count];
        float startRadius = Mathf.Max(3f, hellTrapRadius);
        float shrinkSeconds = Mathf.Max(.8f, hellTrapShrinkTime);
        float finalRadius = Mathf.Max(2f, hellTrapFinalRadius);
        float warningSeconds = Mathf.Max(.4f, hellTrapWarning);
        float safeAngle = Random.value * 360f;
        int safePillar = Random.Range(0, count);
        GameObject warning = CreateExplosionCircle(center, startRadius,
            new Color(1f, .55f, .08f, 1f), "HellTrap_Warning_" + index);
        yield return new WaitForSeconds(warningSeconds);
        RemoveBeamVisual(warning);
        if (isDown || defeated || player == null) yield break;

        for (int i = 0; i < count; i++)
        {
            if (i == safePillar) continue; // visible escape gap

            float angle = (i / (float)count) * Mathf.PI * 2f
                + safeAngle * Mathf.Deg2Rad;

            Vector3 position = center +
                new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * startRadius;

            pillars[i] = CreateHellTrapPillarVisual(
                "HELL_TRAP_PILLAR_" + index + "_" + i,
                position
            );
        }
        int meteors = enraged ? rageHellTrapMeteorCount : hellTrapMeteorCount;
        float nextMeteorAt = 0.45f;
        float nextPillarHit = 0f;
        float elapsed = 0f;
        while (elapsed < shrinkSeconds && !isDown && !defeated && player != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / shrinkSeconds);
            float radius = Mathf.Lerp(startRadius, Mathf.Max(finalRadius + 1.3f, 3f), t);
            for (int i = 0; i < count; i++)
            {
                if (pillars[i] == null) continue;
                float a = (i / (float)count) * Mathf.PI * 2f + safeAngle * Mathf.Deg2Rad;
                Vector3 position = center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius;
                LineRenderer lr = pillars[i].GetComponent<LineRenderer>();

                if (lr != null)
                {
                    // Старый вариант: красный луч.
                    lr.SetPosition(0, position + Vector3.up * .1f);
                    lr.SetPosition(1, position + Vector3.up * hellTrapPillarHeight);
                }
                else
                {
                    // Новый вариант: модель копья движется вместе с ловушкой.
                    pillars[i].transform.position =
                        position + Vector3.up * hellTrapSpearVerticalOffset;
                    pillars[i].transform.rotation =
                        Quaternion.Euler(hellTrapSpearRotationOffset);
                }
                if (Time.time >= nextPillarHit && FlatDistance(position, player.position) <= 1.05f &&
                    player.position.y >= position.y - 1f && player.position.y < position.y + hellTrapPillarHeight)
                {
                    DealHellTrapDamage(hellTrapPillarDamage);
                    nextPillarHit = Time.time + .65f;
                }
            }
            if (elapsed >= nextMeteorAt && meteors > 0)
            {
                meteors--;
                nextMeteorAt += Mathf.Max(.27f, shrinkSeconds / Mathf.Max(1, enraged ? rageHellTrapMeteorCount : hellTrapMeteorCount));
                Vector3 meteorPoint = GroundPoint(PredictPlayer(.3f) +
                    new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f)));
                StartCoroutine(HellTrapMeteor(meteorPoint));
            }
            yield return null;
        }
        for (int i = 0; i < count; i++) RemoveBeamVisual(pillars[i]);
        if (isDown || defeated || player == null) yield break;
        GameObject finalWarning = CreateExplosionCircle(center, finalRadius,
            new Color(1f, .7f, .12f, 1f), "HELL_TRAP_FINAL_WARNING");
        yield return new WaitForSeconds(.85f);
        RemoveBeamVisual(finalWarning);
        if (isDown || defeated || player == null) yield break;
        PlayOnce(slamImpactVFX != null ? slamImpactVFX : slamVFX, center);
        if (FlatDistance(player.position, center) <= finalRadius &&
            player.position.y >= center.y - 1.5f && player.position.y <= center.y + hellTrapPillarHeight)
            DealHellTrapDamage(hellTrapFinalDamage);
        GameObject flash = CreateExplosionCircle(center, finalRadius,
            new Color(1f, .12f, .015f, 1f), "HELL_TRAP_FINAL_BLAST");
        yield return new WaitForSeconds(.25f);
        RemoveBeamVisual(flash);
    }

    private IEnumerator HellTrapMeteor(Vector3 location)
    {
        GameObject warning = CreateExplosionCircle(location, hellTrapMeteorRadius,
            new Color(1f, .35f, .05f, 1f), "HELL_TRAP_METEOR_WARNING");
        yield return new WaitForSeconds(Mathf.Max(.3f, hellTrapMeteorWarning));
        RemoveBeamVisual(warning);
        if (isDown || defeated || player == null) yield break;
        PlayOnce(fireballImpactPrefab != null ? fireballImpactPrefab : slamImpactVFX, location);
        if (FlatDistance(location, player.position) <= hellTrapMeteorRadius &&
            player.position.y <= location.y + hellTrapPillarHeight)
            DealHellTrapDamage(hellTrapMeteorDamage);
    }

    private void DealHellTrapDamage(float amount)
    {
        if (isDown || defeated || player == null) return;
        if (playerEntity == null) playerEntity = player.GetComponentInParent<Entity>();
        if (playerEntity != null) playerEntity.TakeDamage(Damage(amount), this);
    }

    // RADIAL FIREBALL BARRAGE: surrounds boss and shoots outward
    // ==========================================

    private IEnumerator FireballRingAttack()
    {
        acting = true;
        StopAgent();
        Animate("Fireball");
        PlayOnce(summonVFX, transform.position);
        yield return new WaitForSeconds(Mathf.Max(.15f, ringWindup));
        if (isDown || defeated || fireballPrefab == null) yield break;

        int count = Mathf.Clamp(enraged ? rageRingFireballCount : ringFireballCount, 3, 60);
        int waves = enraged ? 2 : 1;
        for (int wave = 0; wave < waves; wave++)
        {
            if (isDown || defeated) yield break;
            // Visual charging: fireballs assemble around the boss before launch.
            GameObject[] previews = new GameObject[count];
            for (int i = 0; i < count; i++)
            {
                float angle = (i + (wave == 1 ? .5f : 0f)) * Mathf.PI * 2f / count;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 origin = transform.position + direction * ringSpawnRadius +
                    Vector3.up * fireballHeight;
                GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                orb.name = "Boss_Charging_Fireball";
                orb.transform.position = origin;
                orb.transform.localScale = Vector3.one * .65f;
                Collider c = orb.GetComponent<Collider>();
                if (c != null) Destroy(c);
                Material glow = MakeBeamMaterial(new Color(1f, .22f, .015f));
                if (glow != null) orb.GetComponent<Renderer>().material = glow;
                previews[i] = orb;
                beamVisuals.Add(orb); // also removed if the boss dies mid-cast
            }
            float charge = 0f;
            while (charge < .6f && !defeated && !isDown)
            {
                charge += Time.deltaTime;
                for (int i = 0; i < count; i++)
                    if (previews[i] != null)
                        previews[i].transform.Rotate(Vector3.up, 150f * Time.deltaTime);
                yield return null;
            }
            for (int i = 0; i < count; i++)
            {
                if (previews[i] == null) continue;
                Vector3 origin = previews[i].transform.position;
                Vector3 direction = origin - transform.position;
                direction.y = 0f;
                direction.Normalize();
                beamVisuals.Remove(previews[i]);
                Destroy(previews[i]);
                if (isDown || defeated) continue;
                FireballProjectile projectile = Instantiate(fireballPrefab,
                    origin, Quaternion.LookRotation(direction));
                projectile.Initialize(this, player, direction, Damage(fireballDamage),
                    ringFireballSpeed * (enraged ? 1.2f : 1f),
                    fireballImpactPrefab, 0f);
            }
            if (wave + 1 < waves) yield return new WaitForSeconds(.5f);
        }
        MarkRecovery(.6f);
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

        yield return new WaitForSeconds(phase >= 3 ? 0.24f : 0.40f);

        if (defeated || isDown || player == null)
            yield break;

        Vector3 heading =
            player.position - transform.position;

        heading.y = 0f;
        heading.Normalize();
        if (heading.sqrMagnitude < 0.001f) heading = transform.forward;

        HashSet<Entity> hit =
            new HashSet<Entity>();

        float elapsed = 0f;

        while (elapsed < (phase >= 3 ? 0.62f : 0.50f) &&
               agent.enabled &&
               agent.isOnNavMesh &&
               !isDown)
        {
            float dt = Time.deltaTime;

            elapsed += dt;

            Vector3 desired = PredictPlayer(0.13f) - transform.position;
            desired.y = 0f;
            if (desired.sqrMagnitude > 0.01f)
                heading = Vector3.Slerp(heading, desired.normalized,
                    Mathf.Clamp01(dashSteering * dt)).normalized;
            agent.Move(heading * dashSpeed * dt);

            if (Random.value < 0.4f)
                PlayOnce(dashVFX, transform.position);

            if (playerEntity == null && player != null)
                playerEntity =
                    player.GetComponentInParent<Entity>();

            if (playerEntity != null &&
                !hit.Contains(playerEntity) &&
                FlatDistance(transform.position, player.position) < 2.6f &&
                Mathf.Abs(transform.position.y - player.position.y) < 3.8f)
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
                !stunned && !acting)
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
        nextKnifeStorm = Time.time + 0.8f;
        // Rage should not force an instant laser: keep time for other moves.
        nextBeamAttack = Mathf.Max(nextBeamAttack, Time.time + 6f);
        nextRingAttack = Mathf.Min(nextRingAttack, Time.time + 2.5f);
        nextSummon = Mathf.Min(nextSummon, Time.time + 3f);

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
    // RAGE KNIVES: multiple homing volleys, even during other attacks.
    // ==========================================

    private GameObject CreateRageKnife(Vector3 location)
    {
        GameObject knife;
        if (knifePrefab != null)
        {
            knife = Instantiate(knifePrefab, location, Quaternion.identity);
        }
        else
        {
            // Fallback visible blade when no prefab has been assigned.
            knife = GameObject.CreatePrimitive(PrimitiveType.Cube);
            knife.transform.position = location;
            knife.transform.localScale = new Vector3(.13f, .15f, 1.25f);
            Renderer rend = knife.GetComponent<Renderer>();
            if (rend != null)
                rend.material = MakeBeamMaterial(new Color(1f, .12f, .04f));
        }
        knife.name = "Rage_Homing_Knife";
        foreach (Collider c in knife.GetComponentsInChildren<Collider>())
            c.enabled = false; // Damage is handled by the boss, not by physics callbacks.
        activeKnifeVisuals.Add(knife);
        return knife;
    }

    private void ClearKnifeVisuals()
    {
        foreach (GameObject obj in activeKnifeVisuals)
            if (obj != null) Destroy(obj);
        activeKnifeVisuals.Clear();
    }

    private IEnumerator RageKnifeStorm()
    {
        if (!enraged || isDown || defeated || player == null) yield break;
        int count = Mathf.Clamp(rageKnivesPerVolley, 1, 32);
        // A staggered second wave makes jumping through the first volley risky.
        int waves = phase >= 3 ? 2 : 1;
        for (int wave = 0; wave < waves; wave++)
        {
            if (isDown || defeated || player == null) yield break;
            for (int i = 0; i < count; i++)
            {
                float angle = (i + wave * .5f) * Mathf.PI * 2f / count;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                // All knives spawn level with the player's body, not at random heights.
                float knifeY = player.position.y + knifeHeightOffset;
                Vector3 spawn = transform.position + offset * rageKnifeSpreadRadius;
                spawn.y = knifeY;
                StartCoroutine(FlyRageKnife(spawn, i * .04f));
            }
            if (wave + 1 < waves) yield return new WaitForSeconds(.65f);
        }
    }

    private IEnumerator FlyRageKnife(Vector3 spawn, float additionalDelay)
    {
        GameObject knife = CreateRageKnife(spawn);
        float windup = Mathf.Max(.08f, rageKnifeLaunchDelay) + additionalDelay;
        float t = 0f;
        while (t < windup && knife != null && !isDown && !defeated)
        {
            t += Time.deltaTime;
            if (knifeFollowPlayerHeightDuringWindup && player != null)
            {
                Vector3 p = knife.transform.position;
                p.y = player.position.y + knifeHeightOffset;
                knife.transform.position = p;
            }
            knife.transform.Rotate(0f, 150f * Time.deltaTime, 0f, Space.World);
            yield return null;
        }
        if (knife == null || isDown || defeated || player == null)
        {
            RemoveKnife(knife);
            yield break;
        }

        Vector3 direction = (player.position + Vector3.up * knifeHeightOffset - knife.transform.position).normalized;
        float life = 0f;
        while (life < Mathf.Max(.2f, rageKnifeLifetime) &&
            knife != null && !isDown && !defeated && player != null)
        {
            float dt = Time.deltaTime;
            life += dt;
            Vector3 target = player.position + Vector3.up * knifeHeightOffset + playerVelocity * .13f;
            Vector3 desired = target - knife.transform.position;
            if (desired.sqrMagnitude > .001f)
                direction = Vector3.Slerp(direction, desired.normalized,
                    Mathf.Clamp01(rageKnifeTurnSpeed * dt)).normalized;
            knife.transform.rotation = Quaternion.LookRotation(direction);
            Vector3 previous = knife.transform.position;
            Vector3 next = previous + direction * rageKnifeSpeed * dt;
            knife.transform.position = next;

            // Segment distance prevents fast blades tunneling past the player.
            Vector3 segment = next - previous;
            float u = segment.sqrMagnitude > .00001f ?
                Mathf.Clamp01(Vector3.Dot(target - previous, segment) / segment.sqrMagnitude) : 0f;
            Vector3 closest = previous + segment * u;
            if (Vector3.Distance(closest, target) <= rageKnifeHitRadius)
            {
                if (playerEntity == null) playerEntity = player.GetComponentInParent<Entity>();
                if (playerEntity != null)
                    playerEntity.TakeDamage(Damage(rageKnifeDamage), this);
                PlayOnce(fireballImpactPrefab, next);
                break;
            }
            yield return null;
        }
        RemoveKnife(knife);
    }

    private void RemoveKnife(GameObject knife)
    {
        if (knife == null) return;
        activeKnifeVisuals.Remove(knife);
        Destroy(knife);
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
        ClearBeamVisuals();
        ClearKnifeVisuals();

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