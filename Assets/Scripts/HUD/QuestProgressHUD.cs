using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Persistent objective HUD.
/// Each mission owns its own objective, so one mission cannot hide another.
/// </summary>
public sealed class QuestProgressHUD : MonoBehaviour
{
    [Header("Layout (edit on StatsHUD in the Inspector)")]
    [SerializeField] private Vector2 anchor = new Vector2(1f, 0.5f);
    [SerializeField] private Vector2 anchoredOffset = new Vector2(-36f, 0f);
    [SerializeField] private Vector2 cardSize = new Vector2(440f, 132f);
    [SerializeField] private Color cardColor = new Color(0.035f, 0.055f, 0.075f, 0.88f);
    [SerializeField] private Color progressColor = new Color(0.18f, 0.78f, 0.95f, 1f);

    private sealed class Objective
    {
        public EntityId ownerId;
        public Object owner;
        public string text;
        public int current;
        public int total;
        public int priority;
        public long order;
    }

    private static QuestProgressHUD instance;
    private static readonly Dictionary<EntityId, Objective> objectives = new();
    private static long nextOrder;

    private TMP_Text title;
    private TMP_Text detail;
    private Slider progress;
    private GameObject card;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        Build();
        RefreshDisplay();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
            objectives.Clear();
        }
    }

    // Legacy API. Kept so existing scripts continue compiling.
    public static void Show(string objective, int current = 0, int total = 0)
    {
        SetObjective(null, objective, current, total, 0);
    }

    // Owner-aware API. Prefer this in mission scripts.
    public static void Show(
        Object owner,
        string objective,
        int current = 0,
        int total = 0,
        int priority = 0)
    {
        SetObjective(owner, objective, current, total, priority);
    }

    // Legacy API. Hides only the legacy objective.
    public static void Hide()
    {
        Hide(null);
    }

    // Owner-aware API. Removes only the specified owner's objective.
    public static void Hide(Object owner)
    {
        if (owner == null)
        {
            if (objectives.Remove(default) && instance != null)
                instance.RefreshDisplay();

            return;
        }

        EntityId ownerId = owner.GetEntityId();

        if (objectives.Remove(ownerId) && instance != null)
            instance.RefreshDisplay();
    }

    private static void SetObjective(
        Object owner,
        string objective,
        int current,
        int total,
        int priority)
    {
        QuestProgressHUD hud = Ensure();
        if (hud == null) return;

        EntityId ownerId = owner == null ? default : owner.GetEntityId();

        objectives[ownerId] = new Objective
        {
            ownerId = ownerId,
            owner = owner,
            text = objective ?? string.Empty,
            current = current,
            total = total,
            priority = priority,
            order = ++nextOrder
        };

        hud.RefreshDisplay();
    }

    private static QuestProgressHUD Ensure()
    {
        if (instance != null) return instance;

        instance = FindAnyObjectByType<QuestProgressHUD>(
            FindObjectsInactive.Include);

        if (instance != null)
        {
            if (instance.card == null)
                instance.Build();

            return instance;
        }

        var root = new GameObject("QuestProgressHUD");
        return root.AddComponent<QuestProgressHUD>();
    }

    private void RefreshDisplay()
    {
        if (card == null) Build();

        // Remove objectives whose owner was destroyed.
        var staleIds = new List<EntityId>();

        foreach (var pair in objectives)
        {
            if (!pair.Key.Equals(default(EntityId)) && pair.Value.owner == null)
                staleIds.Add(pair.Key);
        }

        foreach (EntityId id in staleIds)
            objectives.Remove(id);

        Objective selected = null;

        foreach (var objective in objectives.Values)
        {
            if (selected == null ||
                objective.priority > selected.priority ||
                (objective.priority == selected.priority &&
                 objective.order > selected.order))
            {
                selected = objective;
            }
        }

        if (selected == null)
        {
            card.SetActive(false);
            return;
        }

        Set(
            selected.text,
            selected.current,
            selected.total);
    }

    private void Build()
    {
        if (card != null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        Transform parent = canvas != null ? canvas.transform : transform;

        if (canvas == null)
        {
            var canvasObject = new GameObject(
                "QuestCanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            canvasObject.transform.SetParent(transform, false);

            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            parent = canvasObject.transform;
        }

        card = new GameObject(
            "QuestObjectiveCard",
            typeof(RectTransform),
            typeof(Image));

        card.transform.SetParent(parent, false);

        var cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = anchor;
        cardRect.anchorMax = anchor;
        cardRect.pivot = anchor;
        cardRect.anchoredPosition = anchoredOffset;
        cardRect.sizeDelta = cardSize;

        card.GetComponent<Image>().color = cardColor;

        title = MakeText(
            "ObjectiveTitle", card.transform, "ЗАДАНИЕ",
            16, FontStyles.Bold,
            new Vector2(20, -12), new Vector2(400, 26));

        title.color = new Color(0.38f, 0.86f, 1f);

        detail = MakeText(
            "ObjectiveDetail", card.transform, "",
            23, FontStyles.Bold,
            new Vector2(20, -43), new Vector2(400, 38));

        detail.color = Color.white;

        var sliderObject = MakeRect(
            "QuestProgress", card.transform,
            new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(20, -94),
            new Vector2(cardSize.x - 40, 14));

        sliderObject.AddComponent<Image>().color =
            new Color(0.12f, 0.16f, 0.19f, 1f);

        var fillObject = MakeRect(
            "ProgressFill", sliderObject.transform,
            Vector2.zero, new Vector2(0, 1),
            Vector2.zero, Vector2.zero);

        var fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        fillObject.AddComponent<Image>().color = progressColor;

        progress = sliderObject.AddComponent<Slider>();
        progress.minValue = 0;
        progress.maxValue = 1;
        progress.wholeNumbers = true;
        progress.interactable = false;
        progress.transition = Selectable.Transition.None;
        progress.fillRect = fillRect;
        progress.targetGraphic = sliderObject.GetComponent<Image>();
        progress.direction = Slider.Direction.LeftToRight;
    }

    private void Set(string objective, int current, int total)
    {
        if (!card.activeSelf)
            card.SetActive(true);

        detail.text = objective;

        bool hasProgress = total > 0;
        progress.gameObject.SetActive(hasProgress);

        if (hasProgress)
        {
            int safeCurrent = Mathf.Clamp(current, 0, total);
            progress.maxValue = total;
            progress.value = safeCurrent;
            title.text = $"ПРОГРЕСС МИССИИ   {safeCurrent}/{total}";
        }
        else
        {
            title.text = "ТЕКУЩАЯ ЦЕЛЬ";
        }
    }

    private static GameObject MakeRect(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 position,
        Vector2 size)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);

        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        return obj;
    }

    private static TMP_Text MakeText(
        string name,
        Transform parent,
        string value,
        float size,
        FontStyles style,
        Vector2 position,
        Vector2 dimensions)
    {
        var obj = MakeRect(
            name, parent,
            new Vector2(0, 1), new Vector2(0, 1),
            position, dimensions);

        var text = obj.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.color = Color.white;

        return text;
    }
}
