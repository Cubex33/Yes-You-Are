#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public static class BossInfernoSetup
{
    const string Root = "Assets/BossCinematicVFX/Prefabs/";

    [MenuItem("Tools/Boss Inferno/1. Generate All Prefabs")]
    public static void GenerateAll()
    {
        BossCinematicVFXGenerator.Generate();
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "FireProjectile.prefab");
        if (source == null) { Debug.LogError("FireProjectile prefab not generated"); return; }
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
        instance.name = "REAL_Fireball_Projectile";
        if (instance.GetComponent<FireballProjectile>() == null) instance.AddComponent<FireballProjectile>();
        // ParticleSystem components are children. Projectile movement and spherecasts live on the root.
        PrefabUtility.SaveAsPrefabAsset(instance, Root + "REAL_Fireball_Projectile.prefab");
        Object.DestroyImmediate(instance);
        AssetDatabase.SaveAssets();
        Debug.Log("BOSS INFERNO: ALL VFX + REAL FIREBALL PREFAB GENERATED.");
    }

    [MenuItem("Tools/Boss Inferno/2. Auto-Assign Effects to Selected Boss")]
    public static void Assign()
    {
        Boss boss = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<Boss>() : null;
        if (boss == null) { EditorUtility.DisplayDialog("Boss Inferno", "Select the GameObject with Boss component first.", "OK"); return; }
        SerializedObject so = new SerializedObject(boss);
        Set(so, "fireballPrefab", Root + "REAL_Fireball_Projectile.prefab", typeof(FireballProjectile));
        Set(so, "fireballImpactPrefab", Root + "FireSlam.prefab", typeof(ParticleSystem));
        Set(so, "spawnVFX", Root + "FireSpawn.prefab", typeof(ParticleSystem));
        Set(so, "comboVFX", Root + "FireCombo.prefab", typeof(ParticleSystem));
        Set(so, "slamVFX", Root + "FireSlam.prefab", typeof(ParticleSystem));
        Set(so, "summonVFX", Root + "FireSummon.prefab", typeof(ParticleSystem));
        Set(so, "dashVFX", Root + "FireDash.prefab", typeof(ParticleSystem));
        Set(so, "rageVFX", Root + "FireRage.prefab", typeof(ParticleSystem));
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(boss);
        Debug.Log("Boss Inferno: VFX and projectile assigned to " + boss.name);
    }

    private static void Set(SerializedObject so, string field, string path, System.Type type)
    {
        SerializedProperty property = so.FindProperty(field);
        if (property == null) { Debug.LogError("Missing Boss field: " + field); return; }
        GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (go == null) { Debug.LogError("Missing prefab: " + path); return; }
        Object component = type == typeof(FireballProjectile) ? (Object)go.GetComponent<FireballProjectile>() : go.GetComponent<ParticleSystem>();
        property.objectReferenceValue = component;
    }
}
#endif
