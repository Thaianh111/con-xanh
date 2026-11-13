// Cồn Xanh – soft rainbow arc. uv.x runs across the bands, uv.y along the arc. VTA Games · Võ Thái Anh
Shader "ConXanh/Rainbow"
{
    Properties { _Alpha ("Alpha", Range(0,1)) = 0.7 }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha One
        ZWrite Off Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial) float _Alpha; CBUFFER_END
            struct A { float4 pos : POSITION; float2 uv : TEXCOORD0; };
            struct V { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float fog : TEXCOORD1; };
            V vert (A i) { V o; o.pos = TransformObjectToHClip(i.pos.xyz); o.uv = i.uv; o.fog = ComputeFogFactor(o.pos.z); return o; }
            float3 Hue(float h) { float3 k = saturate(abs(frac(h + float3(0, 2.0/3.0, 1.0/3.0)) * 6 - 3) - 1); return k; }
            half4 frag (V i) : SV_Target
            {
                float x = i.uv.x;                       // 0 inner (violet) .. 1 outer (red)
                float3 c = Hue((1 - x) * 0.78);
                float edge = smoothstep(0, 0.18, x) * smoothstep(1, 0.82, x);
                float ends = smoothstep(0, 0.12, i.uv.y) * smoothstep(1, 0.88, i.uv.y);
                return half4(c, edge * ends * _Alpha);
            }
            ENDHLSL
        }
    }
}
