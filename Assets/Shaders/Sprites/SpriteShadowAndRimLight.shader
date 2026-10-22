Shader "Custom/SpriteShadowAndRimLight"
{
    Properties
    {
        [PerRendererData] _MainTex("Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
        _Cutoff("Shadow Alpha Cutoff", Range(0,1)) = 0.3
        _LightInfluence("Light Influence", Range(0,1)) = 1
        _MinBrightness("Min Brightness In Shadow", Range(0,1)) = 0.4
        _ReceiveOffset("Receive Shadow Offset", Range(0,1)) = 0.15
        _RimWidth("Rim Width (screen pixels)", Range(0.5,8)) = 3
        _RimBoost("Rim Intensity", Range(0,6)) = 2
        _RimBlur("Rim Blur (screen pixels)", Range(0,8)) = 2
        _AddLightStrength("Extra Light Strength", Range(0,3)) = 1
        _AddRimStrength("Extra Light Rim Strength", Range(0,3)) = 1
        [HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip("Flip", Vector) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        float4 _MainTex_TexelSize;
        float4 _SunRimColor;

        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            half4 _Color;
            half4 _RendererColor;
            float4 _Flip;
            half _Cutoff;
            half _LightInfluence;
            half _MinBrightness;
            half _ReceiveOffset;
            half _RimWidth;
            half _RimBoost;
            half _RimBlur;
            half _AddLightStrength;
            half _AddRimStrength;
        CBUFFER_END

        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM

            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                float3 positionWS : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 positionOS = float3(
                    IN.positionOS.xy * _Flip.xy,
                    IN.positionOS.z
                );

                VertexPositionInputs positionInputs =
                    GetVertexPositionInputs(positionOS);

                OUT.positionCS = positionInputs.positionCS;
                OUT.positionWS = positionInputs.positionWS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color * _Color * _RendererColor;

                return OUT;
            }

            half ComputeRim(float3 L, float3 dpdx, float3 dpdy, float2 duvdx, float2 duvdy, float2 uv)
            {
                float m00 = dot(dpdx, dpdx);
                float m01 = dot(dpdx, dpdy);
                float m11 = dot(dpdy, dpdy);
                float r0 = dot(dpdx, L);
                float r1 = dot(dpdy, L);
                float det = m00 * m11 - m01 * m01;

                float2 pixelDir = float2(0.0, 0.0);

                if (abs(det) > 1e-12)
                {
                    pixelDir = float2(
                        m11 * r0 - m01 * r1,
                        m00 * r1 - m01 * r0
                    ) / det;
                }

                float pixelDirLen = length(pixelDir);
                pixelDir /= max(pixelDirLen, 1e-6);

                float2 stepUV =
                    (duvdx * pixelDir.x + duvdy * pixelDir.y) *
                    max(_RimWidth, 0.5h);

                float2 texel = abs(_MainTex_TexelSize.xy);
                float stepLen = length(stepUV);
                float minLen = min(texel.x, texel.y);

                if (stepLen < minLen)
                {
                    stepUV *= minLen / max(stepLen, 1e-8);
                }

                half centerSum = 0.0h;
                half neighborSum = 0.0h;

                [unroll]
                for (int i = 0; i < 8; i++)
                {
                    float ang = i * 2.39996;
                    float rad = sqrt((i + 0.5) / 8.0) * _RimBlur;
                    float2 p = float2(cos(ang), sin(ang)) * rad;
                    float2 o = duvdx * p.x + duvdy * p.y;

                    centerSum += SAMPLE_TEXTURE2D_LOD(
                        _MainTex,
                        sampler_MainTex,
                        uv + o,
                        0
                    ).a;

                    neighborSum += SAMPLE_TEXTURE2D_LOD(
                        _MainTex,
                        sampler_MainTex,
                        uv + o + stepUV,
                        0
                    ).a;
                }

                half rim = saturate((centerSum - neighborSum) / 8.0h);

                return rim * step(0.0001, pixelDirLen);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(
                    _MainTex,
                    sampler_MainTex,
                    IN.uv
                );

                half4 color = tex * IN.color;

                clip(color.a - 0.001h);

                float3 dpdx = ddx(IN.positionWS);
                float3 dpdy = ddy(IN.positionWS);
                float2 duvdx = ddx(IN.uv);
                float2 duvdy = ddy(IN.uv);

                Light mainLight = GetMainLight();

                float3 samplePosition =
                    IN.positionWS +
                    mainLight.direction * _ReceiveOffset;

                float4 shadowCoord =
                    TransformWorldToShadowCoord(samplePosition);

                mainLight = GetMainLight(shadowCoord);

                half shadow = saturate(mainLight.shadowAttenuation);
                half3 lighting = lerp(_MinBrightness.xxx, 1.0h.xxx, shadow);

                half3 finalLighting = lerp(
                    1.0h.xxx,
                    lighting,
                    saturate(_LightInfluence)
                );

                half sunRim = ComputeRim(
                    mainLight.direction,
                    dpdx,
                    dpdy,
                    duvdx,
                    duvdy,
                    IN.uv
                );

                half3 rimAdd = _SunRimColor.rgb *
                    (sunRim * saturate(_SunRimColor.a) * _RimBoost * smoothstep(0.2, 0.8, shadow));

                half3 extraLight = 0.0h;
                half3 extraRim = 0.0h;

                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);

                uint rimUsed = 0u;

                #if defined(_ADDITIONAL_LIGHTS) || defined(_FORWARD_PLUS)
                uint pixelLightCount = GetAdditionalLightsCount();

                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light l = GetAdditionalLight(lightIndex, inputData.positionWS, half4(1, 1, 1, 1));
                    half3 c = l.color * (l.distanceAttenuation * l.shadowAttenuation);

                    extraLight += c;

                    if (rimUsed < 2u)
                    {
                        extraRim += c * ComputeRim(
                            l.direction,
                            dpdx,
                            dpdy,
                            duvdx,
                            duvdy,
                            IN.uv
                        );
                        rimUsed++;
                    }
                LIGHT_LOOP_END
                #endif

                half3 rgb = color.rgb * (finalLighting + extraLight * _AddLightStrength);
                rgb += rimAdd + extraRim * (_AddRimStrength * _RimBoost);

                return half4(rgb, color.a);
            }

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            Cull Off
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM

            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half alpha : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 positionOS = float3(
                    IN.positionOS.xy * _Flip.xy,
                    IN.positionOS.z
                );

                float3 positionWS =
                    TransformObjectToWorld(positionOS);

                float3 normalWS = TransformObjectToWorldNormal(
                    float3(0.0, 0.0, -1.0)
                );

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirection =
                        normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirection = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(
                    ApplyShadowBias(
                        positionWS,
                        normalWS,
                        lightDirection
                    )
                );

                #if UNITY_REVERSED_Z
                    positionCS.z = min(
                        positionCS.z,
                        UNITY_NEAR_CLIP_VALUE
                    );
                #else
                    positionCS.z = max(
                        positionCS.z,
                        UNITY_NEAR_CLIP_VALUE
                    );
                #endif

                OUT.positionCS = positionCS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.alpha = (
                    IN.color * _Color * _RendererColor
                ).a;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half alpha = SAMPLE_TEXTURE2D(
                    _MainTex,
                    sampler_MainTex,
                    IN.uv
                ).a * IN.alpha;

                clip(alpha - _Cutoff);

                return 0;
            }

            ENDHLSL
        }
    }

    Fallback Off
}