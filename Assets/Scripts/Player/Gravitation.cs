using System;
using UnityEngine;

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
        if (grabOrigin == null) grabOrigin = transform;

        currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
    }

    private void Reset()
    {
        currentDistance = initializationDistance;
    }

    private void FixedUpdate()
    {
        if (grabbedRB != null)
        {
            HoldObject();
        }
    }

    #region Core Logic

    public void Grab()
    {
        if (grabbedRB != null) return;
        if (grabOrigin == null) return;

        if (Physics.Raycast(grabOrigin.position, grabOrigin.forward, out RaycastHit hit, maxGrabDistance, grabLayerMask))
        {
            Rigidbody rb = hit.collider.attachedRigidbody ?? hit.collider.GetComponentInParent<Rigidbody>();
            if (rb == null) return;

            grabbedRB = rb;
            grabbedBrain = hit.collider.GetComponentInParent<EnemyBase>();

            // Physics setup
            grabbedRB.maxLinearVelocity = maxThrowSpeed;
            grabbedRB.isKinematic = false;

            if (grabbedRB.TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var agent))
                agent.enabled = false;

            originalDrag = grabbedRB.linearDamping;
            grabbedRB.linearDamping = 0f;

            // Distance init
            currentDistance = Mathf.Clamp(initializationDistance, minDistance, maxDistance);

            // Enemy logic
            if (grabbedBrain != null)
            {
                grabbedBrain.groundCheckEnable = false;
            }

            OnGrabbed?.Invoke(grabbedBrain != null ? grabbedBrain.gameObject : hit.collider.gameObject);
        } else
        {
            OnGrabbed?.Invoke(null);
        }
    }

    private void HoldObject()
    {
        if (grabOrigin == null || grabbedRB == null) return;

        // Enemy falling behavior
        if (grabbedBrain != null)
        {
            grabbedBrain.ApplyFalling();
        }

        Vector3 targetPos = grabOrigin.position + grabOrigin.forward * currentDistance;
        Vector3 offset = targetPos - grabbedRB.position;

        Vector3 spring = offset * springForce;
        Vector3 damping = -grabbedRB.linearVelocity * damper;

        grabbedRB.AddForce(spring + damping);
    }

    public void Release()
    {
        if (grabbedRB != null)
        {
            grabbedRB.linearDamping = originalDrag;
        }

        if (grabbedBrain != null)
        {
            grabbedBrain.groundCheckEnable = true;
        }

        OnReleased?.Invoke(grabbedBrain != null ? grabbedBrain.gameObject : grabbedRB != null ? grabbedRB.gameObject : null);

        grabbedRB = null;
        grabbedBrain = null;
    }

    #endregion

    #region Distance Control

    public void AdjustDistance(float delta)
    {
        currentDistance = Mathf.Clamp(currentDistance + delta, minDistance, maxDistance);
    }

    public void SetDistance(float distance)
    {
        currentDistance = Mathf.Clamp(distance, minDistance, maxDistance);
    }

    #endregion

    #region Gizmos

    private void OnDrawGizmosSelected()
    {
        if (grabOrigin == null || !drawGizmos) return;

        // Ray
        Gizmos.color = Color.green;
        Gizmos.DrawLine(grabOrigin.position, grabOrigin.position + grabOrigin.forward * maxGrabDistance);

        // Current
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(grabOrigin.position + grabOrigin.forward * currentDistance, 0.18f);

        // Initial
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(grabOrigin.position + grabOrigin.forward * initializationDistance, 0.15f);

        // Min
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(grabOrigin.position + grabOrigin.forward * minDistance, 0.08f);

        // Max
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(grabOrigin.position + grabOrigin.forward * maxDistance, 0.08f);
    }

    #endregion
}