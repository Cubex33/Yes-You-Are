using Lucy.Core;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class KillerZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<Entity>() is Entity entity)
        {
            entity.TakeDamage(entity.MaxHealth, this);
        } else
        {
            Destroy(other.gameObject, 5f);
        }
    }
}