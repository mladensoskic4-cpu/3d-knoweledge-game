// Nebo: gradijent (vrh, horizont, dno), sunce sa oreolom i zvijezde noću. Postavlja se kao RenderSettings.skybox.
Shader "TrkaZnanja/Sky"
{
    Properties
    {
        _TopColor ("Vrh", Color) = (0.25, 0.55, 0.95, 1)
        _HorizonColor ("Horizont", Color) = (0.75, 0.9, 1, 1)
        _BottomColor ("Dno", Color) = (0.55, 0.7, 0.8, 1)
        _SunColor ("Sunce", Color) = (1, 0.95, 0.8, 1)
        _SunDir ("Pravac sunca", Vector) = (0.3, 0.5, 0.4, 0)
        _SunSize ("Veličina sunca", Range(0.001, 0.2)) = 0.04
        _Stars ("Zvijezde", Range(0, 1)) = 0
        _HorizonSharp ("Oštrina horizonta", Range(0.5, 8)) = 2.4
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _TopColor, _HorizonColor, _BottomColor, _SunColor;
                float4 _SunDir;
                float _SunSize, _Stars, _HorizonSharp;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.dir = v.positionOS.xyz;
                return o;
            }

            float Hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float y = d.y;
                half3 col = y > 0
                    ? lerp(_HorizonColor.rgb, _TopColor.rgb, pow(saturate(y), 1.0 / _HorizonSharp))
                    : lerp(_HorizonColor.rgb, _BottomColor.rgb, saturate(-y * 4));

                float3 sd = normalize(_SunDir.xyz);
                float sd2 = dot(d, sd);
                half sun = smoothstep(1 - _SunSize, 1 - _SunSize * 0.6, sd2);
                half halo = pow(saturate(sd2), 64) * 0.5 + pow(saturate(sd2), 8) * 0.18;
                col += _SunColor.rgb * (sun * 2.5 + halo);

                if (_Stars > 0.001 && y > 0)
                {
                    float3 cell = floor(d * 140);
                    float h = Hash(cell);
                    half star = step(0.9965, h) * saturate(y * 3) * _Stars;
                    half tw = 0.6 + 0.4 * sin(_Time.y * (2 + h * 5) + h * 40);
                    col += star * tw * 1.4;
                }
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
