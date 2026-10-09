using Lucy.Core;
using TMPro;
using UnityEngine;

public class PlayerMaxSpeedTracker : MonoBehaviour, IFloatTracker
{
    public TrackerType Type => TrackerType.Float;
    public string SaveKey => "PlayerMaxSpeed";

    [SerializeField] private float maxSpeed = 0f;
    public Movement movement;

    public float Value
    {
        get => maxSpeed;
        set => maxSpeed = value;
    }

    public object GetValueAsObject() => maxSpeed;
    public void SetValueFromObject(object value) => maxSpeed = (float)value;

    void Start()
    {
        maxSpeed = SaveManager.Instance.GetFloat(SaveKey, 0f);
    }

    void FixedUpdate()
    {
        if (movement.currentHorizontalVelocity.magnitude > maxSpeed)
        {
            maxSpeed = movement.currentHorizontalVelocity.magnitude;
            Save();
        }
    }

    public void UpdateMaxSpeed(TMP_Text text) => text.text = maxSpeed.ToString();
    public void UpdateMaxSpeedRecord(TMP_Text text) => text.text = SaveManager.Instance.GetFloat(SaveGame.maxSpeedRecordKey, 0f).ToString();

    private void Save()
    {
        SaveManager.Instance.SetFloat(SaveKey, maxSpeed);
        SaveManager.Instance.Save();
    }
}