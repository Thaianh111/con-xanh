// Cồn Xanh – stylized water: depth tint, shoreline foam, waves, fresnel, sun glint. VTA Games · Võ Thái Anh
Shader "ConXanh/Water"
{
    Properties
    {
        _Shallow("Shallow", Color) = (0.36, 0.82, 0.86, 0.55)
        _Deep("Deep", Color) = (0.07, 0.42, 0.72, 0.92)
        _Foam("Foam", Color) = (1, 1, 1, 1)
        _DepthRange("Depth Range", Float) = 1.4
        _FoamWidth("Foam Width", Float) = 0.18
        _WaveHeight("Wave Height", Float) = 0.025
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Water"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Shallow; half4 _Deep; half4 _Foam;
                float _DepthRange; float _FoamWidth; float _WaveHeight;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float4 screenPos : TEXCOORD1; half fog : TEXCOORD2; };
            Varyings vert(Attributes i)
            {
                Varyings o;
                float3 w = TransformObjectToWorld(i.positionOS.xyz);
                float t = _Time.y;
                w.y += (sin(w.x * 0.7 + t * 1.1) + cos(w.z * 0.9 + t * 0.8)) * _WaveHeight;
                o.positionWS = w;
                o.positionCS = TransformWorldToHClip(w);
                o.screenPos = ComputeScreenPos(o.positionCS);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }
            float3 WaveNormal(float2 p, float t)
            {
                float gx = cos(p.x * 1.3 + t) * 0.5 + cos(p.x * 2.9 - p.y * 1.7 + t * 1.3) * 0.25 + cos(p.x * 6.1 + p.y * 2.3 - t * 2.0) * 0.12;
                float gz = sin(p.y * 1.1 + t * 0.9) * 0.5 + sin(p.y * 3.1 + p.x * 1.9 - t * 1.1) * 0.25 + sin(p.y * 5.7 - p.x * 2.9 + t * 1.7) * 0.12;
                return normalize(float3(-gx * 0.18, 1, -gz * 0.18));
            }
            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.screenPos.xy / i.screenPos.w;
                float sceneZ = LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
                float surfZ = LinearEyeDepth(i.positionCS.z, _ZBufferParams);
                float d = max(0, sceneZ - surfZ);
                half4 col = lerp(_Shallow, _Deep, saturate(d / _DepthRange));
                float3 n = WaveNormal(i.positionWS.xz, _Time.y);
                float3 v = normalize(GetWorldSpaceViewDir(i.positionWS));
                Light l = GetMainLight();
                float3 h = normalize(l.direction + v);
                half spec = pow(saturate(dot(n, h)), 220) * 2.0;
                half fres = pow(1 - saturate(dot(n, v)), 4);
                half3 sky = SampleSH(float3(0, 1, 0));
                col.rgb = col.rgb * (l.color * 0.55 + sky * 0.65) + fres * sky * 0.6 + spec * l.color;
                half foam = 1 - saturate(d / _FoamWidth);
                foam *= 0.65 + 0.35 * sin(_Time.y * 2.2 + i.positionWS.x * 3.0 + i.positionWS.z * 2.0);
                col.rgb = lerp(col.rgb, _Foam.rgb, saturate(foam));
                col.a = saturate(col.a + foam + fres * 0.25);
                col.rgb = MixFog(col.rgb, i.fog);
                return col;
            }
            ENDHLSL
        }
    }
}
