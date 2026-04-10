Shader "Custom/TelecentricDepthOfField"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _MaxDepthDiff ("Max Depth Difference", Float) = 0.01
        _MaxBlurRadius ("Max Blur Radius (pixels)", Float) = 0.0
        _BlurAmount ("Blur Amount", Float) = 0.0
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

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 center = tex2D(_MainTex, i.uv);
                float blurPixels = _MaxBlurRadius;

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

                // Complete the outer ring symmetrically.
                float2 extraOffsets[4];
                extraOffsets[0] = float2(-0.6187, -0.6187);
                extraOffsets[1] = float2( 0.8750,  0.0000);
                extraOffsets[2] = float2(-0.8750,  0.0000);
                extraOffsets[3] = float2( 0.0000,  0.8750);

                for (int j = 0; j < 4; j++)
                {
                    float2 uv = saturate(i.uv + extraOffsets[j] * texel);
                    float weight = 0.75;
                    col += tex2D(_MainTex, uv) * weight;
                    weightSum += weight;
                }

                col /= weightSum;
                return lerp(center, col, saturate(_BlurAmount));
            }
            ENDCG
        }
    }

    FallBack Off
}
