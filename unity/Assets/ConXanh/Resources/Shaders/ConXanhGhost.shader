// Cồn Xanh – translucent placement preview. VTA Games · Võ Thái Anh
Shader "ConXanh/Ghost"
{
    Properties { _Color("Color", Color) = (0.4, 1, 0.55, 0.45) }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+10" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial) half4 _Color; CBUFFER_END
            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct V { float4 positionCS : SV_POSITION; float3 n : TEXCOORD0; float3 w : TEXCOORD1; };
            V vert(A i) { V o; o.w = TransformObjectToWorld(i.positionOS.xyz); o.positionCS = TransformWorldToHClip(o.w); o.n = TransformObjectToWorldNormal(i.normalOS); return o; }
            half4 frag(V i) : SV_Target
            {
                float3 v = normalize(GetWorldSpaceViewDir(i.w));
                half rim = pow(1 - saturate(abs(dot(normalize(i.n), v))), 2);
                half pulse = 0.85 + 0.15 * sin(_Time.y * 4);
                return half4(_Color.rgb * (0.7 + rim * 0.8), saturate(_Color.a * pulse + rim * 0.35));
            }
            ENDHLSL
        }
    }
}
