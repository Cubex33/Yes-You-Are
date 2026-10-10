using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Lucy.Core;

// Put this file on the boss GameObject, alongside a NavMeshAgent.
// Effects are created from Unity primitives at runtime (no asset downloads).
[RequireComponent(typeof(NavMeshAgent))]
public class Boss : Entity
{
    [Header("Target / movement")]
    [SerializeField] private Transform player;
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float meleeRange = 3f;
    [SerializeField] private float spawnDelay = 2f;
    [SerializeField] private float navMeshSearchRadius = 5f;

    [Header("Battle")]
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float comboDamage = 12f; // each of 3 strikes
    [SerializeField] private float slamDamage = 40f;
    [SerializeField] private float slamRadius = 5f;
    [SerializeField] private float dashDamage = 25f;
    [SerializeField] private float dashDistance = 6f;
    [SerializeField] private float projectileDamage = 16f;
    [SerializeField] private float projectileSpeed = 10f;

    [Header("Skeletons")]
    [SerializeField] private GameObject skeletonPrefab;
    [SerializeField] private int minSkeletons = 3;
    [SerializeField] private int maxSkeletons = 5;
    [SerializeField] private int maxAliveSkeletons = 12;
    [SerializeField] private float summonCooldown = 15f;
    [SerializeField] private float summonRadius = 5f;

    [Header("Rage")]
    [SerializeField] private bool startInRage = false;
    [SerializeField] private float rageSpeedMultiplier = 1.5f;
    [SerializeField] private float rageDamageMultiplier = 1.5f;
    [SerializeField] private float rageCooldownMultiplier = 0.7f;

    [Header("Visual effects")]
    [SerializeField] private bool showEffects = true;
    [SerializeField] private float effectHeight = 0.08f;

    [Header("Optional Particle System prefabs")]
    [Tooltip("Optional ParticleSystem prefab played when the boss appears.")]
    [SerializeField] private ParticleSystem spawnParticles;
    [SerializeField] private ParticleSystem comboParticles;
    [SerializeField] private ParticleSystem slamParticles;
    [SerializeField] private ParticleSystem summonParticles;
    [SerializeField] private ParticleSystem projectileParticles;
    [SerializeField] private ParticleSystem dashParticles;
    [SerializeField] private ParticleSystem rageParticles;
    [SerializeField] private bool playParticlePrefabs = true;

    private NavMeshAgent agent;
    private Entity playerEntity;
    private float wakeTime, nextAttackTime, nextSummonTime, nextNavMeshRetry;
    private bool busy, raging;
    private readonly List<GameObject> summoned = new List<GameObject>();
    private readonly List<GameObject> effects = new List<GameObject>();
    private Material redMaterial, orangeMaterial, purpleMaterial, cyanMaterial;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        agent.acceleration = 12f;
        agent.angularSpeed = 480f;
        agent.stoppingDistance = Mathf.Max(0.2f, meleeRange - 0.5f);
        agent.updatePosition = true;
        agent.updateRotation = true;

