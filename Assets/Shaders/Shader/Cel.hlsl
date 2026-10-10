void CelShading_float(
    in float3 Normal,
    in float ToonRampSmoothness,
    in float3 ClipSpacePos,
    in float3 WorldPos,
    in float3 ToonRampTinting,
    in float ToonRampOffset,
    in float ToonRampOffsetPoint,
    in float Ambient,
    out float3 ToonRampOutput,
    out float3 Direction)
{
#ifdef SHADERGRAPH_PREVIEW
    ToonRampOutput = float3(0.5, 0.5, 0);
    Direction = float3(0.5, 0.5, 0);
#else

    // --- Shadow coord для main light ---
#if SHADOWS_SCREEN
    half4 shadowCoord = ComputeScreenPos(ClipSpacePos);
#else
    half4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
#endif

#if _MAIN_LIGHT_SHADOWS_CASCADE || _MAIN_LIGHT_SHADOWS || _MAIN_LIGHT_SHADOWS_SCREEN
    Light light = GetMainLight(shadowCoord);
#else
    Light light = GetMainLight();
#endif

    // --- Main directional light ---
    half NdotL = saturate(dot(Normal, light.direction) * 0.5 + 0.5);
    half shadowedNdotL = NdotL * light.shadowAttenuation;
    half toonRampMain = smoothstep(ToonRampOffset,
                                   ToonRampOffset + max(ToonRampSmoothness, 0.001),
                                   shadowedNdotL);

    float3 mainLightResult = light.color * toonRampMain + ToonRampTinting * (1.0 - toonRampMain);

    // --- Ambient (SH / sky / light probes) ---
    float3 ambientSH = max(float3(0,0,0), SampleSH(Normal));
    float3 ambientResult = ambientSH * Ambient;

    // --- Additional lights (point, spot) ---
    // Используем raw NdotL без half-lambert для точечных источников —
    // это даёт более реалистичное круглое пятно на плоских поверхностях
    float3 extraLights = float3(0, 0, 0);

#ifdef _ADDITIONAL_LIGHTS
    int pixelLightCount = GetAdditionalLightsCount();
    for (int j = 0; j < pixelLightCount; ++j)
    {
        Light aLight = GetAdditionalLight(j, WorldPos, shadowCoord);

        // Прямой NdotL (не half-lambert) — даёт чёткое круглое пятно на стене
        half NdotL_a = saturate(dot(Normal, aLight.direction));
        half attenuation = aLight.distanceAttenuation * aLight.shadowAttenuation;
        half shadowedNdotL_a = NdotL_a * attenuation;

        half toonRampExtra = smoothstep(ToonRampOffsetPoint,
                                        ToonRampOffsetPoint + max(ToonRampSmoothness, 0.001),
                                        shadowedNdotL_a);
        extraLights += aLight.color * toonRampExtra;
    }
#endif

    // --- Финальная сборка ---
    ToonRampOutput = mainLightResult + ambientResult + extraLights;
    Direction = normalize(light.direction);

#endif
}
