// More i jezera: talasi u šejderu (ništa se ne računa na procesoru), odsjaj neba, sunčev odbljesak i pjena uz obalu.
// _ShoreTex: crveni kanal = blizina obale (1 uz obalu, 0 daleko), mapira se preko _ShoreRect (x0, z0, širina, dužina).
Shader "TrkaZnanja/Water"
{
    Properties
    {
        _DeepColor ("Duboko", Color) = (0.05, 0.36, 0.62, 1)
        _ShallowColor ("Plitko", Color) = (0.22, 0.72, 0.82, 1)
        _SkyColor ("Odsjaj neba", Color) = (0.72, 0.88, 1, 1)
        _FoamColor ("Pjena", Color) = (1, 1, 1, 1)
        _WaveHeight ("Visina talasa", Float) = 0.05
        _WaveScale ("Gustina talasa", Float) = 0.45
        _Alpha ("Prozirnost", Range(0, 1)) = 0.88
        _ShoreTex ("Obala", 2D) = "black" {}
        _ShoreRect ("Pravougaonik obale", Vector) = (0, 0, 1, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-10" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Name "Water"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_ShoreTex);
            SAMPLER(sampler_ShoreTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _DeepColor, _ShallowColor, _SkyColor, _FoamColor;
                float _WaveHeight, _WaveScale;
                half _Alpha;
                float4 _ShoreTex_ST;
                float4 _ShoreRect;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                half fogFactor : TEXCOORD2;
            };

            // zbir nekoliko sinusnih talasa; vraća visinu i nagib (za normalu)
            float3 Waves(float2 p, float t)
            {
                float2 d1 = normalize(float2(1, 0.3)), d2 = normalize(float2(-0.4, 1)), d3 = normalize(float2(0.7, -0.7));
                float s = _WaveScale;
                float a1 = dot(p, d1) * 1.0 * s + t * 1.1, a2 = dot(p, d2) * 1.7 * s + t * 1.4, a3 = dot(p, d3) * 2.9 * s + t * 2.1;
                float h = sin(a1) + 0.6 * sin(a2) + 0.3 * sin(a3);
                float2 grad = d1 * cos(a1) * s + d2 * 0.6 * 1.7 * s * cos(a2) + d3 * 0.3 * 2.9 * s * cos(a3);
                return float3(h, grad) * _WaveHeight;
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 ws = TransformObjectToWorld(v.positionOS.xyz);
                float3 w = Waves(ws.xz, _Time.y);
                ws.y += w.x;
                o.positionWS = ws;
                o.normalWS = normalize(float3(-w.y, 1, -w.z));
                o.positionCS = TransformWorldToHClip(ws);
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 n = normalize(i.normalWS);
                float3 v = normalize(GetWorldSpaceViewDir(i.positionWS));
                Light main = GetMainLight();

                float2 suv = (i.positionWS.xz - _ShoreRect.xy) / max(_ShoreRect.zw, 0.001);
                half shore = SAMPLE_TEXTURE2D(_ShoreTex, sampler_ShoreTex, suv).r;

                half fres = pow(1 - saturate(dot(n, v)), 4);
                half3 col = lerp(_DeepColor.rgb, _ShallowColor.rgb, saturate(shore * 1.3));
                col = lerp(col, _SkyColor.rgb, fres * 0.65);
                col *= lerp(0.55, 1.0, saturate(dot(n, main.direction) * 0.5 + 0.5)) * main.color;

                float3 h = normalize(main.direction + v);
                half spec = pow(saturate(dot(n, h)), 180) * 2.2;
                col += spec * main.color;

                // pjena: pruge koje putuju ka obali
                half band = sin(shore * 14 - _Time.y * 2.2 + (i.positionWS.x + i.positionWS.z) * 0.6) * 0.5 + 0.5;
                half foam = saturate(smoothstep(0.72, 1.0, shore) + smoothstep(0.5, 0.95, shore) * smoothstep(0.7, 0.95, band) * 0.8);
                col = lerp(col, _FoamColor.rgb * (0.6 + 0.4 * main.color), foam * 0.85);

                col = MixFog(col, i.fogFactor);
                return half4(col, saturate(_Alpha + foam * 0.3 + fres * 0.1));
            }
            ENDHLSL
        }
    }
    FallBack Off
}
