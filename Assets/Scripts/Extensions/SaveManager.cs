using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Universal save/load system using PlayerPrefs with lazy loading.
/// Values are only loaded from PlayerPrefs when first accessed.
/// </summary>
public class SaveManager
{
    private static SaveManager _instance;
    public static SaveManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = new SaveManager();
            return _instance;
        }
    }

    private readonly Dictionary<string, object> _loadedValues = new Dictionary<string, object>();
    private readonly HashSet<string> _requestedKeys = new HashSet<string>();

    private SaveManager() { }

    /// <summary>
    /// Gets an int value by key. Loads from PlayerPrefs only on first access.
    /// </summary>
    public int GetInt(string key, int defaultValue = 0)
    {
        if (!_requestedKeys.Contains(key))
        {
            _loadedValues[key] = PlayerPrefs.GetInt(key, defaultValue);
            _requestedKeys.Add(key);
        }
        return (int)_loadedValues[key];
    }

    /// <summary>
    /// Gets a float value by key. Loads from PlayerPrefs only on first access.
    /// </summary>
    public float GetFloat(string key, float defaultValue = 0f)
    {
        if (!_requestedKeys.Contains(key))
        {
            _loadedValues[key] = PlayerPrefs.GetFloat(key, defaultValue);
            _requestedKeys.Add(key);
        }
        return (float)_loadedValues[key];
    }

    /// <summary>
    /// Gets a string value by key. Loads from PlayerPrefs only on first access.
    /// </summary>
    public string GetString(string key, string defaultValue = "")
    {
        if (!_requestedKeys.Contains(key))
        {
            _loadedValues[key] = PlayerPrefs.GetString(key, defaultValue);
            _requestedKeys.Add(key);
        }
        return (string)_loadedValues[key];
    }

    /// <summary>
    /// Sets an int value in memory. Does NOT write to PlayerPrefs immediately.
    /// Call Save() to persist all pending changes.
    /// </summary>
    public void SetInt(string key, int value)
    {
        _loadedValues[key] = value;
        _requestedKeys.Add(key);
        PlayerPrefs.SetInt(key, value);
    }

    /// <summary>
    /// Sets a float value in memory. Does NOT write to PlayerPrefs immediately.
    /// Call Save() to persist all pending changes.
    /// </summary>
    public void SetFloat(string key, float value)
    {
        _loadedValues[key] = value;
        _requestedKeys.Add(key);
        PlayerPrefs.SetFloat(key, value);
    }

    /// <summary>
    /// Sets a string value in memory. Does NOT write to PlayerPrefs immediately.
    /// Call Save() to persist all pending changes.
    /// </summary>
    public void SetString(string key, string value)
    {
        _loadedValues[key] = value;
        _requestedKeys.Add(key);
        PlayerPrefs.SetString(key, value);
    }

    /// <summary>
    /// Gets a value by key with generic type. Loads from PlayerPrefs only on first access.
    /// </summary>
    public T GetGenericValue<T>(string key, T defaultValue)
    {
        if (!_requestedKeys.Contains(key))
        {
            _loadedValues[key] = GetTypedValue(key, defaultValue);
            _requestedKeys.Add(key);
        }
        return (T)_loadedValues[key];
    }

    /// <summary>
    /// Sets a value by key with generic type. Does NOT write to PlayerPrefs immediately.
    /// Call Save() to persist all pending changes.
    /// </summary>
    public void SetGenericValue<T>(string key, T value)
    {
        _loadedValues[key] = value;
        _requestedKeys.Add(key);
        SetTypedValue(key, value);
    }

    private object GetTypedValue(string key, object defaultValue)
    {
        switch (defaultValue)
        {
            case int i: return PlayerPrefs.GetInt(key, i);
            case float f: return PlayerPrefs.GetFloat(key, f);
            case string s: return PlayerPrefs.GetString(key, s);
            case bool b: return PlayerPrefs.GetInt(key, b ? 1 : 0) == 1;
            default: return defaultValue;
        }
    }

    private void SetTypedValue(string key, object value)
    {
        switch (value)
        {
            case int i: PlayerPrefs.SetInt(key, i); break;
            case float f: PlayerPrefs.SetFloat(key, f); break;
            case string s: PlayerPrefs.SetString(key, s); break;
            case bool b: PlayerPrefs.SetInt(key, b ? 1 : 0); break;
        }
    }

    /// <summary>
    /// Persists all pending changes to PlayerPrefs.
    /// </summary>
    public void Save()
    {
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Checks if a key has been accessed/loaded.
    /// </summary>
    public bool HasKey(string key)
    {
        return _requestedKeys.Contains(key);
    }

    /// <summary>
    /// Removes a value from memory and PlayerPrefs.
    /// </summary>
    public void DeleteKey(string key)
    {
        _loadedValues.Remove(key);
        _requestedKeys.Remove(key);
        PlayerPrefs.DeleteKey(key);
    }

    /// <summary>
    /// Clears all tracked values and PlayerPrefs data.
    /// </summary>
    public void DeleteAll()
    {
        _loadedValues.Clear();
        _requestedKeys.Clear();
        PlayerPrefs.DeleteAll();
    }

    /// <summary>
    /// Resets the manager state (keeps PlayerPrefs data intact).
    /// Values will be reloaded on next access.
    /// </summary>
    public void Reset()
    {
        _loadedValues.Clear();
        _requestedKeys.Clear();
    }
}
