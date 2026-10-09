using System;
using UnityEngine;

public static class ScoreSystem
{
    private const string SAVE_KEY = "SCORE_SDK_VALUE";
    private static int _score;
    private static bool _initialized;

    public static event Action<int> OnScoreChanged;

    public static void Init()
    {
        if (_initialized) return;

        _score = PlayerPrefs.GetInt(SAVE_KEY, 0);
        _initialized = true;
    }

    public static int GetScore()
    {
        EnsureInit();
        return _score;
    }

    public static void SetScore(int value)
    {
        EnsureInit();

        _score = Mathf.Max(0, value);
        Save();

        OnScoreChanged?.Invoke(_score);
    }

    public static void Add(int amount)
    {
        EnsureInit();

        if (amount <= 0) return;

        _score += amount;
        Save();

        OnScoreChanged?.Invoke(_score);
    }

    public static bool Spend(int amount)
    {
        EnsureInit();

        if (amount <= 0) return false;
        if (_score < amount) return false;

        _score -= amount;
        Save();

        OnScoreChanged?.Invoke(_score);
        return true;
    }

    public static bool HasEnough(int amount)
    {
        EnsureInit();
        return _score >= amount;
    }

    public static void Reset()
    {
        EnsureInit();

        _score = 0;
        Save();

        OnScoreChanged?.Invoke(_score);
    }

    public static void Save()
    {
        PlayerPrefs.SetInt(SAVE_KEY, _score);
        PlayerPrefs.Save();
    }

    private static void EnsureInit()
    {
        if (!_initialized)
            Init();
    }
}