Shader "Hidden/ScreenBlur"
{
    Properties
    {
        _BlurAmount ("Blur Amount", Range(0,1)) = 0
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _BlurAmount;

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;

                if (_BlurAmount <= 0.001)
                    return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                float r = _BlurAmount * 0.012;

                half4 col = 0;
                col += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(-r, -r)) * 0.0625;
                col += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2( 0, -r)) * 0.125;
                col += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2( r, -r)) * 0.0625;
                col += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(-r,  0)) * 0.125;
                col += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv              ) * 0.25;
                col += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2( r,  0)) * 0.125;
                col += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(-r,  r)) * 0.0625;
                col += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2( 0,  r)) * 0.125;
                col += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2( r,  r)) * 0.0625;

                return col;
            }
            ENDHLSL
        }
    }
 }