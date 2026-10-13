using IDosGames.WebBridge;
using UnityEngine;

public class LoadInventory : MonoBehaviour
{
    public IDosWebsiteBridge bridge;
    public Material goldenSkeletonMaterial;

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

    private void FindBridge()
    {
        if (bridge == null)
        {
            bridge = FindAnyObjectByType<IDosWebsiteBridge>(
                FindObjectsInactive.Include
            );
        }
    }

    private void OnEnable()
    {
        FindBridge();

        if (bridge != null)
            bridge.DataReceived += Receive;
    }

    private void OnDisable()
    {
        if (bridge != null)
            bridge.DataReceived -= Receive;
    }

    private void Start()
    {
        RefreshInventory();
    }

    public void RefreshInventory()
    {
        if (bridge == null)
        {
            Debug.LogWarning("Назначь Bridge в Inspector.", this);
            return;
        }

        bridge.RequestInventory();
    }

    public void Receive(string json)
    {
        InventoryResponse response;

        try
        {
            response = JsonUtility.FromJson<InventoryResponse>(json);
        }
        catch (System.Exception)
        {
            Debug.LogWarning("Получен некорректный JSON.", this);
            return;
        }

        if (response == null || response.type != "inventory")
            return;

        if (!response.ok)
        {
            Debug.LogWarning(
                "Не удалось проверить инвентарь: " + response.error,
                this
            );
            return;
        }

        foreach (var item in response.data?.items
                 ?? System.Array.Empty<InventoryItem>())
        {
            if (item != null
                && item.itemId == "golden_skeleton"
                && item.quantity > 0)
            {
                ApplyGoldenSkeleton();
                return;
            }
        }

        Debug.Log("Золотого скелета нет.", this);
    }

    private void ApplyGoldenSkeleton()
    {
        if (goldenSkeletonMaterial == null)
        {
            Debug.LogWarning("Назначь материал скина.", this);
            return;
        }

        // Включает неактивные дочерние объекты.
        var renderers = GetComponentsInChildren<Renderer>(true);

        foreach (var target in renderers)
        {
            // Сам объект со скриптом пропускаем.
            if (target.transform == transform)
                continue;

            var materials = target.sharedMaterials;
            bool changed = false;

            for (int i = 0; i < materials.Length; i++)
            {
                // Если материала нет, переходим к следующему слоту.
                if (materials[i] == null)
                    continue;

                materials[i] = goldenSkeletonMaterial;
                changed = true;
            }

            if (changed)
                target.sharedMaterials = materials;
        }
    }
}