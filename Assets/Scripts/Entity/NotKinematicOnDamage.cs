using System;
using Lucy.Core;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Entity))]
public class NotKinematicOnDamage : MonoBehaviour
{
    [Header("Runtime")]
    public Rigidbody rb;
    public Entity entity;
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        entity = GetComponent<Entity>();
    }
    void OnEnable() => entity.OnDamageTaken += DisableKinematicOnDamage;
    void OnDisable() => entity.OnDamageTaken -= DisableKinematicOnDamage;
    void DisableKinematicOnDamage(float arg1, object arg2) => rb.isKinematic = false;
}