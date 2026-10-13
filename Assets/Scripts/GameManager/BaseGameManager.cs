
using System;
using UnityEngine;
using UnityEngine.Events;

public class BaseGameManager : MonoBehaviour
{
    [Header("Settings")]
    public bool autoStart = true;

    [Header("Missions")]
    public BaseMission[] missions;

    public Action<BaseMission> OnMissionChanged;

    [Header("Debug")]
    public BaseMission currentMission;

    [Header("Events")]
    public UnityEvent OnAllMissionCompleted;
    public UnityEvent OnMissionFailed;

    protected int currentMissionIndex = -1;

    public bool debugEnabled;

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

    /// <summary>
    /// Resets all top-level missions and restarts the sequence from the beginning.
    /// Call this when the player dies and needs to retry the entire mission chain.
    /// </summary>
    public virtual void RestartGameSequence()
    {
        // Remove event subscriptions from the current mission.
        if (currentMission != null)
            UnregisterMissionEvents(currentMission);

        // Reset all top-level missions.
        if (missions != null)
        {
            foreach (BaseMission mission in missions)
            {
                if (mission == null)
                    continue;

                mission.ResetMission();
            }
        }

        currentMission = null;
        currentMissionIndex = -1;

        if (debugEnabled)
            Debug.Log("[GameManager] Restarting mission sequence from the beginning.");

        StartGame();
    }

    #endregion

    #region Missions

    protected void StartNextMission()
    {
        currentMissionIndex++;

        if (!TryGetMission(currentMissionIndex, out currentMission))
        {
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

        mission.OnMissionCompleted += HandleMissionCompleted;
        mission.OnMissionFailed += HandleMissionFailed;
    }

    protected void UnregisterMissionEvents(BaseMission mission)
    {
        if (mission == null)
            return;

        mission.OnMissionCompleted -= HandleMissionCompleted;
        mission.OnMissionFailed -= HandleMissionFailed;
    }

    protected virtual void HandleMissionCompleted(BaseMission mission)
    {
        if (debugEnabled)
            Debug.Log($"[GameManager] Completed mission: {mission.missionName}");

        UnregisterMissionEvents(mission);
        StartNextMission();
    }

    protected virtual void HandleMissionFailed(BaseMission mission)
    {
        if (debugEnabled)
            Debug.Log($"[GameManager] Failed mission: {mission.missionName}");

        UnregisterMissionEvents(mission);
        MissionFailed();
    }

    #endregion

    #region Hooks (for subclasses)

    protected virtual void AllMissionsCompletedHandler()
    {
        if (debugEnabled)
            Debug.Log("=== All missions completed ===");

        OnAllMissionCompleted?.Invoke();
    }

    protected virtual void MissionFailedHandler()
    {
        OnMissionFailed?.Invoke();
    }

    #endregion
}
