// Mission layer
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

    protected bool isActive = false;
    protected bool isCompleted = false;

    public virtual void StartMission()
    {
        if (isActive) return;


        isActive = true;
        isCompleted = false;
        OnMissionStarted?.Invoke(this);
    }

    public virtual void ResetMission(){}

    protected virtual void CompleteMission()
    {
        if (!isActive || isCompleted) return;

        isActive = false;
        isCompleted = true;
        OnMissionCompleted?.Invoke(this);

        if (debugEnabled) Debug.Log($"[BaseMission] Mission completed: {missionName}");
    }

    protected virtual void FailMission()
    {
        if (!isActive || isCompleted) return;

        isActive = false;
        isCompleted = false;
        OnMissionFailed?.Invoke(this);

        if (debugEnabled) Debug.Log($"[BaseMission] Mission failed: {missionName}");
    }
}