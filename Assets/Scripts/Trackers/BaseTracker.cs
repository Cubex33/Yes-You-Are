using UnityEngine;

/// <summary>
/// Abstract base class for all trackers. Provides save/load integration with SaveManager.
/// </summary>
public abstract class BaseTracker<T> : MonoBehaviour, ITracker
{
    [SerializeField] protected string saveKey;

    public abstract TrackerType Type { get; }
    public string SaveKey => string.IsNullOrEmpty(saveKey) ? GetType().Name : saveKey;

    public abstract object GetValueAsObject();
    public abstract void SetValueFromObject(object value);

    /// <summary>
    /// Loads the tracked value from SaveManager (lazy load on first call).
    /// </summary>
    protected T LoadValue(T defaultValue)
    {
        return (T)SaveManager.Instance.GetGenericValue(SaveKey, defaultValue);
    }

    /// <summary>
    /// Saves the tracked value to SaveManager.
    /// </summary>
    protected void SaveValue(T value)
    {
        SaveManager.Instance.SetGenericValue(SaveKey, value);
    }

    /// <summary>
    /// Persists all pending save operations to disk.
    /// </summary>
    protected void Persist()
    {
        SaveManager.Instance.Save();
    }
}
