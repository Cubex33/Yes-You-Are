using UnityEditor;
using UnityEngine;

public class IMDCharacterAnimeShaderGUI : ShaderGUI
{
    static MaterialProperty Find(MaterialProperty[] p, string n) => FindProperty(n, p, false);

    static void Prop(MaterialEditor me, MaterialProperty[] p, string n)
    {
        var x = Find(p, n);
        if (x != null) me.ShaderProperty(x, x.displayName);
    }

    static void Tex(MaterialEditor me, MaterialProperty[] p, string n)
    {
        var x = Find(p, n);
        if (x != null) me.TexturePropertySingleLine(new GUIContent(x.displayName), x);
    }

    static bool Section(string title)
    {
        string key = "IMDCharacterV12_" + title;
        bool open = SessionState.GetBool(key, true);
        bool next = EditorGUILayout.Foldout(open, title, true);
        if (next != open) SessionState.SetBool(key, next);
        return next;
    }

    public override void OnGUI(MaterialEditor me, MaterialProperty[] p)
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("I.M.D Character Anime Shader V1.2", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "LOCAL LIGHTING ONLY. No Directional/Main Light is used. " +
            "Point and Spot Lights provide the real dynamic light and realtime shadows. " +
            "Shadow controls are the priority.",
            MessageType.Info
        );

        if (Section("Surface"))
        {
            EditorGUI.indentLevel++;
            Tex(me, p, "_MainTex");
            Prop(me, p, "_Color");
            Prop(me, p, "_AlphaClip");
            EditorGUI.indentLevel--;
        }

        if (Section("Shadow — Priority"))
        {
            EditorGUI.indentLevel++;
            Prop(me, p, "_ShadowColor");
            Prop(me, p, "_ShadowStrength");
            Prop(me, p, "_ShadowThreshold");
            Prop(me, p, "_ShadowSoftness");
            Prop(me, p, "_ShadowFloor");
            Prop(me, p, "_LightResponse");
            Prop(me, p, "_UseLightShadows");
            EditorGUILayout.HelpBox(
                "Shadow Threshold decides where the toon shadow begins. " +
                "Shadow Softness = 0 gives a crisp anime edge; higher values soften the transition. " +
                "Shadow Floor protects painted texture from being crushed to black.",
                MessageType.None
            );
            EditorGUI.indentLevel--;
        }

        if (Section("Emission"))
        {
            EditorGUI.indentLevel++;
            Prop(me, p, "_UseEmission");
            Prop(me, p, "_UseEmissionMask");
            Tex(me, p, "_EmissionMask");
            Prop(me, p, "_EmissionColor");
            Prop(me, p, "_EmissionStrength");
            EditorGUI.indentLevel--;
        }

        if (Section("Outline"))
        {
            EditorGUI.indentLevel++;
            Prop(me, p, "_UseOutline");
            Prop(me, p, "_OutlineWidth");
            Prop(me, p, "_OutlineColor");
            EditorGUI.indentLevel--;
        }

        if (Section("Rendering"))
        {
            EditorGUI.indentLevel++;
            Prop(me, p, "_RenderFace");
            EditorGUI.indentLevel--;
        }
    }
}
