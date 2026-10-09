using Lucy.Core;
using UnityEngine;

public class SaveGame : MonoBehaviour
{
    public PlayerMaxSpeedTracker speedTracker;
    public EntityDeathTracker deathTracker;
    static public string maxSpeedRecordKey = "RecordMaxSpeed";
    static public string deathCountRecordKey = "RecordDeathCount";
    public void Save()
    {
        SaveManager.Instance.SetFloat(speedTracker.SaveKey, speedTracker.Value);
        SaveManager.Instance.SetInt(deathTracker.SaveKey, deathTracker.Value);
        SaveManager.Instance.Save();
    }

    public void Load()
    {
        speedTracker.Value = SaveManager.Instance.GetFloat(speedTracker.SaveKey, 0f);
        deathTracker.Value = SaveManager.Instance.GetInt(deathTracker.SaveKey, 0);
    }

    public void Reset()
    {
        SaveManager.Instance.SetFloat(speedTracker.SaveKey, 0f);
        SaveManager.Instance.SetInt(deathTracker.SaveKey, 0);
        SaveManager.Instance.Save();
        Load();
    }

    public void Delete()
    {
        SaveManager.Instance.DeleteKey(speedTracker.SaveKey);
        SaveManager.Instance.DeleteKey(deathTracker.SaveKey);
        SaveManager.Instance.Save();
        Load();
    }

    public void DeleteAll()
    {
        SaveManager.Instance.DeleteAll();
        SaveManager.Instance.Save();
        Load();
    }

    public void SaveRecords()
    {
        if ((float)speedTracker.Value > SaveManager.Instance.GetFloat(maxSpeedRecordKey, 0f) || deathTracker.Value > SaveManager.Instance.GetInt(deathCountRecordKey, 0))
        {
            if ((float)speedTracker.Value > SaveManager.Instance.GetFloat(maxSpeedRecordKey, 0f)) SaveManager.Instance.SetFloat(maxSpeedRecordKey, (float)speedTracker.Value);
            if (deathTracker.Value > SaveManager.Instance.GetInt(deathCountRecordKey, 0)) SaveManager.Instance.SetInt(deathCountRecordKey, deathTracker.Value);
            SaveManager.Instance.Save();
        }
    }
}