using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class IMDAnimeShaderGUI : ShaderGUI
{
    private static bool surfaceOpen = true;
    private static bool detailsOpen = true;
    private static bool colorOpen = true;
    private static bool secondTextureOpen = false;
    private static bool normalOpen = false;
    private static bool specularOpen = true;
    private static bool emissionOpen = false;
    private static bool outlineOpen = true;
    private static readonly Dictionary<string, string> hexBuffers = new Dictionary<string, string>();

    private static bool Foldout(string title, bool state)
    {
        EditorGUILayout.Space(4);
        return EditorGUILayout.BeginFoldoutHeaderGroup(state, title);
    }

    private static void EndFoldout()
    {
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    private static MaterialProperty P(MaterialProperty[] props, string name)
    {
        return FindProperty(name, props, false);
    }

    private static void Property(MaterialEditor editor, MaterialProperty[] props, string name)
    {
        MaterialProperty p = P(props, name);
        if (p != null)
            editor.ShaderProperty(p, p.displayName);
    }

    private static bool Toggle(MaterialEditor editor, MaterialProperty[] props, string propertyName, string keyword)
    {
        MaterialProperty p = P(props, propertyName);
        if (p == null)
            return false;

        editor.ShaderProperty(p, p.displayName);

        bool enabled = p.floatValue > 0.5f;

        foreach (Object target in editor.targets)
        {
            Material material = target as Material;
            if (material == null)
                continue;

            if (enabled)
                material.EnableKeyword(keyword);
            else
                material.DisableKeyword(keyword);
        }

        return enabled;
    }

    private static string ColorToHex(Color color)
    {
        return ColorUtility.ToHtmlStringRGB(color);
    }

    private static bool TryParseHexColor(string input, out Color color)
    {
        color = Color.white;

        if (string.IsNullOrWhiteSpace(input))
            return false;

        string hex = input.Trim();
        if (hex.StartsWith("#"))
            hex = hex.Substring(1);

        if (hex.Length == 6)
            hex += "FF";

        if (hex.Length != 8)
            return false;

        return ColorUtility.TryParseHtmlString("#" + hex, out color);
    }

    private static void ColorPropertyWithHex(MaterialEditor editor, MaterialProperty[] props, string propertyName)
    {
        MaterialProperty p = P(props, propertyName);
        if (p == null)
            return;

        editor.ShaderProperty(p, p.displayName);

        string currentHex = ColorToHex(p.colorValue);

        string buffer;
        if (!hexBuffers.TryGetValue(propertyName, out buffer))
            buffer = currentHex;

        string controlName = "IMD_ColorHexField_" + propertyName;

        if (GUI.GetNameOfFocusedControl() != controlName)
            buffer = currentHex;

        GUI.SetNextControlName(controlName);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Hex");

        string entered = EditorGUILayout.TextField(buffer);
        buffer = entered;

        EditorGUILayout.EndHorizontal();

        hexBuffers[propertyName] = buffer;

        Color parsed;
        if (TryParseHexColor(entered, out parsed))
        {
            if (Vector4.Distance(p.colorValue, parsed) > 0.0001f)
                p.colorValue = parsed;
        }
    }

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        Material material = materialEditor.target as Material;

        EditorGUI.BeginChangeCheck();

        // ---------------- SURFACE ----------------
        surfaceOpen = Foldout("Surface Inputs", surfaceOpen);
        if (surfaceOpen)
        {
            EditorGUI.indentLevel++;

            Property(materialEditor, properties, "_MainTex");
            Property(materialEditor, properties, "_TextureScale");
            Property(materialEditor, properties, "_TextureOffset");
            ColorPropertyWithHex(materialEditor, properties, "_Color");
            Property(materialEditor, properties, "_RenderFace");

            EditorGUI.indentLevel--;
            EndFoldout();
        }
        else
        {
            EndFoldout();
        }

        // ---------------- DETAILS ----------------
        // Cel shading + all lighting controls live together here.
        detailsOpen = Foldout("Details", detailsOpen);
        if (detailsOpen)
        {
            EditorGUI.indentLevel++;

            // Shadow / cel controls stay directly with the shadow color.
            ColorPropertyWithHex(materialEditor, properties, "_ShadingColor");
            Property(materialEditor, properties, "_CelShaderOffset");
            Property(materialEditor, properties, "_CelRampSmoothness");
            Property(materialEditor, properties, "_ShadowStrength");

            EditorGUILayout.Space(4);

            // When enabled, actual URP scene lights drive the material.
            // When disabled, the material uses its own Light Color /
            // Light Strength while the main light still provides direction
            // and the cel split.
            Toggle(
                materialEditor,
                properties,
                "_UseRealLighting",
                "_IMD_REAL_LIGHTING"
            );

            // Light side controls.
            ColorPropertyWithHex(materialEditor, properties, "_LightColor");
            Property(materialEditor, properties, "_LightStrength");
            Property(materialEditor, properties, "_LightBoost");
            Property(materialEditor, properties, "_LightContrast");

            EditorGUILayout.Space(4);

            bool rimEnabled = Toggle(
                materialEditor,
                properties,
                "_UseRimLighting",
                "_IMD_RIM_LIGHTING"
            );

            if (rimEnabled)
            {
                ColorPropertyWithHex(materialEditor, properties, "_RimColor");
                Property(materialEditor, properties, "_RimLighting");
                Property(materialEditor, properties, "_RimWidth");
                Property(materialEditor, properties, "_RimPower");
                Property(materialEditor, properties, "_RimBrightness");
                Property(materialEditor, properties, "_RimAffectShadow");
            }

            ColorPropertyWithHex(materialEditor, properties, "_AmbientColor");
            Property(materialEditor, properties, "_AmbientSelfLighting");
            Property(materialEditor, properties, "_AlphaClipThreshold");

            EditorGUI.indentLevel--;
            EndFoldout();
        }
        else
        {
            EndFoldout();
        }

        // ---------------- COLOR ----------------
        colorOpen = Foldout("Color Adjustments", colorOpen);
        if (colorOpen)
        {
            EditorGUI.indentLevel++;

            Property(materialEditor, properties, "_Brightness");
            Property(materialEditor, properties, "_Contrast");
            Property(materialEditor, properties, "_Gamma");
            Property(materialEditor, properties, "_Hue");
            Property(materialEditor, properties, "_Saturation");

            EditorGUI.indentLevel--;
            EndFoldout();
        }
        else
        {
            EndFoldout();
        }

        // ---------------- SECOND TEXTURE ----------------
        secondTextureOpen = Foldout("Second Texture", secondTextureOpen);
        if (secondTextureOpen)
        {
            EditorGUI.indentLevel++;

            bool enabled = Toggle(
                materialEditor,
                properties,
                "_UseSecondTexture",
                "_IMD_SECOND_TEXTURE"
            );

            if (enabled)
            {
                Property(materialEditor, properties, "_SecondTex");
                Property(materialEditor, properties, "_SecondTexColor");
                Property(materialEditor, properties, "_SecondTexOpacity");
                Property(materialEditor, properties, "_SecondTexScale");
                Property(materialEditor, properties, "_SecondTexOffset");
                Property(materialEditor, properties, "_SecondTexBlend");
            }

            EditorGUI.indentLevel--;
            EndFoldout();
        }
        else
        {
            EndFoldout();
        }

        // ---------------- NORMAL ----------------
        normalOpen = Foldout("Normal", normalOpen);
        if (normalOpen)
        {
            EditorGUI.indentLevel++;

            bool enabled = Toggle(
                materialEditor,
                properties,
                "_UseNormalMap",
                "_IMD_NORMAL_MAP"
            );

            if (enabled)
            {
                Property(materialEditor, properties, "_BumpMap");
                Property(materialEditor, properties, "_BumpScale");
            }

            EditorGUI.indentLevel--;
            EndFoldout();
        }
        else
        {
            EndFoldout();
        }

        // ---------------- SPECULAR ----------------
        specularOpen = Foldout("Specular", specularOpen);
        if (specularOpen)
        {
            EditorGUI.indentLevel++;

            bool enabled = Toggle(
                materialEditor,
                properties,
                "_UseSpecular",
                "_IMD_SPECULAR"
            );

            if (enabled)
            {
                Property(materialEditor, properties, "_SpecularColor");
                Property(materialEditor, properties, "_SpecularStrength");
                Property(materialEditor, properties, "_SpecularSize");
                Property(materialEditor, properties, "_SpecularHardness");
            }

            EditorGUI.indentLevel--;
            EndFoldout();
        }
        else
        {
            EndFoldout();
        }

        // ---------------- EMISSION ----------------
        emissionOpen = Foldout("Emission", emissionOpen);
        if (emissionOpen)
        {
            EditorGUI.indentLevel++;

            bool enabled = Toggle(
                materialEditor,
                properties,
                "_UseEmission",
                "_IMD_EMISSION"
            );

            if (enabled)
            {
                bool useMask = Toggle(
                    materialEditor,
                    properties,
                    "_UseEmissionMask",
                    "_IMD_EMISSION_MASK"
                );

                if (useMask)
                    Property(materialEditor, properties, "_EmissionsMask");

                Property(materialEditor, properties, "_EmissionsColor");
                Property(materialEditor, properties, "_EmissionStrength");
            }

            EditorGUI.indentLevel--;
            EndFoldout();
        }
        else
        {
            EndFoldout();
        }

        // ---------------- OUTLINE ----------------
        outlineOpen = Foldout("Outline", outlineOpen);
        if (outlineOpen)
        {
            EditorGUI.indentLevel++;

            bool enabled = Toggle(
                materialEditor,
                properties,
                "_UseOutline",
                "_IMD_OUTLINE"
            );

            if (enabled)
            {
                Property(materialEditor, properties, "_OutlineWidth");
                Property(materialEditor, properties, "_OutlineColor");
                Property(materialEditor, properties, "_OutlineZOffset");
                Property(materialEditor, properties, "_ZOffsetMaskRemapStart");
                Property(materialEditor, properties, "_ZOffsetMaskRemapEnd");
                Property(materialEditor, properties, "_IsFace");
                Property(materialEditor, properties, "_OutlineZOffsetMaskTex");
                Property(materialEditor, properties, "_OutlineDistanceFade");
                Property(materialEditor, properties, "_OutlineFadeStart");
                Property(materialEditor, properties, "_OutlineFadeEnd");
            }

            EditorGUI.indentLevel--;
            EndFoldout();
        }
        else
        {
            EndFoldout();
        }

        if (EditorGUI.EndChangeCheck())
        {
            foreach (Object target in materialEditor.targets)
            {
                Material targetMaterial = target as Material;
                if (targetMaterial == null)
                    continue;

                targetMaterial.SetFloat("_UseSecondTexture",
                    targetMaterial.GetFloat("_UseSecondTexture"));
            }

            EditorUtility.SetDirty(material);
        }
    }
}
