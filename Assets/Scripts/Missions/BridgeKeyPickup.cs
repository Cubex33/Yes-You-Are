using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BridgeKeyPickup : MonoBehaviour
{
    public event System.Action<BridgeKeyPickup> Collected;
    [SerializeField] private string playerTag = "Player";
    private bool isCollected;

    private void Reset() => GetComponent<Collider>().isTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected || other == null) return;
        if (!other.CompareTag(playerTag) && other.GetComponentInParent<PlayerController>() == null) return;
        isCollected = true;
        gameObject.SetActive(false);
        Collected?.Invoke(this);
    }

    public void SetCollected(bool value)
    {
        isCollected = value;
        gameObject.SetActive(!value);
    }
}
