Shader "Custom/BlobShadow"
{
    Properties
    {
        _MainTex ("Shadow Texture (optional, white circle)", 2D) = "white" {}
        _Color ("Shadow Color", Color) = (0, 0, 0, 0.5)
        _Softness ("Edge Softness", Range(0.01, 1)) = 0.4
        _Radius ("Radius", Range(0.01, 1)) = 0.5
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _Softness;
            float _Radius;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 centered = i.uv - 0.5;
                float dist = length(centered) * 2.0; 

                float alpha = 1.0 - smoothstep(_Radius - _Softness, _Radius, dist);

                fixed4 texSample = tex2D(_MainTex, i.uv);

                fixed4 col = _Color;
                col.a *= alpha * texSample.a;

                return col;
            }
            ENDCG
        }
    }
}
