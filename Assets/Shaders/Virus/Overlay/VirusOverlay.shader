Shader "UI/VirusOverlay"
{
Properties
{
    _MainTex       ("Main Texture",    2D)           = "white" {}
    _Intensity     ("Intensity",       Range(0,1))   = 0
    _VignColor0    ("Vignette Red A",  Color)        = (0.55, 0.0, 0.02, 1)
    _VignColor1    ("Vignette Red B",  Color)        = (0.20, 0.0, 0.00, 1)
    _VignColorDark ("Vignette Black",  Color)        = (0.02, 0.0, 0.00, 1)
    _GrainStrength ("Grain Strength",  Range(0,1))   = 0.22
    _GrainScale    ("Grain Scale",     Range(1,12))  = 5.0
    _GrainSpeed    ("Grain Speed",     Range(0,120)) = 60
    _CenterX       ("Center X",        Range(0,1))   = 0.5
    _CenterY       ("Center Y",        Range(0,1))   = 0.5
    _RadiusX       ("Radius X",        Range(0.1,2)) = 0.7
    _RadiusY       ("Radius Y",        Range(0.1,2)) = 0.5
}

SubShader
{
    Tags { "Queue"="Overlay" "RenderType"="Transparent" }
    Blend SrcAlpha OneMinusSrcAlpha
    ZWrite Off
    Cull Off

    Pass
    {
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #include "UnityCG.cginc"

        struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
        struct v2f     { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float2 screenUV : TEXCOORD1; };

        sampler2D _MainTex;
        float  _Intensity;
        float4 _VignColor0, _VignColor1, _VignColorDark;
        float  _GrainStrength, _GrainScale, _GrainSpeed;
        float  _CenterX, _CenterY, _RadiusX, _RadiusY;

        v2f vert(appdata v)
        {
            v2f o;
            o.pos = UnityObjectToClipPos(v.vertex);
            o.uv = v.uv;
            o.screenUV = o.pos.xy / o.pos.w * 0.5 + 0.5;
            
            return o;
        }

        float vnoise(float2 p)
        {
            float2 i=floor(p), f=frac(p); f=f*f*(3.-2.*f);
            return lerp(lerp(frac(sin(dot(i, float2(127.1,311.7))) * 43758.5),
                             frac(sin(dot(i + float2(1,0), float2(127.1,311.7))) * 43758.5),f.x),
                             lerp(frac(sin(dot(i + float2(0,1), float2(127.1,311.7))) * 43758.5),
                             frac(sin(dot(i + float2(1,1),float2(127.1,311.7))) * 43758.5),f.x),f.y);
        }

        float fbm2(float2 p)
        {
            float v=.50*vnoise(p); 
            p=p*2.1+float2(1.7,.9);
            v+=.25*vnoise(p);      
            p=p*2.1+float2(3.4,1.8);

            return v+.125*vnoise(p);
        }

        float tvNoise(float2 u){ return frac(sin(dot(u,float2(12.9898,78.233)))*43758.5453); }

        float organicEdge(float2 uv, float t)
        {
            float2 c=(uv-float2(_CenterX,_CenterY))/float2(_RadiusX,_RadiusY);
            float2 w=float2(fbm2(uv*2.3+float2(t*.07,t*.05)), fbm2(uv*1.6+float2(-t*.04,t*.06)))*2.-1.;
            float2 q=abs(c+w*.08)-.5;
            return pow(smoothstep(0.,.75,length(max(q,0.))+min(max(q.x,q.y),0.)),3.2);
        }

        fixed4 frag(v2f i) : SV_Target
        {
            if(_Intensity<=0.001) return fixed4(0,0,0,0);

            float t = _Time.y;
            float2 uv = i.screenUV;

            float d=(fbm2(uv*8+t*2.5)*2-1)*.03+(fbm2(uv*18-t*4.)*2-1)*.015;
            float2 uvW=uv+float2(d,d*.5);
            float edge=organicEdge(uvW,t);

            float gn=fbm2(uvW*_GrainScale+float2(t*_GrainSpeed*.73,t*_GrainSpeed*.51))*.7 + vnoise(uvW*_GrainScale*1.8+float2(-t*_GrainSpeed*.39,t*_GrainSpeed*.66))*.3;
            float g=(gn-.5)*_GrainStrength*_Intensity*2.4;

            float cn=fbm2(uv*1.7+float2(t*.03,t*.02));
            float3 vc=lerp(lerp(_VignColor0.rgb,_VignColor1.rgb,cn),_VignColorDark.rgb,pow(edge,1.2));
            float alpha=min(saturate(edge*edge*_Intensity*1.45+abs(g)*1.2*edge),.97);

            float2 guv=uv*_ScreenParams.xy*.75;
            float  tj=floor(t*60.);
            float  grain=tvNoise(guv+tj)+tvNoise(guv*1.37-tj)-1.;
            float  blood=pow(saturate(fbm2(uv*3.+float2(t*.08,-t*.05))),4.);

            float3 col=vc*edge*1.35 + float3(abs(g)*2.5,abs(g)*.04,abs(g)*.04) + float3(blood*.18,blood*.01,blood*.01);
            col+=float3(grain*1.02,grain*.98,grain*.98)*.05;
            float fg=(tvNoise(uv*_ScreenParams.xy+tj)+tvNoise(uv*_ScreenParams.xy*1.37-tj)-1.)*.15;
            col+=float3(fg*1.02,fg*.99,fg*.99);

            alpha=saturate(alpha+abs(fg));

            return fixed4(col,alpha);
        }
        ENDCG
    }
}
}