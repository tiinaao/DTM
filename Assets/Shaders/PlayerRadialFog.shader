Shader "Hidden/PlayerRadialFog"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "PlayerRadialFog"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D_X(_BlitTexture);
            SAMPLER(sampler_BlitTexture);

            float4 _FogPlayerPos; 
            float  _FogInnerRadius;
            float  _FogOuterRadius;
            float4 _FogColor;
            float  _FogMaxDensity;
            float  _FogHeightFalloff;
            float4 _FogNoiseParams; 

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                o.uv = GetFullScreenTriangleTexCoord(input.vertexID);
                return o;
            }

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float a = Hash(i);
                float b = Hash(i + float2(1, 0));
                float c = Hash(i + float2(0, 1));
                float d = Hash(i + float2(1, 1));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float4 Frag(Varyings i) : SV_Target
            {
                float4 sceneColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, i.uv);

                float depth = SampleSceneDepth(i.uv);
                float3 worldPos = ComputeWorldSpacePosition(i.uv, depth, UNITY_MATRIX_I_VP);

                float3 toPixel = worldPos - _FogPlayerPos.xyz;
                float dist = length(toPixel.xz); 

                float n = ValueNoise(toPixel.xz * _FogNoiseParams.x + _FogNoiseParams.z * _FogNoiseParams.y);
                float radiusJitter = (n - 0.5) * (_FogOuterRadius - _FogInnerRadius) * 0.4;

                float innerR = _FogInnerRadius + radiusJitter;
                float outerR = _FogOuterRadius + radiusJitter;

                float density = smoothstep(innerR, outerR, dist) * _FogMaxDensity;

                float heightAttenuation = saturate(1.0 - max(toPixel.y, 0.0) * _FogHeightFalloff);
                density *= lerp(1.0, heightAttenuation, 0.5);

                float3 finalColor = lerp(sceneColor.rgb, _FogColor.rgb, density);
                return float4(finalColor, sceneColor.a);
            }
            ENDHLSL
        }
    }
}
