// Preview of the RealSenseD405 depth (R channel of its depth+id texture, in meters)
// for the Game view displays: near = bright, far = dark, outside the range = black.
// Only for viewing; the published depth image is not affected.
Shader "Hidden/PM/DepthVisualize"
{
    Properties
    {
        _MainTex ("Depth+Id", 2D) = "black" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _DepthMin;
            float _DepthMax;

            float4 frag(v2f_img i) : SV_Target
            {
                float d = tex2D(_MainTex, i.uv).r;
                if (d < _DepthMin || d > _DepthMax)
                    return float4(0, 0, 0, 1);
                float t = 1.0 - saturate((d - _DepthMin) / max(_DepthMax - _DepthMin, 1e-6));
                float g = 0.15 + 0.85 * t;
                return float4(g, g, g, 1);
            }
            ENDCG
        }
    }
}
