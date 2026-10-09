using UnityEngine;
using TMPro;

public class FPSDisplay : MonoBehaviour
{
    public TMP_Text fpsText;
    private void Update()
    {
        float fps = 1f / Time.deltaTime;
        fpsText.text = $"FPS: {fps:F2}";
    }
}