// Zamućena slika za UI (pitanje „mutna slika”): uzorkuje manje mipmape teksture i prosječi ih.
// _Blur 0 = oštro, 6+ = jako mutno. Radi sa maskama u UI-ju (stencil) kao i običan UI šejder.
Shader "TrkaZnanja/UIBlur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Tekstura", 2D) = "white" {}
        _Color ("Boja", Color) = (1, 1, 1, 1)
        _Blur ("Zamućenje", Range(0, 8)) = 0
        _Desat ("Bez boje", Range(0, 1)) = 0

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" "CanUseSpriteAtlas" = "True" }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t { float4 vertex : POSITION; float4 color : COLOR; float2 texcoord : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 texcoord : TEXCOORD0; };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            float _Blur;
            float _Desat;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float lod = _Blur;
                float2 r = _MainTex_TexelSize.xy * exp2(lod) * 0.75;
                fixed4 c = tex2Dlod(_MainTex, float4(i.texcoord, 0, lod)) * 0.28;
                c += tex2Dlod(_MainTex, float4(i.texcoord + float2(r.x, 0), 0, lod)) * 0.12;
                c += tex2Dlod(_MainTex, float4(i.texcoord - float2(r.x, 0), 0, lod)) * 0.12;
                c += tex2Dlod(_MainTex, float4(i.texcoord + float2(0, r.y), 0, lod)) * 0.12;
                c += tex2Dlod(_MainTex, float4(i.texcoord - float2(0, r.y), 0, lod)) * 0.12;
                c += tex2Dlod(_MainTex, float4(i.texcoord + r, 0, lod)) * 0.06;
                c += tex2Dlod(_MainTex, float4(i.texcoord - r, 0, lod)) * 0.06;
                c += tex2Dlod(_MainTex, float4(i.texcoord + float2(r.x, -r.y), 0, lod)) * 0.06;
                c += tex2Dlod(_MainTex, float4(i.texcoord + float2(-r.x, r.y), 0, lod)) * 0.06;
                fixed g = dot(c.rgb, fixed3(0.3, 0.59, 0.11));
                c.rgb = lerp(c.rgb, g.xxx, _Desat);
                return c * i.color;
            }
            ENDCG
        }
    }
}
