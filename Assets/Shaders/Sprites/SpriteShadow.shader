Shader "Custom/SpriteShadow"
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

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(
                    _MainTex,
                    sampler_MainTex,
                    IN.uv
                );

                half4 color = tex * IN.color;

                clip(color.a - 0.001h);

                Light mainLight = GetMainLight();

                float3 samplePosition =
                    IN.positionWS +
                    mainLight.direction * _ReceiveOffset;

                float4 shadowCoord =
                    TransformWorldToShadowCoord(samplePosition);

                mainLight = GetMainLight(shadowCoord);

                half3 directLight =
                    mainLight.color *
                    mainLight.shadowAttenuation *
                    mainLight.distanceAttenuation;

                half3 ambientLight =
                    SampleSH(float3(0.0, 1.0, 0.0));

                half3 lighting = max(
                    directLight + ambientLight,
                    _MinBrightness.xxx
                );

                lighting = min(lighting, 1.0h.xxx);

                half3 finalLighting = lerp(
                    1.0h.xxx,
                    lighting,
                    saturate(_LightInfluence)
                );

                half3 rgb = color.rgb * finalLighting;

                float3 lightDirectionOS =
                    TransformWorldToObjectDir(
                        mainLight.direction,
                        false
                    );

                float2 rimDirection =
                    lightDirectionOS.xy * _Flip.xy;

                float rimDirectionLength = length(rimDirection);

                rimDirection /= max(rimDirectionLength, 0.0001);

                float2 uvPerPixel = float2(
                    length(float2(ddx(IN.uv.x), ddy(IN.uv.x))),
                    length(float2(ddx(IN.uv.y), ddy(IN.uv.y)))
                );

                float2 texelSize = abs(_MainTex_TexelSize.xy);

                float2 stepUV = max(
                    uvPerPixel * max(_RimWidth, 0.5h),
                    texelSize
                );

                float2 neighborUV =
                    IN.uv + rimDirection * stepUV;

                half neighborAlpha = SAMPLE_TEXTURE2D(
                    _MainTex,
                    sampler_MainTex,
                    neighborUV
                ).a;

                half rim = saturate(tex.a - neighborAlpha);

                rim *= step(
                    0.0001,
                    rimDirectionLength
                );

                rgb += _SunRimColor.rgb *
                    (rim * saturate(_SunRimColor.a) * _RimBoost);

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
