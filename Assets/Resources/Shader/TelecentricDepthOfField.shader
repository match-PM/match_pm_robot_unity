Shader "Custom/TelecentricDepthOfField"
{
    // _MaxBlurRadius controls sample distance.
    // _BlurAmount controls linear blend amount (0..1).
    // Both are set from TelecentricDepthOfField.cs.
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _MaxDepthDiff ("Max Depth Difference", Float) = 0.01
        _MaxBlurRadius ("Max Blur Radius (pixels)", Float) = 5.0
        _BlurAmount ("Blur Amount (0-1)", Float) = 0.0
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
            float _MaxDepthDiff;
            float _MaxBlurRadius;
            float _BlurAmount;

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

            fixed4 frag (v2f i) : SV_Target
            {
                // Radius in Pixeln, direkt aus dem Skript gesteuert
                float radius = _MaxBlurRadius;

                // Wenn Radius fast null, direkt Originalfarbe zurückgeben
                if (radius < 0.001)
                {
                    return tex2D(_MainTex, i.uv);
                }

                // Texel Schritte
                float2 texel = _MainTex_TexelSize.xy * radius;

                // Mehrere Samples in zwei Ringen für stärkeren Effekt
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

                // Kernpunkt schwächer gewichten für deutlichere Unschärfe
                fixed4 center = tex2D(_MainTex, i.uv);
                col += center * 0.5;
                weightSum += 0.5;

                for (int k = 0; k < sampleCount; k++)
                {
                    float2 duv1 = offsets[k] * texel;
                    float2 suv1 = i.uv + duv1;
                    fixed4 s1 = tex2D(_MainTex, suv1);
                    col += s1;
                    weightSum += 1.0;

                    float2 duv2 = offsets[k] * texel * 2.0;
                    float2 suv2 = i.uv + duv2;
                    fixed4 s2 = tex2D(_MainTex, suv2);
                    col += s2 * 0.8;
                    weightSum += 0.8;
                }

                col /= weightSum;

                // Lineare Mischstärke aus dem Skript (distanzabhängig)
                float blurMix = saturate(_BlurAmount);
                return lerp(center, col, blurMix);
            }
            ENDCG
        }
    }

    FallBack Off
}
