Shader "Custom/TelecentricDepthOfField"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _FocusZ ("Focus Distance (camera space)", Float) = 1.0
        _MaxDepthDiff ("Max Depth Difference", Float) = 0.02
        _MaxBlurRadius ("Max Blur Radius (pixels)", Float) = 2.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Transparent+100" }
        Cull Off
        ZWrite Off
        ZTest Always
        Lighting Off
        Fog { Mode Off }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            sampler2D _CameraDepthTexture;

            float _FocusZ;
            float _MaxDepthDiff;
            float _MaxBlurRadius;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float fragDepth(float2 uv)
            {
                #if UNITY_REVERSED_Z
                    float raw = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
                    return LinearEyeDepth(raw);
                #else
                    float raw = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
                    return LinearEyeDepth(raw);
                #endif
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Tiefe im Blick Raum
                float depth = fragDepth(i.uv);

                // Differenz zur Fokus Ebene
                float dz = abs(depth - _FocusZ);

                // Normierte Unschärfe von 0 bis 1
                float blur01 = saturate(dz / max(_MaxDepthDiff, 1e-5));

                // Radius in Pixeln
                float radius = blur01 * _MaxBlurRadius;

                // Wenn Radius fast null, direkt Originalfarbe zurückgeben
                if (radius < 0.001)
                {
                    return tex2D(_MainTex, i.uv);
                }

                // Texel Schritte
                float2 texel = _MainTex_TexelSize.xy * radius;

                // Mehrere Samples in einem Kreis
                fixed4 col = 0;
                float weightSum = 0;

                const int sampleCount = 8;

                // einfache feste Musterpunkte
                float2 offsets[sampleCount];
                offsets[0] = float2( 1,  0);
                offsets[1] = float2(-1,  0);
                offsets[2] = float2( 0,  1);
                offsets[3] = float2( 0, -1);
                offsets[4] = float2( 0.7071,  0.7071);
                offsets[5] = float2(-0.7071,  0.7071);
                offsets[6] = float2( 0.7071, -0.7071);
                offsets[7] = float2(-0.7071, -0.7071);

                // Kernpunkt stärker gewichten
                fixed4 center = tex2D(_MainTex, i.uv);
                col += center * 2.0;
                weightSum += 2.0;

                for (int k = 0; k < sampleCount; k++)
                {
                    float2 duv = offsets[k] * texel;
                    float2 suv = i.uv + duv;
                    fixed4 s = tex2D(_MainTex, suv);
                    col += s;
                    weightSum += 1.0;
                }

                col /= weightSum;

                // Unschärfe abhängig von blur01 mit Original mischen
                return lerp(center, col, blur01);
            }
            ENDCG
        }
    }

    FallBack Off
}
