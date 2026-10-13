Shader "I.M.D/Anime Cel Web Optimized"
{
    Properties
    {
        [Header(Main)]
        [MainTexture] _MainTex("Base Map", 2D) = "white" {}
        [MainColor] _Color("Base Color", Color) = (1,1,1,1)
        _Texture_Scale("Texture Scale", Vector) = (1,1,0,0)
        _Texture_Offset("Texture Offset", Vector) = (0,0,0,0)
        [Toggle(_ALPHATEST_ON)] _AlphaClip("Alpha Clipping", Float) = 0
        _Alpha_Clip_Threshold("Alpha Clip Threshold", Range(0,1)) = 0.5

        [Header(Cel Shading)]
        [Toggle(_CEL_SHADING)] _UseCelShading("Enable Cel Shading", Float) = 1
        _Shading_Color("Shadow Color", Color) = (0.49,0.49,0.49,1)
        _Cel_Shader_Offset("Cel Threshold", Range(0,1)) = 0.5
        _Cel_Ramp_Smoothness("Cel Transition Softness", Range(0,0.5)) = 0.03
        _Ambient_Self_Lighting("Ambient Contribution", Range(0,1)) = 0.25

        [Header(Optional Effects)]
        [Toggle(_RIM_LIGHTING)] _UseRimLighting("Enable Rim Lighting", Float) = 0
        _Rim_Lighting("Rim Threshold", Range(-1,1)) = -0.01
        _Rim_Brightness("Rim Brightness", Range(0,5)) = 1
        _RimColor("Rim Color", Color) = (1,1,1,1)

        [Toggle(_EMISSION_MAP)] _UseEmission("Enable Emission", Float) = 0
        [NoScaleOffset] _Emissions_Mask("Emission Mask", 2D) = "black" {}
        _Emissions_Color("Emission Color", Color) = (1,1,1,1)

        [Toggle(_NORMALMAP)] _UseNormalMap("Enable Normal Map", Float) = 0
        [Normal][NoScaleOffset] _NormalMap("Normal Map", 2D) = "bump" {}
        _NormalStrength("Normal Strength", Range(0,2)) = 1

        [Header(Optional Outline)]
        [Toggle(_OUTLINE_ON)] _UseOutline("Enable Outline Pass", Float) = 0
        _OutlineWidth("Outline Width (screen-ish units)", Range(0,10)) = 1
        _OutlineColor("Outline Color", Color) = (0,0,0,1)
        _OutlineZOffset("Outline Depth Offset", Range(-1,1)) = 0

        [Header(Rendering)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
        [Toggle] _ReceiveShadows("Receive Main Light Shadows", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }
        LOD 150

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma shader_feature_local_fragment _ _CEL_SHADING
            #pragma shader_feature_local_fragment _ _RIM_LIGHTING
            #pragma shader_feature_local_fragment _ _EMISSION_MAP
            #pragma shader_feature_local_fragment _ _NORMALMAP
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);
            TEXTURE2D(_Emissions_Mask); SAMPLER(sampler_Emissions_Mask);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _Texture_Scale;
                float4 _Texture_Offset;
                float4 _Shading_Color;
                float4 _Emissions_Color;
                float4 _RimColor;
                float _Cel_Shader_Offset;
                float _Cel_Ramp_Smoothness;
                float _Alpha_Clip_Threshold;
                float _Ambient_Self_Lighting;
                float _Rim_Lighting;
                float _Rim_Brightness;
                float _NormalStrength;
                float _ReceiveShadows;
                float4 _OutlineColor;
                float _OutlineWidth;
                float _OutlineZOffset;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half4 tangentWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 4);
                half fogFactor : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs nrm = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.normalWS = nrm.normalWS;
                output.tangentWS = half4(nrm.tangentWS, input.tangentOS.w * GetOddNegativeScale());
                output.uv = input.uv * _Texture_Scale.xy + _Texture_Offset.xy;
                OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
                OUTPUT_SH(output.normalWS, output.vertexSH);
                output.fogFactor = ComputeFogFactor(pos.positionCS.z);
                return output;
            }

            half3 GetSurfaceNormal(Varyings input, half3 normalWS)
            {
                #if defined(_NORMALMAP)
                    half3 tangentNormal = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv), _NormalStrength);
                    half3 bitangentWS = cross(normalWS, input.tangentWS.xyz) * input.tangentWS.w;
                    half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangentWS, normalWS);
                    return NormalizeNormalPerPixel(TransformTangentToWorld(tangentNormal, tangentToWorld));
                #else
                    return NormalizeNormalPerPixel(normalWS);
                #endif
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.uv;
                half4 baseSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * _Color;
                #if defined(_ALPHATEST_ON)
                    clip(baseSample.a - _Alpha_Clip_Threshold);
                #endif

                half3 geometricNormal = NormalizeNormalPerPixel(input.normalWS);
                half3 normalWS = GetSurfaceNormal(input, geometricNormal);
                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                mainLight.shadowAttenuation = lerp(1.0h, mainLight.shadowAttenuation, saturate(_ReceiveShadows));

                half ndotl = saturate(dot(normalWS, mainLight.direction));
                half directAttenuation = ndotl * mainLight.distanceAttenuation * mainLight.shadowAttenuation;
                half3 bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, normalWS);
                half3 ambient = bakedGI * _Ambient_Self_Lighting;

                half3 lighting;
                #if defined(_CEL_SHADING)
                    half ramp;
                    half softness = max((half)_Cel_Ramp_Smoothness, 0.0001h);
                    ramp = smoothstep((half)_Cel_Shader_Offset - softness, (half)_Cel_Shader_Offset + softness, directAttenuation);
                    lighting = lerp(_Shading_Color.rgb, 1.0h.xxx, ramp) * mainLight.color;
                    lighting += ambient;
                #else
                    lighting = mainLight.color * directAttenuation + ambient;
                #endif

                half3 color = baseSample.rgb * lighting;

                #if defined(_RIM_LIGHTING)
                    half rim = 1.0h - saturate(dot(normalWS, viewDirWS));
                    rim = smoothstep(saturate(1.0h - (half)_Rim_Lighting), 1.0h, rim);
                    color += _RimColor.rgb * rim * (half)_Rim_Brightness;
                #endif

                #if defined(_EMISSION_MAP)
                    half emissionMask = SAMPLE_TEXTURE2D(_Emissions_Mask, sampler_Emissions_Mask, uv).r;
                    color += emissionMask * _Emissions_Color.rgb;
                #endif

                color = MixFog(color, input.fogFactor);
                return half4(color, baseSample.a);
            }
            ENDHLSL
        }


        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma shader_feature_local_fragment _ _ALPHATEST_ON
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _Texture_Scale;
                float4 _Texture_Offset;
                float4 _Shading_Color;
                float4 _Emissions_Color;
                float4 _RimColor;
                float _Cel_Shader_Offset;
                float _Cel_Ramp_Smoothness;
                float _Alpha_Clip_Threshold;
                float _Ambient_Self_Lighting;
                float _Rim_Lighting;
                float _Rim_Brightness;
                float _NormalStrength;
                float _ReceiveShadows;
                float4 _OutlineColor;
                float _OutlineWidth;
                float _OutlineZOffset;
            CBUFFER_END
            struct ShadowAttributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct ShadowVaryings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            float3 _LightDirection;
            ShadowVaryings ShadowVert(ShadowAttributes input)
            {
                ShadowVaryings output = (ShadowVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input); UNITY_TRANSFER_INSTANCE_ID(input, output);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                positionWS = ApplyShadowBias(positionWS, normalWS, _LightDirection);
                output.positionCS = TransformWorldToHClip(positionWS);
                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    output.positionCS.z = max(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                output.uv = input.uv * _Texture_Scale.xy + _Texture_Offset.xy;
                return output;
            }
            half4 ShadowFrag(ShadowVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                #if defined(_ALPHATEST_ON)
                    half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a * _Color.a;
                    clip(alpha - _Alpha_Clip_Threshold);
                #endif
                return 0;
            }
            ENDHLSL
        }

        // Optional inverted-hull outline. When disabled, its fragment shader clips;
        // for best draw-call savings, remove/strip this pass in a project-specific variant or use a separate outline material.
        Pass
        {
            Name "OptionalOutline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex OutlineVert
            #pragma fragment OutlineFrag
            #pragma shader_feature_local_vertex _ _OUTLINE_ON
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _Texture_Scale;
                float4 _Texture_Offset;
                float4 _Shading_Color;
                float4 _Emissions_Color;
                float4 _RimColor;
                float _Cel_Shader_Offset;
                float _Cel_Ramp_Smoothness;
                float _Alpha_Clip_Threshold;
                float _Ambient_Self_Lighting;
                float _Rim_Lighting;
                float _Rim_Brightness;
                float _NormalStrength;
                float _ReceiveShadows;
                float4 _OutlineColor;
                float _OutlineWidth;
                float _OutlineZOffset;
            CBUFFER_END

            struct OutlineAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct OutlineVaryings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            OutlineVaryings OutlineVert(OutlineAttributes input)
            {
                OutlineVaryings output = (OutlineVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                #if defined(_OUTLINE_ON)
                    VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                    float3 normalVS = TransformWorldToViewDir(normalWS, true);
                    float4 clipPos = pos.positionCS;
                    float2 projectedNormal = normalize(mul((float3x3)UNITY_MATRIX_P, normalVS).xy);
                    clipPos.xy += projectedNormal * (_OutlineWidth * 0.0015) * clipPos.w;
                    clipPos.z += _OutlineZOffset * 0.0001 * clipPos.w;
                    output.positionCS = clipPos;
                #else
                    output.positionCS = float4(0,0,0,0);
                #endif
                return output;
            }

            half4 OutlineFrag(OutlineVaryings input) : SV_Target
            {
                #if defined(_OUTLINE_ON)
                    return half4(_OutlineColor.rgb, _OutlineColor.a);
                #else
                    clip(-1);
                    return 0;
                #endif
            }
            ENDHLSL
        }

    }
    FallBack Off
}
