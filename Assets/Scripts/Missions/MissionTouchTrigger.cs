#region Helper: MissionTouchTrigger
using System;
using Lucy.Core;
using UnityEngine;

/// <summary>
/// Lightweight trigger component used by missions. When the player enters the trigger, it invokes OnTouched.
/// Add it to trigger colliders or let missions add it at runtime.
/// The player is detected by the presence of a component derived from Entity (usual Player Entity) or by tag "Player".
/// </summary>
[RequireComponent(typeof(Collider))]
public class MissionTouchTrigger : MonoBehaviour
{
    public event Action<MissionTouchTrigger> OnTouched;
    public bool onlyTriggerOnce = true;
    public string playerTag = "Player"; // optional: set to the tag your player uses

    private void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;

        // Accept by tag or by Entity-derived component
        bool isPlayer = (!string.IsNullOrEmpty(playerTag) && other.CompareTag(playerTag)) || other.GetComponentInParent<Entity>() != null || other.GetComponentInParent<PlayerController>() != null;
        if (!isPlayer) return;

        OnTouched?.Invoke(this);

        if (onlyTriggerOnce)
        {
            // Deactivate instead of destroying so it can be restored after player death
            gameObject.SetActive(false);
        }
    }
}
#endregion
