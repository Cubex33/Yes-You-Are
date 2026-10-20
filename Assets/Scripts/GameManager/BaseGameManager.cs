
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BaseGameManager : MonoBehaviour
{
    [Header("Settings")]
    public bool autoStart = true;

    [Header("Main Mission Sequence")]
    [Tooltip("ћиссии, которые должны запускатьс€ строго по очереди.")]
    public BaseMission[] missions;

    [Header("Independent Missions")]
    [Tooltip("ћиссии, которые запускаютс€ сразу и выполн€ютс€ независимо от основной цепочки.")]
    public BaseMission[] independentMissions;

    public Action<BaseMission> OnMissionChanged;

    [Header("Debug")]
    public BaseMission currentMission;

    [Header("Events")]
    public UnityEvent OnAllMissionCompleted;
    public UnityEvent OnMissionFailed;

    protected int currentMissionIndex = -1;

    public bool debugEnabled;

    private readonly HashSet<BaseMission> registeredMissions = new();

    public string CurrentMissionName =>
        currentMission != null ? currentMission.missionName : string.Empty;

    public BaseMission CurrentMission => currentMission;

    protected virtual void Start()
    {
        if (autoStart)
            StartGame();
    }

    #region Game Flow

    public virtual void StartGame()
    {
        currentMissionIndex = -1;

        // Ќезависимые миссии запускаютс€ сразу.
        StartIndependentMissions();

        // ќсновна€ цепочка продолжает работать по старым правилам.
        StartNextMission();
    }

    public virtual void PauseGame()
    {
        if (debugEnabled)
            Debug.Log("=== Game paused ===");
    }

    public virtual void MissionFailed()
    {
        if (debugEnabled)
            Debug.Log($"[GameManager] Mission failed: {CurrentMissionName}");

        MissionFailedHandler();
    }

    public virtual void RestartGameSequence()
    {
        UnregisterAllMissionEvents();

        var resetMissions = new HashSet<BaseMission>();

        ResetMissionArray(missions, resetMissions);
        ResetMissionArray(independentMissions, resetMissions);

        currentMission = null;
        currentMissionIndex = -1;

        if (debugEnabled)
            Debug.Log("[GameManager] Restarting mission sequence from the beginning.");

        StartGame();
    }

    private void ResetMissionArray(
        BaseMission[] missionArray,
        HashSet<BaseMission> alreadyReset)
    {
        if (missionArray == null)
            return;

        foreach (BaseMission mission in missionArray)
        {
            if (mission == null || !alreadyReset.Add(mission))
                continue;

            mission.ResetMission();
        }
    }

    #endregion

    #region Missions

    private void StartIndependentMissions()
    {
        if (independentMissions == null)
            return;

        foreach (BaseMission mission in independentMissions)
        {
            if (mission == null)
                continue;

            // Ќе запускаем одну миссию одновременно в двух списках.
            if (ContainsMission(missions, mission))
            {
                Debug.LogWarning(
                    $"[GameManager] Mission '{mission.missionName}' is assigned to both lists. " +
                    "Remove it from the main sequence or the independent list.",
                    mission);
                continue;
            }

            RegisterMissionEvents(mission);

            if (debugEnabled)
                Debug.Log($"[GameManager] Starting independent mission: {mission.missionName}");

            mission.StartMission();
        }
    }

    private bool ContainsMission(BaseMission[] array, BaseMission target)
    {
        if (array == null || target == null)
            return false;

        foreach (BaseMission mission in array)
        {
            if (mission == target)
                return true;
        }

        return false;
    }

    protected void StartNextMission()
    {
        currentMissionIndex++;

        if (!TryGetMission(currentMissionIndex, out currentMission))
        {
            currentMission = null;
            AllMissionsCompletedHandler();
            return;
        }

        RegisterMissionEvents(currentMission);

        OnMissionChanged?.Invoke(currentMission);

        if (debugEnabled)
            Debug.Log($"=== Start mission {CurrentMissionName} ===");

        currentMission.StartMission();
    }

    public virtual void RestartMission()
    {
        if (currentMission == null)
            return;

        UnregisterMissionEvents(currentMission);

        currentMission.ResetMission();
        RegisterMissionEvents(currentMission);
        currentMission.StartMission();
    }

    protected virtual bool TryGetMission(int index, out BaseMission mission)
    {
        if (missions == null ||
            missions.Length == 0 ||
            index < 0 ||
            index >= missions.Length)
        {
            mission = null;
            return false;
        }

        mission = missions[index];
        return mission != null;
    }

    #endregion

    #region Mission Events

    protected void RegisterMissionEvents(BaseMission mission)
    {
        if (mission == null)
            return;

        // «ащита от повторной подписки на событи€.
        mission.OnMissionCompleted -= HandleMissionCompleted;
        mission.OnMissionFailed -= HandleMissionFailed;

        mission.OnMissionCompleted += HandleMissionCompleted;
        mission.OnMissionFailed += HandleMissionFailed;

        registeredMissions.Add(mission);
    }

    protected void UnregisterMissionEvents(BaseMission mission)
    {
        if (mission == null)
            return;

        mission.OnMissionCompleted -= HandleMissionCompleted;
        mission.OnMissionFailed -= HandleMissionFailed;

        registeredMissions.Remove(mission);
    }

    private void UnregisterAllMissionEvents()
    {
        foreach (BaseMission mission in registeredMissions)
        {
            if (mission == null)
                continue;

            mission.OnMissionCompleted -= HandleMissionCompleted;
            mission.OnMissionFailed -= HandleMissionFailed;
        }

        registeredMissions.Clear();
    }

    protected virtual void HandleMissionCompleted(BaseMission mission)
    {
        if (mission == null)
            return;

        if (debugEnabled)
            Debug.Log($"[GameManager] Completed mission: {mission.missionName}");

        UnregisterMissionEvents(mission);

        // Ќезависима€ мисси€ завершаетс€ сама по себе и не переключает
        // основную последовательность.
        if (mission != currentMission)
        {
            if (debugEnabled)
                Debug.Log($"[GameManager] Independent mission completed: {mission.missionName}");

            return;
        }

        StartNextMission();
    }

    protected virtual void HandleMissionFailed(BaseMission mission)
    {
        if (mission == null)
            return;

        if (debugEnabled)
            Debug.Log($"[GameManager] Failed mission: {mission.missionName}");

        UnregisterMissionEvents(mission);

        // ќшибка независимой миссии не должна автоматически проваливать игру.
        if (mission != currentMission)
        {
            if (debugEnabled)
                Debug.Log($"[GameManager] Independent mission failed: {mission.missionName}");

            return;
        }

        MissionFailed();
    }

    #endregion

    #region Hooks

    protected virtual void AllMissionsCompletedHandler()
    {
        if (debugEnabled)
            Debug.Log("=== All main-sequence missions completed ===");

        OnAllMissionCompleted?.Invoke();
    }

    protected virtual void MissionFailedHandler()
    {
        OnMissionFailed?.Invoke();
    }

    #endregion
}
