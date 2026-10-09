using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

public class CopyComponentsWithOptionsEditor : EditorWindow
{
    GameObject sourcePrefab;
    GameObject targetPrefab;

    bool copyValues = true;
    bool rewriteExisting = false;

    [MenuItem("Tools/Prefab/Copy Components With Options")]
    public static void ShowWindow()
    {
        GetWindow<CopyComponentsWithOptionsEditor>("Copy Components With Options");
    }

    void OnGUI()
    {
        GUILayout.Label("Копирование компонентов с настройками", EditorStyles.boldLabel);

        sourcePrefab = (GameObject)EditorGUILayout.ObjectField("Источник", sourcePrefab, typeof(GameObject), true);
        targetPrefab = (GameObject)EditorGUILayout.ObjectField("Цель", targetPrefab, typeof(GameObject), true);

        copyValues = EditorGUILayout.Toggle("Копировать значения", copyValues);
        rewriteExisting = EditorGUILayout.Toggle("Перезаписывать существующие", rewriteExisting);

        if (GUILayout.Button("Скопировать компоненты"))
        {
            if (sourcePrefab == null || targetPrefab == null)
            {
                Debug.LogWarning("Выберите оба префаба!");
                return;
            }

            CopyComponentsWithOptions();
        }
    }

    void CopyComponentsWithOptions()
    {
        var sourceComponents = sourcePrefab.GetComponents<Component>();

        // Счётчик, чтобы различать одинаковые типы компонентов
        var typeCounters = new Dictionary<Type, int>();

        foreach (var sourceComp in sourceComponents)
        {
            if (sourceComp is Transform) continue;

            Type type = sourceComp.GetType();

            if (!typeCounters.ContainsKey(type))
                typeCounters[type] = 0;

            int index = typeCounters[type]++;
            var targetSameType = targetPrefab.GetComponents(type);

            Component targetComp = index < targetSameType.Length ? targetSameType[index] : null;

            if (targetComp != null && rewriteExisting)
            {
                if (copyValues)
                {
                    UnityEditorInternal.ComponentUtility.CopyComponent(sourceComp);
                    UnityEditorInternal.ComponentUtility.PasteComponentValues(targetComp);
                }
            }
            else
            {
                if (copyValues)
                {
                    UnityEditorInternal.ComponentUtility.CopyComponent(sourceComp);
                    UnityEditorInternal.ComponentUtility.PasteComponentAsNew(targetPrefab);
                }
                else
                {
                    targetPrefab.AddComponent(type);
                }
            }
        }

        Debug.Log("Копирование компонентов завершено!");
    }
}