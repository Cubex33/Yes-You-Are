using UnityEngine;
using UnityEngine.Events;
public class CollisionEventTrigger : MonoBehaviour {
    [Header("Collision Event")]
    public UnityEvent OnCollision;
    [Header("Debug")]
    public bool debugEnabled;
    private void OnTriggerEnter(Collider other) {
        if (debugEnabled) Debug.Log("Collision detected with " + other.gameObject.name);
        OnCollision.Invoke();   
    } 
}
