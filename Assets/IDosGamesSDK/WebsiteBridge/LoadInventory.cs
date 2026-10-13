using IDosGames.WebBridge;
using UnityEngine;

public class LoadInventory : MonoBehaviour
{
    public IDosWebsiteBridge bridge;
    [System.Serializable]
    public class InventoryResponse
    {
        public string type;
        public bool ok;
        public string error;
        public InventoryData data;
    }

    [System.Serializable]
    public class InventoryData
    {
        public InventoryItem[] items;
    }

    [System.Serializable]
    public class InventoryItem
    {
        public string itemId;
        public int quantity;
    }

    private void OnEnable()
    {
        bridge.DataReceived += Receive;
    }

    private void OnDisable()
    {
        bridge.DataReceived -= Receive;
    }

    public void Receive(string json)
    {
        var response = JsonUtility.FromJson<InventoryResponse>(json);

        // Этот метод получает также ответы на другие запросы.
        if (response == null || response.type != "inventory")
            return;

        if (!response.ok)
        {
            Debug.LogWarning("Не удалось проверить инвентарь: " + response.error);
            return;
        }

        bool hasGoldenSkeleton = false;

        foreach (var item in response.data?.items
                 ?? System.Array.Empty<InventoryItem>())
        {
            if (item != null
                && item.itemId == "golden_skeleton"
                && item.quantity > 0)
            {
                hasGoldenSkeleton = true;
                break;
            }
        }

        if (hasGoldenSkeleton)
        {
            Debug.Log("Золотой скелет есть!");
        }
        else
        {
            Debug.Log("Золотого скелета нет.");
        }
    }
}
