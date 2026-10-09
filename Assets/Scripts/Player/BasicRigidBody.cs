using Lucy.Core;
using UnityEngine;

public class BasicRigidBodyPush : MonoBehaviour
{
	public LayerMask pushLayers;
	public bool canPush;
	[Range(0.5f, 5f)] public float strength = 1.1f;
	[Range(0f, 100f)] public float threshold = 5f;
	[Range(0f, 100f)] public float damageMultiplier = 1f;

	private void OnControllerColliderHit(ControllerColliderHit hit)
	{
		if (canPush) PushRigidBodies(hit);
	}

	private void PushRigidBodies(ControllerColliderHit hit)
	{
		if (TryGetComponent<Movement>(out var movement))
		{
			var entity = hit.collider.GetComponentInParent<Entity>();
			if (entity != null && movement.currentHorizontalVelocity.magnitude >= threshold) entity.TakeDamage(movement.currentHorizontalVelocity.magnitude);
		}
		Rigidbody body = hit.collider.attachedRigidbody;
		if (body == null || body.isKinematic) return;
		var bodyLayerMask = 1 << body.gameObject.layer;
		if ((bodyLayerMask & pushLayers.value) == 0) return;
		if (hit.moveDirection.y < -0.3f) return;
		Vector3 pushDir = new Vector3(hit.moveDirection.x, 0.0f, hit.moveDirection.z);
		body.AddForce(pushDir * strength, ForceMode.Impulse);
		
	}
}