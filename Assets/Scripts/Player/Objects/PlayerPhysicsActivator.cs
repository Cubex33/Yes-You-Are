
using Lucy.Core;
using UnityEngine;

public class PlayerPhysicsActivator : MonoBehaviour
{
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        DeferredPhysicsActivation deferredPhysics =
            hit.collider.GetComponentInParent<DeferredPhysicsActivation>();

        if (deferredPhysics != null)
            deferredPhysics.ActivatePhysics();
    }
}
