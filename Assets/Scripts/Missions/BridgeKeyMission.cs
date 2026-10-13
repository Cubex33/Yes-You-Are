using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Lucy.Core;

/// <summary>Collects unique scene keys and raises bridge panels in a fixed order.</summary>
public class BridgeKeyMission : BaseMission
{
    [SerializeField] private List<BridgeKeyPickup> keys = new();
    [SerializeField] private List<Transform> bridgePanels = new();
    [SerializeField] private float panelRaiseHeight = 3f;
    [SerializeField] private float panelRaiseDuration = 0.8f;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private Slider progressBar;
    [SerializeField] private UnityEvent onAllKeysCollected;

    private readonly HashSet<BridgeKeyPickup> collected = new();
    private readonly List<Vector3> panelStartPositions = new();
    private Coroutine raiseRoutine;
    private int raisedPanelCount;
    private bool allKeysCollected;
    public int CollectedCount => collected.Count;
    public int RequiredCount => keys == null ? 0 : keys.Count;

    private void Awake()
    {
        ResolveBridgePanels();
        CaptureStandingPositions();
        SetPanelsLowered();
    }

    private void ResolveBridgePanels()
    {
        if (bridgePanels != null && bridgePanels.Count >= 4) return;
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
        raisedPanelCount = 0;
        allKeysCollected = false;
        foreach (var key in keys)
        {
            if (key == null) continue;
            key.SetCollected(false);
            key.Collected += HandleKeyCollected;
        }
        if (bridgePanels.Count != RequiredCount)
            Debug.LogError($"{nameof(BridgeKeyMission)} needs one bridge panel for each key ({RequiredCount} keys, {bridgePanels.Count} panels).", this);
        SetPanelsLowered();
        base.StartMission();
        UpdateProgress();
        QuestProgressHUD.Show("Соберите ключи", collected.Count, RequiredCount);
        if (RequiredCount == 0) CompleteMission();
    }

    private void SetPanelsLowered()
    {
        for (int i = 0; i < bridgePanels.Count; i++)
            if (bridgePanels[i] != null && i < panelStartPositions.Count)
                bridgePanels[i].position = panelStartPositions[i] - Vector3.up * panelRaiseHeight;
    }

    private void HandleKeyCollected(BridgeKeyPickup key)
    {
        if (!isActive || key == null || !collected.Add(key)) return;
        key.Collected -= HandleKeyCollected;
        if (raiseRoutine == null && raisedPanelCount < collected.Count)
            raiseRoutine = StartCoroutine(RaiseNextPanel());
        UpdateProgress();
        if (collected.Count >= RequiredCount)
        {
            allKeysCollected = true;
            QuestProgressHUD.Show("Все ключи собраны — поднимите мост", collected.Count, RequiredCount);
            onAllKeysCollected?.Invoke();
            if (raisedPanelCount >= RequiredCount && bridgePanels.Count == RequiredCount)
                CompleteMission();
        }
    }

    private IEnumerator RaiseNextPanel()
    {
        int index = raisedPanelCount;
        if (index >= bridgePanels.Count)
        {
            raiseRoutine = null;
            yield break;
        }
        Transform panel = bridgePanels[index];
        if (panel == null)
        {
            raiseRoutine = null;
            Debug.LogError($"{nameof(BridgeKeyMission)}: assign bridge panel #{index + 1} in the Inspector.", this);
            yield break;
        }
        Vector3 from = panel.position;
        Vector3 to = panelStartPositions[index];
        float elapsed = 0f;
        while (elapsed < panelRaiseDuration)
        {
            elapsed += Time.deltaTime;
            panel.position = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / panelRaiseDuration));
            yield return null;
        }
        panel.position = to;
        raisedPanelCount++;
        raiseRoutine = null;
        if (raisedPanelCount < collected.Count && isActive)
            raiseRoutine = StartCoroutine(RaiseNextPanel());
        else if (allKeysCollected && isActive && raisedPanelCount >= RequiredCount && bridgePanels.Count == RequiredCount)
            CompleteMission();
    }

    private void UpdateProgress()
    {
        if (progressText != null)
            progressText.text = $"Соберите ключи: {collected.Count}/{RequiredCount}";
        if (progressBar != null)
        {
            progressBar.minValue = 0;
            progressBar.maxValue = Mathf.Max(1, RequiredCount);
            progressBar.value = collected.Count;
        }
        if (isActive) QuestProgressHUD.Show(allKeysCollected ? "Мост открыт — победите босса" : "Соберите ключи", collected.Count, RequiredCount);
    }

    private void OnGUI()
    {
        if (isActive && progressText == null)
        {
            string message = allKeysCollected && bridgePanels.Count != RequiredCount
                ? "Ключи собраны — назначьте панели моста в инспекторе."
                : $"Ключи: {collected.Count}/{RequiredCount}";
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
        raisedPanelCount = 0;
        allKeysCollected = false;
        for (int i = 0; i < bridgePanels.Count; i++)
            if (bridgePanels[i] != null && i < panelStartPositions.Count)
                bridgePanels[i].position = panelStartPositions[i] - Vector3.up * panelRaiseHeight;
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
