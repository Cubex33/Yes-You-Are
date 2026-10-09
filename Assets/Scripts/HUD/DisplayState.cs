using System;
using TMPro;
using UnityEngine;

public class DisplayState : MonoBehaviour
{
    public TMP_Text text;
    public EnemyBase enemy;
    public void Awake()
    {
        enemy.OnStateChanged += UpdateStateDisplay;
    }

    private void UpdateStateDisplay()
    {
        if (text == null) return;
        Debug.Log("Enemy state changed: " + enemy.CurrentState);
        text.text = enemy.CurrentState.ToString();
    }
}