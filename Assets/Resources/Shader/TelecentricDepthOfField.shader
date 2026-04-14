Shader "Custom/TelecentricDepthOfField"
{
    // _MaxBlurRadius controls sample distance.
    // _BlurAmount controls linear blend amount (0..1).
    // Both are set from TelecentricDepthOfField.cs.
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _MaxDepthDiff ("Max Depth Difference", Float) = 0.01
<<<<<<< .merge_file_owRFtH
        _MaxBlurRadius ("Max Blur Radius (pixels)", Float) = 0.0
        _BlurAmount ("Blur Amount", Float) = 0.0
=======
        _MaxBlurRadius ("Max Blur Radius (pixels)", Float) = 5.0
        _BlurAmount ("Blur Amount (0-1)", Float) = 0.0
>>>>>>> .merge_file_AU8clp
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
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

<<<<<<< .merge_file_owRFtH
            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 center = tex2D(_MainTex, i.uv);
                float blurPixels = _MaxBlurRadius;
=======
            fixed4 frag (v2f i) : SV_Target
            {
                // Radius in Pixeln, direkt aus dem Skript gesteuert
                float radius = _MaxBlurRadius;
>>>>>>> .merge_file_AU8clp

                if (blurPixels < 0.001 || _BlurAmount <= 0.001)
                {
                    return center;
                }

                // Use a circular multi-sample kernel so the image gets genuinely
                // softened like defocus blur instead of being turned into blocks.
                float2 texel = _MainTex_TexelSize.xy * max(blurPixels, 0.5);

                const int sampleCount = 16;
                float2 offsets[sampleCount];
                offsets[0]  = float2( 0.0000,  0.0000);
                offsets[1]  = float2( 0.5278,  0.0000);
                offsets[2]  = float2(-0.5278,  0.0000);
                offsets[3]  = float2( 0.0000,  0.5278);
                offsets[4]  = float2( 0.0000, -0.5278);
                offsets[5]  = float2( 0.3730,  0.3730);
                offsets[6]  = float2(-0.3730,  0.3730);
                offsets[7]  = float2( 0.3730, -0.3730);
                offsets[8]  = float2(-0.3730, -0.3730);
                offsets[9]  = float2( 0.8750,  0.0000);
                offsets[10] = float2(-0.8750,  0.0000);
                offsets[11] = float2( 0.0000,  0.8750);
                offsets[12] = float2( 0.0000, -0.8750);
                offsets[13] = float2( 0.6187,  0.6187);
                offsets[14] = float2(-0.6187,  0.6187);
                offsets[15] = float2( 0.6187, -0.6187);

<<<<<<< .merge_file_owRFtH
=======
                // Mehrere Samples in zwei Ringen für stärkeren Effekt
>>>>>>> .merge_file_AU8clp
                fixed4 col = 0;
                float weightSum = 0;

                for (int k = 0; k < sampleCount; k++)
                {
                    float2 uv = saturate(i.uv + offsets[k] * texel);
                    float radius01 = saturate(length(offsets[k]));
                    float weight = lerp(1.5, 0.75, radius01);
                    col += tex2D(_MainTex, uv) * weight;
                    weightSum += weight;
                }

<<<<<<< .merge_file_owRFtH
                // Complete the outer ring symmetrically.
                float2 extraOffsets[4];
                extraOffsets[0] = float2(-0.6187, -0.6187);
                extraOffsets[1] = float2( 0.8750,  0.0000);
                extraOffsets[2] = float2(-0.8750,  0.0000);
                extraOffsets[3] = float2( 0.0000,  0.8750);
=======
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
>>>>>>> .merge_file_AU8clp

                for (int j = 0; j < 4; j++)
                {
<<<<<<< .merge_file_owRFtH
                    float2 uv = saturate(i.uv + extraOffsets[j] * texel);
                    float weight = 0.75;
                    col += tex2D(_MainTex, uv) * weight;
                    weightSum += weight;
                }

                col /= weightSum;
                return lerp(center, col, saturate(_BlurAmount));
=======
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
>>>>>>> .merge_file_AU8clp
            }
            ENDCG
        }
    }

    FallBack Off
}
