using System;
using TMPro;
using UnityEngine;

public class DisplayScore : MonoBehaviour
{
    public TMP_Text scoreText;
    void Awake()
    {
        if (scoreText == null) Debug.LogError("DisplayScore: No TextMeshPro text component assigned, score will not be displayed.");
        else scoreText.text = ScoreSystem.GetScore().ToString();
        ScoreSystem.OnScoreChanged += IncreaseScore;
    }
    void OnDestroy() => ScoreSystem.OnScoreChanged -= IncreaseScore;
    private void IncreaseScore(int obj) => scoreText.text = ScoreSystem.GetScore().ToString();
}