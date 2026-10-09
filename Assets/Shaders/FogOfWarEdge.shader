Shader "Custom/FogPlane"
{
    Properties
    {
        _FogColor  ("Fog Color",  Color)         = (0.75, 0.82, 0.85, 1)
        _Opacity   ("Opacity",    Range(0, 1))    = 0.85
        _TurbScale ("Turbulence", Range(0, 1))    = 0.3
        _Speed     ("Speed",      Range(0, 3))    = 1.0
        _Scale     ("Scale",      Range(1, 20))   = 5.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+10" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _FogColor;
            float  _Opacity, _TurbScale, _Speed, _Scale;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f     { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            float hash(float2 p) {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }
            float noise(float2 p) {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(hash(i),               hash(i + float2(1,0)), f.x),
                    lerp(hash(i + float2(0,1)), hash(i + float2(1,1)), f.x),
                    f.y);
            }
            float fbm(float2 p) {
                float v = 0.0, a = 0.5;
                for (int i = 0; i < 6; i++) { v += a * noise(p); p *= 2.1; a *= 0.5; }
                return v;
            }

            fixed4 frag(v2f i) : SV_Target {
                float t = _Time.y * _Speed;
                float2 uv = i.uv * _Scale;

                float f1 = fbm(uv + float2(t * 0.08, t * 0.05));
                float f2 = fbm(uv * 1.5 + float2(3.7, 1.9) - t * 0.06);
                float turb = lerp(f1, f2, 0.5);

                float alpha = _Opacity * (1.0 - _TurbScale + turb * _TurbScale);

                return float4(_FogColor.rgb, saturate(alpha));
            }
            ENDCG
        }
    }
}