        FindPlayer();
        InitializeMaterials();
        TryPlaceOnNavMesh();
        raging = startInRage;
        wakeTime = Time.time + Mathf.Max(0f, spawnDelay);
        nextAttackTime = wakeTime;
        nextSummonTime = wakeTime + summonCooldown;
        StartCoroutine(SpawnIntro());
    }

    private void FindPlayer()
    {
        if (player == null)
        {
            GameObject target = GameObject.FindGameObjectWithTag("Player");
            if (target != null) player = target.transform;
        }
        if (player != null) playerEntity = player.GetComponentInParent<Entity>();
    }

    private bool TryPlaceOnNavMesh()
    {
        if (agent == null || !agent.enabled) return false;
        if (agent.isOnNavMesh) return true;
        NavMeshHit sample;
        if (NavMesh.SamplePosition(transform.position, out sample,
            navMeshSearchRadius, agent.areaMask))
            return agent.Warp(sample.position);
        return false;
    }

    private void Update()
    {
        if (player == null) FindPlayer();
        if (player == null) return;
        if (!agent.isOnNavMesh)
        {
            if (Time.time >= nextNavMeshRetry)
            {
                if (!TryPlaceOnNavMesh())
                    Debug.LogWarning("Boss: No baked NavMesh near the boss.", this);
                nextNavMeshRetry = Time.time + 2f;
            }
            return;
        }

        if (Time.time < wakeTime || busy)
        {
            StopMoving();
            return;
        }

        agent.speed = moveSpeed * (raging ? rageSpeedMultiplier : 1f);
        summoned.RemoveAll(item => item == null);
        if (skeletonPrefab != null && Time.time >= nextSummonTime)
        {
            nextSummonTime = Time.time + Mathf.Max(1f, summonCooldown);
            if (summoned.Count < maxAliveSkeletons)
            {
                StartCoroutine(Summon());
                return;
            }
        }

        float distance = Vector3.Distance(transform.position, player.position);
        if (Time.time >= nextAttackTime)
        {
            // Available abilities are selected by range, so boss never gets stuck
            // trying to use a short-range attack on a distant player.
            int choice = Random.Range(0, 3);
            if (distance <= meleeRange + 0.6f)
            {
                if (choice == 0) StartCoroutine(Combo());
                else if (choice == 1) StartCoroutine(Slam());
                else StartCoroutine(Dash());
            }
            else if (distance <= 14f)
            {
                if (choice == 0) StartCoroutine(Dash());
                else StartCoroutine(ProjectileVolley());
            }
            else
            {
                Chase();
            }
        }
        else
        {
            if (distance > meleeRange) Chase();
            else { StopMoving(); FacePlayer(); }
        }
    }

    private void Chase()
    {
        agent.isStopped = false;
        if (!agent.SetDestination(player.position))
            Debug.LogWarning("Boss: Cannot set destination", this);
    }

    private void StopMoving()
    {
        if (agent == null || !agent.isOnNavMesh) return;
        agent.isStopped = true;
        if (agent.hasPath) agent.ResetPath();
    }

    private void FacePlayer()
    {
        if (player == null) return;
        Vector3 forward = player.position - transform.position;
        forward.y = 0;
        if (forward.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.RotateTowards(transform.rotation,
                Quaternion.LookRotation(forward), 720f * Time.deltaTime);
    }

    private float Damage(float baseDamage) => baseDamage * (raging ? rageDamageMultiplier : 1f);

    private void DamagePlayer(float radius, float damage, float angle = 360f)
    {
        if (player == null) return;
        Vector3 delta = player.position - transform.position;
        delta.y = 0;
        if (delta.magnitude > radius) return;
        if (angle < 360f && delta.sqrMagnitude > 0.001f &&
            Vector3.Angle(transform.forward, delta) > angle / 2f) return;
        if (playerEntity == null) playerEntity = player.GetComponentInParent<Entity>();
        if (playerEntity != null) playerEntity.TakeDamage(Damage(damage), this);
    }

    private void FinishAbility()
    {
        nextAttackTime = Time.time + Mathf.Max(0.2f,
            attackCooldown * (raging ? rageCooldownMultiplier : 1f));
        busy = false;
    }

    // 1. Three telegraphed sword swipes.
    private IEnumerator Combo()
    {
        busy = true;
        StopMoving();
        for (int i = 0; i < 3; i++)
        {
            FacePlayer();
            if (showEffects) StartCoroutine(GrowingDisc(transform.position,
                1.6f, meleeRange, 0.35f, redMaterial));
            yield return new WaitForSeconds(0.35f);
            if (showEffects) Burst(transform.position + transform.forward * 1.4f,
                orangeMaterial, 1.5f);
            PlayParticles(comboParticles, transform.position + transform.forward * 1.4f + Vector3.up);
            DamagePlayer(meleeRange, comboDamage, 130f);
            yield return new WaitForSeconds(0.2f);
        }
        FinishAbility();
    }

    // 2. Warning area, then ground impact.
    private IEnumerator Slam()
    {
        busy = true;
        StopMoving();
        if (showEffects) StartCoroutine(GrowingDisc(transform.position,
            0.2f, slamRadius, 1.25f, redMaterial));
        yield return new WaitForSeconds(1.25f);
        if (showEffects)
        {
            StartCoroutine(GrowingDisc(transform.position,
                0.3f, slamRadius * 1.3f, 0.45f, orangeMaterial));
            Burst(transform.position, orangeMaterial, slamRadius);
        }
        PlayParticles(slamParticles, transform.position);
        DamagePlayer(slamRadius, slamDamage);
        yield return new WaitForSeconds(0.35f);
        FinishAbility();
    }

    // 3. Summon 3-5 skeletons at NavMesh-valid positions.
    private IEnumerator Summon()
    {
        busy = true;
        StopMoving();
        summoned.RemoveAll(item => item == null);
        int slots = Mathf.Max(0, maxAliveSkeletons - summoned.Count);
        int min = Mathf.Max(1, minSkeletons);
        int max = Mathf.Max(min, maxSkeletons);
        int count = Mathf.Min(Random.Range(min, max + 1), slots);
        if (count == 0) { FinishAbility(); yield break; }

        // Build exact positions before showing portals.
        List<Vector3> positions = new List<Vector3>();
        for (int i = 0; i < count; i++)
        {
            float a = Mathf.PI * 2f * i / count;
            Vector3 requested = transform.position +
                new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * summonRadius;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(requested, out hit, 3f, agent.areaMask))
            {
                positions.Add(hit.position);
                if (showEffects) StartCoroutine(GrowingDisc(hit.position,
                    0.2f, 1.7f, 1f, purpleMaterial));
            }
        }
        yield return new WaitForSeconds(1f);
        foreach (Vector3 pos in positions)
        {
            if (skeletonPrefab != null)
            {
                GameObject s = Instantiate(skeletonPrefab, pos, Quaternion.identity);
                summoned.Add(s);
                PlayParticles(summonParticles, pos);
                if (showEffects) Burst(pos + Vector3.up * 0.6f, purpleMaterial, 1.2f);
            }
            yield return new WaitForSeconds(0.2f);
        }
        FinishAbility();
    }

    // 4. Three independently tracked magical projectiles.
    private IEnumerator ProjectileVolley()
    {
        busy = true;
        StopMoving();
        FacePlayer();
        for (int i = 0; i < 3; i++)
        {
            if (player != null)
            {
                Vector3 source = transform.position + Vector3.up * 1.5f +
                    transform.right * (i - 1) * 0.7f;
                PlayParticles(projectileParticles, source);
                StartCoroutine(MagicProjectile(source));
            }
            yield return new WaitForSeconds(0.3f);
        }
        yield return new WaitForSeconds(0.3f);
        FinishAbility();
    }

    private IEnumerator MagicProjectile(Vector3 start)
    {
        GameObject orb = showEffects ? MakeSphere(start, 0.55f, purpleMaterial) : null;
        Vector3 position = start;
        // Snapshot aim gives the player an opportunity to dodge.
        Vector3 destination = player != null ? player.position + Vector3.up : start + transform.forward * 8f;
        Vector3 direction = (destination - position).normalized;
        float traveled = 0f;
        while (traveled < 25f)
        {
            Vector3 step = direction * projectileSpeed * Time.deltaTime;
            position += step;
            traveled += step.magnitude;
            if (orb != null) orb.transform.position = position;
            if (player != null && Vector3.Distance(position, player.position + Vector3.up) < 1f)
            {
                if (playerEntity == null) playerEntity = player.GetComponentInParent<Entity>();
                if (playerEntity != null) playerEntity.TakeDamage(Damage(projectileDamage), this);
                if (showEffects) Burst(position, purpleMaterial, 1.3f);
                break;
            }
            if (showEffects && Random.value < 0.4f)
                StartCoroutine(FadeSphere(position, 0.3f, purpleMaterial, 0.28f));
            yield return null;
        }
        if (orb != null) Destroy(orb);
    }

    // 5. Navigation-safe forward charge with a trail.
    private IEnumerator Dash()
    {
        busy = true;
        StopMoving();
        FacePlayer();
        if (showEffects) StartCoroutine(GrowingDisc(transform.position,
            0.5f, 2f, 0.55f, cyanMaterial));
        yield return new WaitForSeconds(0.55f);
        PlayParticles(dashParticles, transform.position + Vector3.up * 0.5f);
        Vector3 direction = transform.forward;
        float traveled = 0f;
        bool hitPlayer = false;
        while (traveled < dashDistance && agent.isOnNavMesh)
        {
            float step = Mathf.Min(12f * Time.deltaTime, dashDistance - traveled);
            Vector3 desired = transform.position + direction * step;
            NavMeshHit hit;
            if (!NavMesh.SamplePosition(desired, out hit, 0.65f, agent.areaMask)) break;
            // Only move if nearby destination is reachable on the current navigation island.
            if (Vector3.Distance(hit.position, desired) > 0.55f) break;
            if (!agent.Warp(hit.position)) break;
            traveled += step;
            if (showEffects && Random.value < 0.55f)
                StartCoroutine(FadeSphere(transform.position + Vector3.up * 0.7f,
                    0.75f, cyanMaterial, 0.4f));
            if (!hitPlayer && player != null &&
                Vector3.Distance(transform.position, player.position) <= 2.2f)
            {
                DamagePlayer(2.2f, dashDamage);
                hitPlayer = true;
            }
            yield return null;
        }
        yield return new WaitForSeconds(0.2f);
        FinishAbility();
    }

    // Phase 2 activation (call this from your Entity health/damage event).
    public void ActivateRage()
    {
        if (raging) return;
        raging = true;
        PlayParticles(rageParticles, transform.position + Vector3.up);
        if (showEffects) StartCoroutine(RageFlash());
        Debug.Log("BOSS ENTERED RAGE MODE", this);
    }

    private IEnumerator RageFlash()
    {
        for (int i = 0; i < 5; i++)
        {
            Burst(transform.position + Vector3.up, redMaterial, 2.5f);
            yield return new WaitForSeconds(0.25f);
        }
    }

    private IEnumerator SpawnIntro()
    {
        PlayParticles(spawnParticles, transform.position);
        if (!showEffects) yield break;
        float duration = Mathf.Max(0.2f, spawnDelay);
        StartCoroutine(GrowingDisc(transform.position, 0.25f, 5f,
            duration, purpleMaterial));
        yield return new WaitForSeconds(duration * 0.65f);
        Burst(transform.position + Vector3.up, purpleMaterial, 2.4f);
    }

    // Particle Systems are optional: assign prefab assets in the Inspector.
    // Without them, all five attacks still work with procedural effects.
    private void PlayParticles(ParticleSystem prefab, Vector3 position)
    {
        if (!playParticlePrefabs || prefab == null) return;
        ParticleSystem instance = Instantiate(prefab, position, Quaternion.identity);
        instance.gameObject.SetActive(true);
        instance.Play(true);
        // Destroy after even a long burst; no permanent particle objects.
        float lifetime = instance.main.duration + instance.main.startLifetime.constantMax + 2f;
        Destroy(instance.gameObject, Mathf.Max(3f, lifetime));
    }

    // Procedural visuals. Cylinders are translucent discs; spheres are flashes.
    private void InitializeMaterials()
    {
        redMaterial = MakeMaterial(new Color(1f, 0.08f, 0.08f, 0.45f));
        orangeMaterial = MakeMaterial(new Color(1f, 0.4f, 0.04f, 0.65f));
        purpleMaterial = MakeMaterial(new Color(0.65f, 0.1f, 1f, 0.5f));
        cyanMaterial = MakeMaterial(new Color(0.1f, 0.85f, 1f, 0.55f));
    }

    private static Material MakeMaterial(Color color)
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader);
        material.color = color;
        return material;
    }

    private GameObject MakePrimitive(PrimitiveType shape, Vector3 point,
        Vector3 scale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(shape);
        go.name = "Boss VFX";
        go.transform.position = point;
        go.transform.localScale = scale;
        Collider col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);
        Renderer renderer = go.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        effects.Add(go);
        return go;
    }

    private GameObject MakeSphere(Vector3 pos, float diameter, Material mat)
    {
        return MakePrimitive(PrimitiveType.Sphere, pos,
            Vector3.one * diameter, mat);
    }

    private IEnumerator GrowingDisc(Vector3 center, float startRadius,
        float endRadius, float duration, Material mat)
    {
        GameObject go = MakePrimitive(PrimitiveType.Cylinder,
            center + Vector3.up * effectHeight,
            new Vector3(startRadius * 2f, 0.015f, startRadius * 2f), mat);
        float elapsed = 0f;
        while (elapsed < duration && go != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float radius = Mathf.Lerp(startRadius, endRadius, t);
            go.transform.localScale = new Vector3(radius * 2f, 0.015f, radius * 2f);
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    private IEnumerator FadeSphere(Vector3 center, float diameter,
        Material mat, float duration)
    {
        GameObject go = MakeSphere(center, diameter, mat);
        float elapsed = 0f;
        while (elapsed < duration && go != null)
        {
            elapsed += Time.deltaTime;
            go.transform.localScale = Vector3.one *
                (diameter * (1f - Mathf.Clamp01(elapsed / duration)));
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    private void Burst(Vector3 pos, Material mat, float radius)
    {
        for (int i = 0; i < 8; i++)
        {
            Vector3 p = pos + Random.onUnitSphere * radius * 0.65f;
            StartCoroutine(FadeSphere(p, Random.Range(0.35f, 0.8f),
                mat, Random.Range(0.25f, 0.55f)));
        }
    }

    private void OnDestroy()
    {
        for (int i = 0; i < effects.Count; i++)
            if (effects[i] != null) Destroy(effects[i]);
        if (redMaterial != null) Destroy(redMaterial);
        if (orangeMaterial != null) Destroy(orangeMaterial);
        if (purpleMaterial != null) Destroy(purpleMaterial);
        if (cyanMaterial != null) Destroy(cyanMaterial);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, slamRadius);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, summonRadius);
    }
}
