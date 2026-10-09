using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
[DisallowMultipleComponent]
public class SceneSnapshotRestorer : MonoBehaviour
{
    [Header("Root object that contains all restoreable scene objects")]
    public Transform locationRoot;
    [Header("Options")]
    public bool captureOnAwake = true;
    public bool includeInactiveChildren = true;
    [Header("Events")]
    public UnityEvent onSnapshotSaving;
    public UnityEvent onSnapshotRestoring;
    public UnityEvent onSnapshotSaved;
    public UnityEvent onSnapshotRestored;
    [Header("Debug")]
    public bool debugEnabled;
    private GameObject snapshotRoot;
    private bool hasSnapshot;
    private readonly List<Transform> rootsToSave = new List<Transform>(64);
    private void Awake()
    {
        if (locationRoot == null) locationRoot = transform;
        if (captureOnAwake) SaveSnapshot();
    }
    public void SaveSnapshot()
    {
        if (debugEnabled) Debug.Log("[SceneSnapshotRestorer] Saving snapshot.");
        StartCoroutine(SaveSnapshotRoutine());
    }
    private IEnumerator SaveSnapshotRoutine()
    {
        if (locationRoot == null) locationRoot = transform;
        onSnapshotSaving?.Invoke();
        yield return null;
        EnsureSnapshotRoot();
        ClearChildren(snapshotRoot.transform);
        rootsToSave.Clear();
        for (int i = 0; i < locationRoot.childCount; i++)
        {
            Transform child = locationRoot.GetChild(i);
            if (child == null) continue;
            if (!includeInactiveChildren && !child.gameObject.activeSelf) continue;
            rootsToSave.Add(child);
        }
        for (int i = 0; i < rootsToSave.Count; i++)
        {
            Transform source = rootsToSave[i];
            GameObject clone = Instantiate(source.gameObject, snapshotRoot.transform, false);
            clone.name = source.name;
            clone.SetActive(source.gameObject.activeSelf);
            if (i > 0 && i % 10 == 0) yield return null; 
        }
        hasSnapshot = true;
        onSnapshotSaved?.Invoke();
    }
    public void RestoreSnapshot()
    {
        if (debugEnabled) Debug.Log("[SceneSnapshotRestorer] Restoring snapshot.");
        StartCoroutine(RestoreSnapshotRoutine());
    }

    public IEnumerator RestoreSnapshotRoutine()
    {
        if (!hasSnapshot || snapshotRoot == null || locationRoot == null)
        {
            if (debugEnabled) Debug.LogWarning("[SceneSnapshotRestorer] No snapshot to restore or location root is null.");  
            yield break;
        }
        onSnapshotRestoring?.Invoke();
        yield return null;
        ClearChildren(locationRoot);
        Transform snapshotParent = snapshotRoot.transform;
        int childCount = snapshotParent.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform snapshotChild = snapshotParent.GetChild(i);
            if (snapshotChild == null) continue;
            GameObject clone = Instantiate(snapshotChild.gameObject, locationRoot, false);
            clone.name = snapshotChild.name;
            clone.SetActive(snapshotChild.gameObject.activeSelf);
            if (i > 0 && i % 10 == 0) yield return null; 
        }
        onSnapshotRestored?.Invoke();
    }
    public void ClearSnapshot()
    {
        if (snapshotRoot == null) return;
        if (Application.isPlaying) Destroy(snapshotRoot);
        else DestroyImmediate(snapshotRoot);
        snapshotRoot = null;
        hasSnapshot = false;
        rootsToSave.Clear();
    }
    private void EnsureSnapshotRoot()
    {
        if (snapshotRoot != null) return;
        snapshotRoot = new GameObject($"{name}_Snapshot");
        snapshotRoot.SetActive(false);
    }
    private static void ClearChildren(Transform root)
    {
        if (root == null) return;
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Transform child = root.GetChild(i);
            if (child == null) continue;
            if (Application.isPlaying) Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        }
    }
    public void BakeInitializationBaseline()
    {
        RestoreSnapshot();
        SaveSnapshot();
    }
    private void OnDestroy() => ClearSnapshot();
}