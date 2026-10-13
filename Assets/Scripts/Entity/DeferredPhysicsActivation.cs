
using UnityEngine;
using Lucy.Core;

[RequireComponent(typeof(Rigidbody))]
public class DeferredPhysicsActivation : MonoBehaviour
{
    [Header("Deferred Physics")]
    [Tooltip("Удерживать объект неподвижным до взаимодействия")]
    [SerializeField] private bool holdUntilInteracted = true;

    [Header("Collision Activation")]
    [Tooltip("Включать физику при достаточно сильном столкновении")]
    [SerializeField] private bool activateOnCollision = true;

    [Tooltip("Минимальная относительная скорость столкновения")]
    [SerializeField, Min(0f)] private float collisionVelocityThreshold = 0.15f;

    private Rigidbody rb;
    private bool activated;

    public bool IsActivated => activated;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (holdUntilInteracted)
        {
            rb.isKinematic = true;
            activated = false;
        }
        else
        {
            activated = !rb.isKinematic;
        }
    }

    public void ActivatePhysics()
    {
        if (activated)
            return;

        if (rb == null)
            rb = GetComponent<Rigidbody>();

        activated = true;
        rb.isKinematic = false;
        rb.WakeUp();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!activateOnCollision || activated)
            return;

        if (collision.relativeVelocity.magnitude >= collisionVelocityThreshold)
            ActivatePhysics();
    }
}
