
using System;
using UnityEngine;

public abstract class BaseMission : MonoBehaviour
{
    [Header("Mission Info")]
    public string missionName = "Unnamed Mission";
    public string missionDescription = "No description";

    [Header("Debug")]
    public bool debugEnabled;

    public event Action<BaseMission> OnMissionStarted;
    public event Action<BaseMission> OnMissionCompleted;
    public event Action<BaseMission> OnMissionFailed;

    protected bool isActive;
    protected bool isCompleted;

    public bool IsActive => isActive;
    public bool IsCompleted => isCompleted;

    public virtual void StartMission()
    {
        if (isActive || isCompleted)
            return;

        isActive = true;
        isCompleted = false;

        if (debugEnabled)
            Debug.Log($"[BaseMission] Started: {missionName}");

        OnMissionStarted?.Invoke(this);
    }

    public virtual void ResetMission()
    {
        isActive = false;
        isCompleted = false;

        if (debugEnabled)
            Debug.Log($"[BaseMission] Reset: {missionName}");
    }

    protected virtual void CompleteMission()
    {
        if (!isActive || isCompleted)
            return;

        isActive = false;
        isCompleted = true;

        if (debugEnabled)
            Debug.Log($"[BaseMission] Completed: {missionName}");

        OnMissionCompleted?.Invoke(this);
    }

    protected virtual void FailMission()
    {
        if (!isActive || isCompleted)
            return;

        isActive = false;
        isCompleted = false;

        if (debugEnabled)
            Debug.Log($"[BaseMission] Failed: {missionName}");

        OnMissionFailed?.Invoke(this);
    }
}
