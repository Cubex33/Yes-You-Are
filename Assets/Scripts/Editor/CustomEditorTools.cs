using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

public static class CustomEditorTools
{
    [MenuItem("Tools/Recompile Scripts #&r")] // Ctrl+R
    public static void RecompileScripts() => AssetDatabase.Refresh();
    static void ClearConsole()
    {
        var assembly = System.Reflection.Assembly.GetAssembly(typeof(SceneView));
        var type = assembly.GetType("UnityEditor.LogEntries");
        var method = type.GetMethod("Clear");
        method.Invoke(null, null);
    }
}

[InitializeOnLoad]
public class AutoSaveOnPlay
{
    static AutoSaveOnPlay() => EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    static void OnPlayModeStateChanged(PlayModeStateChange state) { if (state == PlayModeStateChange.ExitingEditMode && EditorSceneManager.SaveOpenScenes()) Debug.Log("Auto-save of the scene before starting Play Mode is completed"); }
}