using UnityEngine;
using Lucy.Core;
using System.Linq;
using System.Collections.Generic;

public class EntityDeathTracker : MonoBehaviour, IIntegerTracker
{
    public TrackerType Type => TrackerType.Int;
    public string SaveKey => "EntityDeathCount";

    [SerializeField] private string entityTag = "Enemy";
    [SerializeField] private int deathCount = 0;

    public int Value
    {
        get => deathCount;
        set => deathCount = value;
    }

    public object GetValueAsObject() => deathCount;
    public void SetValueFromObject(object value) => deathCount = (int)value;

    void Start()
    {
        deathCount = SaveManager.Instance.GetInt(SaveKey, 0);
        List<Entity> entities = GameObject.FindGameObjectsWithTag(entityTag).Select(go => go.GetComponent<Entity>()).Where(e => e != null).ToList();
        foreach (var entity in entities) entity.OnDeath += (_) => { deathCount++; Save(); };
    }

    public void UpdateDeathCount(TMPro.TMP_Text text) => text.text = deathCount.ToString();
    public void UpdateDeathRecord(TMPro.TMP_Text text) => text.text = SaveManager.Instance.GetInt(SaveGame.deathCountRecordKey, 0).ToString();

    private void Save()
    {
        SaveManager.Instance.SetInt(SaveKey, deathCount);
        SaveManager.Instance.Save();
    }
}
