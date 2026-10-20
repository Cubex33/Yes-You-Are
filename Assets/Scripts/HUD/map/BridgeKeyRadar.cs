
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Простой радар ключей без импортированных изображений.
/// Отображает направление на активные BridgeKeyPickup.
/// </summary>
public class BridgeKeyRadar : MonoBehaviour
{
    [Header("Radar")]
    [SerializeField, Min(50f)] private float radarSize = 160f;
    [SerializeField, Min(1f)] private float radarRange = 30f;
    [SerializeField, Min(0f)] private float screenMargin = 20f;
    [SerializeField] private bool showRadar = true;

    [Header("Colors")]
    [SerializeField]
    private Color radarColor =
        new Color(0.02f, 0.10f, 0.08f, 0.88f);
    [SerializeField]
    private Color gridColor =
        new Color(0.2f, 1f, 0.65f, 0.55f);
    [SerializeField]
    private Color keyColor =
        new Color(1f, 0.8f, 0.2f, 1f);
    [SerializeField] private Color playerColor = Color.white;

    private PlayerController player;
    private Transform viewTransform;
    private readonly List<BridgeKeyPickup> keys = new();

    private Texture2D radarTexture;
    private Texture2D dotTexture;
    private GUIStyle titleStyle;
    private GUIStyle infoStyle;

    private float nextRefreshTime;

    private void Awake()
    {
        CreateTextures();
        CreateStyles();
        RefreshKeys();
    }

    private void Update()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();

            if (player != null)
                FindViewTransform();
        }

        if (Time.unscaledTime >= nextRefreshTime)
        {
            RefreshKeys();
            nextRefreshTime = Time.unscaledTime + 1f;
        }
    }

    private void FindViewTransform()
    {
        Camera mainCamera = Camera.main;

        viewTransform = mainCamera != null
            ? mainCamera.transform
            : player.transform;
    }

    private void RefreshKeys()
    {
        keys.Clear();

        keys.AddRange(
            FindObjectsByType<BridgeKeyPickup>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            )
        );
    }

    private void CreateTextures()
    {
        const int textureSize = 128;

        radarTexture = new Texture2D(
            textureSize, textureSize, TextureFormat.RGBA32, false
        );

        radarTexture.name = "Runtime Key Radar";
        radarTexture.hideFlags = HideFlags.HideAndDontSave;
        radarTexture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[textureSize * textureSize];

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float nx = (x + 0.5f) / textureSize * 2f - 1f;
                float ny = (y + 0.5f) / textureSize * 2f - 1f;
                float radius = Mathf.Sqrt(nx * nx + ny * ny);

                Color pixel = Color.clear;

                if (radius <= 1f)
                {
                    pixel = radarColor;

                    // Outer ring and inner distance ring.
                    if (Mathf.Abs(radius - 0.96f) < 0.025f ||
                        Mathf.Abs(radius - 0.5f) < 0.012f)
                    {
                        pixel = gridColor;
                    }

                    // Crosshair lines.
                    if (Mathf.Abs(nx) < 0.009f ||
                        Mathf.Abs(ny) < 0.009f)
                    {
                        pixel = new Color(
                            gridColor.r,
                            gridColor.g,
                            gridColor.b,
                            0.35f
                        );
                    }
                }

                pixels[y * textureSize + x] = pixel;
            }
        }

        radarTexture.SetPixels(pixels);
        radarTexture.Apply();

        dotTexture = new Texture2D(
            32, 32, TextureFormat.RGBA32, false
        );

        dotTexture.name = "Runtime Radar Dot";
        dotTexture.hideFlags = HideFlags.HideAndDontSave;
        dotTexture.filterMode = FilterMode.Bilinear;

        Color[] dotPixels = new Color[32 * 32];

        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float dx = (x + 0.5f) / 32f * 2f - 1f;
                float dy = (y + 0.5f) / 32f * 2f - 1f;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);

                float alpha = Mathf.Clamp01((1f - distance) * 16f);
                dotPixels[y * 32 + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        dotTexture.SetPixels(dotPixels);
        dotTexture.Apply();
    }


    private void CreateStyles()
    {
        titleStyle = new GUIStyle();
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.fontSize = 13;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = gridColor;

        infoStyle = new GUIStyle();
        infoStyle.alignment = TextAnchor.MiddleCenter;
        infoStyle.fontSize = 12;
        infoStyle.normal.textColor = Color.white;
    }


    private void OnGUI()
    {
        if (!showRadar || player == null || radarTexture == null)
            return;

        float size = radarSize;
        float left = Screen.width - screenMargin - size;
        float top = screenMargin;

        GUI.Label(
            new Rect(left, top, size, 22f),
            "РАДАР КЛЮЧЕЙ",
            titleStyle
        );

        Rect radarRect = new Rect(
            left, top + 24f, size, size
        );

        GUI.DrawTexture(radarRect, radarTexture);

        float centerX = radarRect.center.x;
        float centerY = radarRect.center.y;
        float radarRadius = size * 0.43f;

        Vector3 forward = viewTransform != null
            ? viewTransform.forward
            : player.transform.forward;

        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            forward = player.transform.forward;

        forward.y = 0f;
        forward.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        int availableKeys = 0;
        float nearestDistance = float.MaxValue;

        foreach (BridgeKeyPickup key in keys)
        {
            if (key == null || !key.gameObject.activeInHierarchy)
                continue;

            Vector3 offset = key.transform.position - player.transform.position;
            offset.y = 0f;

            float distance = offset.magnitude;
            nearestDistance = Mathf.Min(nearestDistance, distance);
            availableKeys++;

            // Показываем точки только в пределах радиуса радара.
            if (distance > radarRange || distance < 0.01f)
                continue;

            Vector3 direction = offset / distance;

            float horizontal = Vector3.Dot(direction, right);
            float vertical = Vector3.Dot(direction, forward);

            float x = centerX + horizontal * (distance / radarRange) * radarRadius;
            float y = centerY - vertical * (distance / radarRange) * radarRadius;

            DrawDot(x, y, 10f, keyColor);
        }

        // Игрок — точка в центре радара.
        DrawDot(centerX, centerY, 8f, playerColor);

        string status = availableKeys == 0
            ? "Нет доступных ключей"
            : $"Ближайший ключ: {Mathf.RoundToInt(nearestDistance)} м";

        GUI.Label(
            new Rect(left - 10f, top + size + 26f, size + 20f, 22f),
            status,
            infoStyle
        );
    }

    private void DrawDot(float x, float y, float dotSize, Color color)
    {
        Color previousColor = GUI.color;
        GUI.color = color;

        GUI.DrawTexture(
            new Rect(
                x - dotSize * 0.5f,
                y - dotSize * 0.5f,
                dotSize,
                dotSize
            ),
            dotTexture
        );

        GUI.color = previousColor;
    }

    private void OnDestroy()
    {
        if (radarTexture != null)
            Destroy(radarTexture);

        if (dotTexture != null)
            Destroy(dotTexture);
    }
}
