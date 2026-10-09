using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ExitGameButton : MonoBehaviour
{
    public bool debugEnabled;

    public void OnExitButtonPressed()
    {
        if (debugEnabled) Debug.Log("Выход из игры...");
        #if UNITY_EDITOR
        EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}