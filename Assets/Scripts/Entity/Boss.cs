
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
    [SerializeField] private float beamCooldown = 11f;
    [SerializeField] private float beamDamage = 17f;
    [SerializeField] private float beamHeight = 24f;
    [SerializeField] private float beamRadius = 0.85f;
    [SerializeField] private float beamDuration = 5f;
    [SerializeField] private float beamWarning = 0.8f;
    [SerializeField] private float beamFollowSpeed = 5.5f;
    [SerializeField] private float beamTickInterval = 0.65f;
    [SerializeField] private float beamSpread = 3.2f;
    [SerializeField] private Color beamColor = new Color(1f, 0.06f, 0.02f, 1f);

    [Header("FIREBALL RING - Radial Barrage")]
    [SerializeField] private bool enableFireballRing = true;
    [SerializeField] private float fireballRingCooldown = 8f;
    [SerializeField] private int ringFireballCount = 12;
    [SerializeField] private int rageRingFireballCount = 22;
    [SerializeField] private float ringFireballSpeed = 22f;
    [SerializeField] private float ringSpawnRadius = 2.5f;
    [SerializeField] private float ringWindup = 0.7f;

    private float nextBeamAttack;
    private float nextRingAttack;
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
        nextBeamAttack = availableAt + 4f;
        nextRingAttack = availableAt + 2.5f;

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

        SamplePlayerMotion();

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

        float playerDistance = FlatDistance(transform.position, player.position);

        // Repeated jumping is dangerous: an air burst targets the predicted landing area.
        if (enableAntiAir && IsPlayerAirborne() &&
            playerDistance <= antiAirRange && Time.time >= nextAntiAir &&
            Time.time >= nextAction)
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
            nextSummon = Time.time + summonInterval;

            StartCoroutine(SummonAttack());
            return;
        }

        // Attack selection
        if (Time.time >= nextAction)
        {
            if (enableDeathBeams && playerDistance <= 35f &&
                Time.time >= nextBeamAttack)
            {
                nextBeamAttack = Time.time + beamCooldown * (enraged ? 0.7f : 1f);
                StartCoroutine(DeathBeamAttack());
            }
            else if (enableFireballRing && fireballPrefab != null &&
                     playerDistance <= 22f && Time.time >= nextRingAttack)
            {
                nextRingAttack = Time.time + fireballRingCooldown * (enraged ? 0.7f : 1f);
                StartCoroutine(FireballRingAttack());
            }
            else if (playerDistance > 6f &&
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
        StopAgent();
        Animate("Slam");
        int count = enraged ? rageBeamCount : beamCount;
        count = Mathf.Clamp(count, 1, 12);
        Vector3[] centers = new Vector3[count];
        Vector3[] offsets = new Vector3[count];
        GameObject[] pillars = new GameObject[count];
        GameObject[] warnings = new GameObject[count];
        float[] nextHit = new float[count];

        // Offset beams so that dodging one can lead into another.
        for (int i = 0; i < count; i++)
        {
            float angle = (i * Mathf.PI * 2f / count) + 0.3f;
            offsets[i] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) *
                (count == 2 ? beamSpread * 0.65f : beamSpread);
            centers[i] = GroundPoint(PredictPlayer(0.25f) + offsets[i]);
            warnings[i] = CreateBeamVisual("DeathBeam_Warning", new Color(1f, .55f, .03f), .18f);
            LineRenderer warningLine = warnings[i].GetComponent<LineRenderer>();
            warningLine.SetPosition(0, centers[i] + Vector3.up * .04f);
            warningLine.SetPosition(1, centers[i] + Vector3.up * beamHeight);
        }
        yield return new WaitForSeconds(Mathf.Max(.15f, beamWarning));
        for (int i = 0; i < count; i++) RemoveBeamVisual(warnings[i]);
        if (isDown || defeated || player == null) yield break;

        for (int i = 0; i < count; i++)
        {
            pillars[i] = CreateBeamVisual("DEATH_BEAM_" + i, beamColor, beamRadius * 2f);
            PlayOnce(slamImpactVFX != null ? slamImpactVFX : slamVFX, centers[i]);
        }

        float elapsed = 0f;
        while (elapsed < beamDuration && !defeated && !isDown && player != null)
        {
            float dt = Time.deltaTime;
            elapsed += dt;
            Vector3 predicted = PredictPlayer(.22f);
            for (int i = 0; i < count; i++)
            {
                Vector3 destination = GroundPoint(predicted + offsets[i]);
                centers[i] = Vector3.MoveTowards(centers[i], destination,
                    beamFollowSpeed * (enraged ? 1.45f : 1f) * dt);
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
            yield return null;
        }
        for (int i = 0; i < count; i++) RemoveBeamVisual(pillars[i]);
        MarkRecovery(.45f);
    }

    // ==========================================
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
        ClearBeamVisuals();

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
