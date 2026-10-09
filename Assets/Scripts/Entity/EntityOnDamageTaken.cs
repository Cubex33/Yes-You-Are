using Lucy.Core;
using UnityEngine;
using UnityEngine.Events;

public class EntityOnDamageTaken : MonoBehaviour
{
    public UnityEvent OnDamageTaker;
    [Header("Runtime")]
    public Entity entity;
    private void Awake()
    {
        if (entity == null)
            entity = GetComponent<Entity>();

        if (entity != null)
            entity.OnDamageTaken += HandleDamageTaken;
    }

    private void HandleDamageTaken(float amount, object source)
    {
        OnDamageTaker.Invoke();
    }
}