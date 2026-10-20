
using System;
using UnityEngine;
using UnityEngine.AI;
using Lucy.Core;

public class Gravitation : MonoBehaviour
{
    #region Distance Settings

    [Header("Distance settings")]
    [Tooltip("Начальная дистанция при захвате")]
    public float initializationDistance = 2f;

    [Tooltip("Текущая дистанция удержания")]
    public float currentDistance = 2f;

    public float minDistance = 0.5f;
    public float maxDistance = 10f;
    public float distanceStep = 0.5f;

    #endregion

    #region Grab Settings

    [Header("Grab Settings")]
    public float maxGrabDistance = 10f;
    public Transform grabOrigin;
    public LayerMask grabLayerMask;

    #endregion

    #region Physics Settings

    [Header("Physics Settings")]
    public float springForce = 1000f;
    public float damper = 50f;
    public float maxThrowSpeed = 15f;

    [Header("Punch Launch")]
    [Tooltip("Скорость предмета после удара ЛКМ, когда он удерживается гравипушкой.")]
    [Min(1f)]
    public float punchLaunchSpeed = 18f;

    private float originalDrag;

    #endregion

    #region Runtime

    [Header("Runtime")]
    public Rigidbody grabbedRB;
    public EnemyBase grabbedBrain;

    public bool IsGrabbing => grabbedRB != null;

    #endregion

    #region Debug

    [Header("Debug")]
    public bool drawGizmos = false;

    #endregion

    #region Events

    public Action<GameObject> OnGrabbed;
    public Action<GameObject> OnReleased;

    #endregion

    private void Awake()
    {
        if (grabOrigin == null)
            grabOrigin = transform;

        currentDistance = Mathf.Clamp(
            currentDistance,
            minDistance,
            maxDistance
        );
    }

    private void Reset()
    {
        currentDistance = initializationDistance;
    }

    private void FixedUpdate()
    {
        if (grabbedRB != null)
            HoldObject();
    }

    private void OnDisable()
    {
        // Не оставляем объект захваченным, если компонент отключили.
        if (grabbedRB != null)
            Release();
    }

    #region Core Logic

    public void Grab()
    {
        if (grabbedRB != null || grabOrigin == null)
            return;

        if (!Physics.Raycast(
                grabOrigin.position,
                grabOrigin.forward,
                out RaycastHit hit,
                maxGrabDistance,
                grabLayerMask))
        {
            OnGrabbed?.Invoke(null);
            return;
        }

        Rigidbody rb = hit.collider.attachedRigidbody
            ?? hit.collider.GetComponentInParent<Rigidbody>();

        if (rb == null)
            return;

        grabbedRB = rb;
        ThrownObjectDamage previousDamage =
    grabbedRB.GetComponent<ThrownObjectDamage>();

        if (previousDamage != null)
            previousDamage.Disarm();

        grabbedBrain = hit.collider.GetComponentInParent<EnemyBase>();
        grabbedRB.maxLinearVelocity = maxThrowSpeed;

        DeferredPhysicsActivation deferredPhysics =
            grabbedRB.GetComponent<DeferredPhysicsActivation>();

        if (deferredPhysics != null)
        {
            deferredPhysics.ActivatePhysics();
        }
        else
        {
            grabbedRB.isKinematic = false;
        }

        originalDrag = grabbedRB.linearDamping;
        grabbedRB.linearDamping = 0f;

        currentDistance = Mathf.Clamp(
            initializationDistance,
            minDistance,
            maxDistance
        );

        if (grabbedBrain != null)
        {
            // EnemyBase сам отключает NavMeshAgent и включает физику.
            grabbedBrain.ApplyFalling();
        }
        else if (grabbedRB.TryGetComponent<NavMeshAgent>(out NavMeshAgent agent))
        {
            agent.enabled = false;
        }

        OnGrabbed?.Invoke(
            grabbedBrain != null
                ? grabbedBrain.gameObject
                : grabbedRB.gameObject
        );
    }

    private void HoldObject()
    {
        if (grabOrigin == null || grabbedRB == null)
            return;

        Vector3 targetPos =
            grabOrigin.position + grabOrigin.forward * currentDistance;

        Vector3 offset = targetPos - grabbedRB.position;

        Vector3 spring = offset * springForce;
        Vector3 damping = -grabbedRB.linearVelocity * damper;

        grabbedRB.AddForce(spring + damping, ForceMode.Force);
    }

    public void Release()
    {
        if (grabbedRB == null)
            return;

        GameObject releasedObject = grabbedBrain != null
            ? grabbedBrain.gameObject
            : grabbedRB.gameObject;

        grabbedRB.linearDamping = originalDrag;

        // Не включаем NavMeshAgent здесь:
        // скелет должен сначала приземлиться.
        if (grabbedBrain != null)
            grabbedBrain.groundCheckEnable = true;

        OnReleased?.Invoke(releasedObject);

        grabbedRB = null;
        grabbedBrain = null;
    }

    public GameObject LaunchHeldObject(Vector3 direction)
    {
        if (grabbedRB == null)
            return null;

        Rigidbody rb = grabbedRB;
        EnemyBase enemy = grabbedBrain;

        GameObject launchedObject = enemy != null
            ? enemy.gameObject
            : rb.gameObject;

        // Сначала прекращаем удержание, чтобы пружина
        // больше не тянула предмет обратно к игроку.
        rb.linearDamping = originalDrag;

        if (enemy != null)
            enemy.groundCheckEnable = true;

        OnReleased?.Invoke(launchedObject);

        grabbedRB = null;
        grabbedBrain = null;

        if (rb == null)
            return launchedObject;

        if (direction.sqrMagnitude < 0.001f)
            direction = transform.forward;

        direction.Normalize();

        rb.isKinematic = false;
        rb.maxLinearVelocity = Mathf.Max(
            rb.maxLinearVelocity,
            punchLaunchSpeed
        );

        // Направленный запуск с предсказуемой начальной скоростью.
        rb.linearVelocity = direction * punchLaunchSpeed;
        rb.WakeUp();

        // Урон от предметов. Самого врага, захваченного гравипушкой,
        // не регистрируем как метательный предмет.
        if (enemy == null)
        {
            ThrownObjectDamage impactDamage =
                rb.GetComponent<ThrownObjectDamage>();

            if (impactDamage == null)
                impactDamage = rb.gameObject.AddComponent<ThrownObjectDamage>();

            impactDamage.Arm();
        }

        return launchedObject;
    }

    #endregion

    #region Distance Control

    public void AdjustDistance(float delta)
    {
        currentDistance = Mathf.Clamp(
            currentDistance + delta,
            minDistance,
            maxDistance
        );
    }

    public void SetDistance(float distance)
    {
        currentDistance = Mathf.Clamp(
            distance,
            minDistance,
            maxDistance
        );
    }

    #endregion

    #region Gizmos

    private void OnDrawGizmosSelected()
    {
        if (grabOrigin == null || !drawGizmos)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(
            grabOrigin.position,
            grabOrigin.position + grabOrigin.forward * maxGrabDistance
        );

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(
            grabOrigin.position + grabOrigin.forward * currentDistance,
            0.18f
        );

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            grabOrigin.position + grabOrigin.forward * initializationDistance,
            0.15f
        );

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            grabOrigin.position + grabOrigin.forward * minDistance,
            0.08f
        );

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(
            grabOrigin.position + grabOrigin.forward * maxDistance,
            0.08f
        );
    }

    #endregion
}
