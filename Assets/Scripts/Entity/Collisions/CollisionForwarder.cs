using UnityEngine;

public class CollisionForwarder : MonoBehaviour
{
    public CollisionDamage parentDamage;

    void Awake()
    {
        if (parentDamage == null) parentDamage = GetComponentInParent<CollisionDamage>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (parentDamage != null && collision.gameObject.layer != gameObject.layer) parentDamage.HandleCollision(collision, transform);
    }
}