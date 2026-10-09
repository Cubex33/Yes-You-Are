using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class MissionGroup : BaseMission
{
    public enum FailurePolicy
    {
        FailParent,
        RestartChildren,
        SkipAndContinue,
        Custom
    }

    [Tooltip("If empty, missions will be gathered from direct children in hierarchy.")]
    [SerializeField] private List<BaseMission> children = new();

    [SerializeField] private FailurePolicy failurePolicy = FailurePolicy.FailParent;

    private int currentIndex = -1;

    public BaseMission CurrentChild => (currentIndex >= 0 && currentIndex < children.Count) ? children[currentIndex] : null;
    public Action<BaseMission> OnCurrentChildChanged;

    #region Unity lifecycle

    private void Awake()
    {
        if (children == null || children.Count == 0)
            CacheChildren();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
            CacheChildren();
    }
#endif

    private void OnDisable()
    {
        UnsubscribeFromCurrent();
    }

    private void OnDestroy()
    {
        UnsubscribeFromCurrent();
    }

    #endregion

    private void CacheChildren()
    {
        children ??= new List<BaseMission>();
        children.Clear();

        foreach (Transform t in transform)
        {
            if (!t.gameObject.activeSelf) continue; // Only consider inactive children as missions (optional, based on your design)
            foreach (var mission in t.GetComponents<BaseMission>())
            {
                if (mission != null && mission != this) children.Add(mission);
            }
        }
    }

    public override void StartMission()
    {
        if (isActive)
            return;

        base.StartMission();

        if (children == null || children.Count == 0)
        {
            Debug.LogWarning($"[MissionGroup] {missionName} has no children — completing immediately.");
            CompleteMission();
            return;
        }

        currentIndex = 0;
        StartChildAt(currentIndex);
    }

    private void StartChildAt(int index)
    {
        if (children == null)
        {
            Debug.LogError($"[MissionGroup] {missionName}: children == null");
            return;
        }

        if (index < 0 || index >= children.Count)
        {
            Debug.LogError($"[MissionGroup] {missionName}: StartChildAt index {index} out of range (0..{Mathf.Max(0, children.Count - 1)})");
            return;
        }

        UnsubscribeFromCurrent();

        currentIndex = index;

        var child = CurrentChild;

        if (child == null)
        {
            Debug.LogError($"[MissionGroup] {missionName}: child at index {index} is null.");
            return;
        }

        if (debugEnabled) Debug.Log($"[MissionGroup] {missionName}: Starting child #{currentIndex} '{child.missionName}'");

        child.OnMissionCompleted += OnChildCompleted;
        child.OnMissionFailed += OnChildFailed;

        child.StartMission();
        OnCurrentChildChanged?.Invoke(child);
    }

    private void OnChildCompleted(BaseMission child)
    {
        child.OnMissionCompleted -= OnChildCompleted;
        child.OnMissionFailed -= OnChildFailed;

        if (debugEnabled) Debug.Log($"[MissionGroup] {missionName}: Child completed '{child.missionName}' (index {currentIndex})");

        if (currentIndex + 1 < children.Count)
        {
            StartChildAt(currentIndex + 1);
        }
        else
        {
            if (debugEnabled) Debug.Log($"[MissionGroup] {missionName}: All children completed. Completing group.");
            CompleteMission();
        }
    }

    private void OnChildFailed(BaseMission child)
    {
        child.OnMissionCompleted -= OnChildCompleted;
        child.OnMissionFailed -= OnChildFailed;

        Debug.LogWarning($"[MissionGroup] {missionName}: Child failed '{child.missionName}' (index {currentIndex}) — policy: {failurePolicy}");

        switch (failurePolicy)
        {
            case FailurePolicy.FailParent:
                FailMission();
                break;

            case FailurePolicy.RestartChildren:
                ResetChildrenAndRestart();
                break;

            case FailurePolicy.SkipAndContinue:
                if (currentIndex + 1 < children.Count)
                    StartChildAt(currentIndex + 1);
                else
                    CompleteMission();
                break;

            case FailurePolicy.Custom:
                OnChildFailedCustom(child);
                break;
        }
    }

    protected virtual void OnChildFailedCustom(BaseMission failedChild)
    {
        Debug.LogWarning($"[MissionGroup] {missionName}: Custom failure handler not implemented; defaulting to FailParent behavior.");
        FailMission();
    }

    private void UnsubscribeFromCurrent()
    {
        var c = CurrentChild;
        if (c != null)
        {
            c.OnMissionCompleted -= OnChildCompleted;
            c.OnMissionFailed -= OnChildFailed;
        }
    }

    #region Public navigation API

    public void ChangeState(int index)
    {
        if (children == null)
        {
            Debug.LogWarning($"[MissionGroup] {missionName}: ChangeState: children == null");
            return;
        }

        if (index < 0 || index >= children.Count)
        {
            Debug.LogWarning($"[MissionGroup] {missionName}: ChangeState: invalid index {index}");
            return;
        }

        if (index == currentIndex)
        {
            if (debugEnabled) Debug.Log($"[MissionGroup] {missionName}: ChangeState: already at {index}");
            return;
        }

        if (debugEnabled) Debug.Log($"[MissionGroup] {missionName}: ChangeState -> {index} (from {currentIndex})");
        StartChildAt(index);
    }

    public void ChangeState(BaseMission target)
    {
        if (target == null || children == null)
        {
            Debug.LogWarning($"[MissionGroup] {missionName}: ChangeState(target): invalid target or children.");
            return;
        }

        int idx = children.IndexOf(target);

        if (idx == -1)
        {
            Debug.LogWarning($"[MissionGroup] {missionName}: ChangeState: target not found among children.");
            return;
        }

        ChangeState(idx);
    }

    public void NextState()
    {
        if (children == null)
            return;

        int next = currentIndex + 1;

        if (next < children.Count)
            StartChildAt(next);
        else
        {
            if (debugEnabled) Debug.Log($"[MissionGroup] {missionName}: NextState: no more children -> completing group.");
            CompleteMission();
        }
    }

    #endregion

    #region Reset/rollback

    public void ResetChildrenAndRestart()
    {
        if (debugEnabled) Debug.Log($"[MissionGroup] {missionName}: ResetChildrenAndRestart invoked. Resetting {children?.Count ?? 0} child(ren).");

        UnsubscribeFromCurrent();

        if (children != null)
        {
            for (int i = 0; i < children.Count; i++)
            {
                var c = children[i];

                if (c == null)
                    continue;

                try
                {
                    c.ResetMission();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[MissionGroup] {missionName}: Error resetting child '{c.missionName}': {e}");
                }
            }
        }

        currentIndex = 0;
        StartChildAt(currentIndex);
    }

    public override void ResetMission()
    {
        if (debugEnabled) Debug.Log($"[MissionGroup] {missionName}: ResetMission called.");

        UnsubscribeFromCurrent();

        if (children != null)
        {
            foreach (var c in children)
            {
                if (c == null)
                    continue;

                c.ResetMission();
            }
        }

        currentIndex = -1;
        isActive = false;
        isCompleted = false;
    }

    #endregion
}
