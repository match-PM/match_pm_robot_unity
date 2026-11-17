// Used by RealSenseD405 (via a CommandBuffer, never assigned to a material in the scene).
// One pass writes two render targets:
//   SV_Target0: flat, unlit segmentation color of the object (_SegColor)
//   SV_Target1: R = linear depth along the optical axis [m], G = segmentation id
// _SegColor/_SegId are set as globals per draw and must NOT be declared in Properties,
// otherwise the material value would override the per-draw global.
Shader "Hidden/PM/SegmentationDepth"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            Cull Off
            ZWrite On
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4x4 _SegView;
            float4x4 _SegViewProj;
            float4 _SegColor;
            float _SegId;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float depth : TEXCOORD0;
            };

            struct FragOut
            {
                float4 seg : SV_Target0;
                float4 data : SV_Target1;
            };

            v2f vert(float4 vertex : POSITION)
            {
                v2f o;
                float4 world = mul(unity_ObjectToWorld, vertex);
                o.pos = mul(_SegViewProj, world);
                // Unity view space looks along -Z.
                o.depth = -mul(_SegView, world).z;
                return o;
            }

            FragOut frag(v2f i)
            {
                FragOut o;
                o.seg = _SegColor;
                o.data = float4(i.depth, _SegId, 0, 0);
                return o;
            }
            ENDCG
        }
    }
}
