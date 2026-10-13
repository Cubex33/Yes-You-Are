using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Lucy.Core;

/// <summary>Raises the whole bridge in proportion to the collected coins.</summary>
public class BridgeKeyMission : BaseMission
{
    [SerializeField] private List<BridgeKeyPickup> keys = new();
    [SerializeField] private List<Transform> bridgePanels = new();
    [Min(0f)]
    [SerializeField] private float raiseHeightPerCoin = 4f;
    [SerializeField] private float panelRaiseDuration = 0.8f;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private Slider progressBar;
    [SerializeField] private UnityEvent onAllKeysCollected;

    private readonly HashSet<BridgeKeyPickup> collected = new();
    private readonly List<Vector3> panelStartPositions = new();
    private Coroutine raiseRoutine;
    private bool allKeysCollected;
    public int CollectedCount => collected.Count;
    public int RequiredCount => keys == null ? 0 : keys.Count;
    private float TotalRaiseHeight => raiseHeightPerCoin * RequiredCount;

    private void Awake()
    {
        ResolveBridgePanels();
        CaptureStandingPositions();
        SetPanelsLowered();
    }

    private void ResolveBridgePanels()
    {
        if (bridgePanels != null && bridgePanels.Count > 0) return;
        var found = new List<Transform>();
        foreach (Transform item in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (item.name == "Floor1" || item.name == "Floor2" || item.name == "Floor3" || item.name == "Floor4")
                found.Add(item);
        }
        found.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        if (found.Count > 0) bridgePanels = found;
    }

    private void CaptureStandingPositions()
    {
        panelStartPositions.Clear();
        foreach (var panel in bridgePanels)
            panelStartPositions.Add(panel != null ? panel.position : Vector3.zero);
    }

    public override void StartMission()
    {
        if (isActive || isCompleted) return;
        collected.Clear();
        allKeysCollected = false;
        foreach (var key in keys)
        {
            if (key == null) continue;
            key.SetCollected(false);
            key.Collected += HandleKeyCollected;
        }
        SetPanelsLowered();
        base.StartMission();
        UpdateProgress();
        if (RequiredCount == 0)
        {
            SetBridgeProgress(1f);
            CompleteMission();
        }
    }

    private void SetPanelsLowered()
    {
        SetBridgeProgress(0f);
    }

    private void HandleKeyCollected(BridgeKeyPickup key)
    {
        if (!isActive || key == null || !keys.Contains(key) || !collected.Add(key)) return;
        key.Collected -= HandleKeyCollected;
        if (raiseRoutine != null) StopCoroutine(raiseRoutine);
        raiseRoutine = null;
        allKeysCollected = collected.Count >= RequiredCount;
        UpdateProgress();
        if (panelRaiseDuration <= 0f)
        {
            SetBridgeProgress((float)collected.Count / RequiredCount);
            FinishBridgeRaise();
        }
        else
            raiseRoutine = StartCoroutine(RaiseBridge());
    }

    private void SetBridgeProgress(float progress)
    {
        for (int i = 0; i < bridgePanels.Count; i++)
            if (bridgePanels[i] != null && i < panelStartPositions.Count)
                bridgePanels[i].position = panelStartPositions[i]
                    - Vector3.up * (TotalRaiseHeight * (1f - Mathf.Clamp01(progress)));
    }

    private IEnumerator RaiseBridge()
    {
        float progress = (float)collected.Count / RequiredCount;
        var from = new Vector3[bridgePanels.Count];
        for (int i = 0; i < bridgePanels.Count; i++)
            if (bridgePanels[i] != null) from[i] = bridgePanels[i].position;
        float elapsed = 0f;
        while (elapsed < panelRaiseDuration)
        {
            elapsed += Time.deltaTime;
            for (int i = 0; i < bridgePanels.Count; i++)
            {
                if (bridgePanels[i] == null || i >= panelStartPositions.Count) continue;
                Vector3 to = panelStartPositions[i] - Vector3.up * (TotalRaiseHeight * (1f - progress));
                bridgePanels[i].position = Vector3.Lerp(from[i], to, Mathf.Clamp01(elapsed / panelRaiseDuration));
            }
            yield return null;
        }
        SetBridgeProgress(progress);
        raiseRoutine = null;
        FinishBridgeRaise();
    }

    private void FinishBridgeRaise()
    {
        if (!isActive || !allKeysCollected) return;
        onAllKeysCollected?.Invoke();
        CompleteMission();
    }

    private void UpdateProgress()
    {
        if (isActive) QuestProgressHUD.Show(allKeysCollected ? "Монеты собраны — мост поднимается" : "Соберите монеты — поднимите мост", collected.Count, RequiredCount);
        ResolveProgressBar();
        if (progressText != null)
            progressText.text = $"Монеты: {collected.Count}/{RequiredCount}";
        if (progressBar != null)
        {
            progressBar.minValue = 0;
            progressBar.maxValue = Mathf.Max(1, RequiredCount);
            progressBar.value = collected.Count;
        }
    }

    private void ResolveProgressBar()
    {
        if (progressBar != null) return;
        foreach (var slider in FindObjectsByType<Slider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (slider.gameObject.name != "QuestProgress") continue;
            progressBar = slider;
            break;
        }
    }

    private void OnGUI()
    {
        if (isActive && progressText == null)
        {
            string message = $"Монеты: {collected.Count}/{RequiredCount}";
            GUI.Box(new Rect(24f, 24f, 440f, 42f), message);
        }
    }

    public override void ResetMission()
    {
        if (raiseRoutine != null) StopCoroutine(raiseRoutine);
        raiseRoutine = null;
        foreach (var key in keys)
        {
            if (key == null) continue;
            key.Collected -= HandleKeyCollected;
            key.SetCollected(false);
        }
        collected.Clear();
        allKeysCollected = false;
        SetPanelsLowered();
        base.ResetMission();
        UpdateProgress();
        QuestProgressHUD.Hide();
    }

    protected override void CompleteMission()
    {
        foreach (var key in keys)
            if (key != null) key.Collected -= HandleKeyCollected;
        base.CompleteMission();
    }
}
