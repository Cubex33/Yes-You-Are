using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class ReplaceWithPrefabWindow : EditorWindow
{
    private GameObject prefabAsset;

    [MenuItem("Tools/Prefab/Replace Selected With Prefab")]
    private static void Open()
    {
        GetWindow<ReplaceWithPrefabWindow>("Replace With Prefab");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Prefab Asset", EditorStyles.boldLabel);
        prefabAsset = (GameObject)EditorGUILayout.ObjectField(prefabAsset, typeof(GameObject), false);

        EditorGUILayout.Space(10);

        GUI.enabled = prefabAsset != null;
        if (GUILayout.Button("Replace Selected Objects"))
        {
            ReplaceSelected();
        }
        GUI.enabled = true;
    }

    private void ReplaceSelected()
    {
        if (prefabAsset == null)
        {
            Debug.LogError("Prefab asset is not assigned.");
            return;
        }

        if (!PrefabUtility.IsPartOfPrefabAsset(prefabAsset))
        {
            Debug.LogError("Assigned object must be a prefab asset, not a scene object.");
            return;
        }

        var selected = Selection.gameObjects;
        if (selected == null || selected.Length == 0)
        {
            Debug.LogWarning("No objects selected.");
            return;
        }

        foreach (var oldObj in selected)
        {
            var parent = oldObj.transform.parent;
            int siblingIndex = oldObj.transform.GetSiblingIndex();
            var localPosition = oldObj.transform.localPosition;
            var localRotation = oldObj.transform.localRotation;
            var localScale = oldObj.transform.localScale;
            bool wasActive = oldObj.activeSelf;
            string newName = oldObj.name;

            Undo.RegisterFullObjectHierarchyUndo(oldObj, "Replace With Prefab");

            GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset, oldObj.scene);
            Undo.RegisterCreatedObjectUndo(newObj, "Create Prefab Instance");

            if (parent != null)
                newObj.transform.SetParent(parent, false);

            newObj.transform.SetSiblingIndex(siblingIndex);
            newObj.transform.localPosition = localPosition;
            newObj.transform.localRotation = localRotation;
            newObj.transform.localScale = localScale;
            newObj.SetActive(wasActive);
            newObj.name = newName;

            Undo.DestroyObjectImmediate(oldObj);
            EditorSceneManager.MarkSceneDirty(newObj.scene);
        }
    }
}