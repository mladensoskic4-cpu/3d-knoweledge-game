// Stilizovan osvijetljen šejder za sve modele u igri (teren, staza, životinje, vozila, kuće).
// Boja dolazi iz boja vrhova (vertex color), pa cijeli svijet koristi jedan materijal (brzo na telefonu).
// Alfa boje vrha < 1 znači „svijetli noću” (prozori, farovi, lampioni): sjaj = (1 - alfa) * _TrkaGlow.
Shader "TrkaZnanja/Lit"
{
    Properties
    {
        _BaseColor ("Boja (množi se sa bojom vrha)", Color) = (1, 1, 1, 1)
        _ShadowColor ("Boja sjenke", Color) = (0.42, 0.48, 0.66, 1)
        _RimColor ("Rub (kontra svjetlo)", Color) = (1, 0.96, 0.88, 1)
        _RimStrength ("Jačina ruba", Range(0, 1)) = 0.28
        _Gloss ("Sjaj (plastika)", Range(0, 1)) = 0.25
        _Wrap ("Mekoća svjetla", Range(0, 1)) = 0.45
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        LOD 200

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half4 _ShadowColor;
            half4 _RimColor;
            half _RimStrength;
            half _Gloss;
            half _Wrap;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            half _TrkaGlow;          // globalno: 0 danju, 1 noću
            half4 _TrkaGlowColor;    // globalno: boja noćnih svjetala

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                half4 color : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.color = v.color;
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half3 albedo = i.color.rgb * _BaseColor.rgb;
                float3 n = normalize(i.normalWS);
                float3 viewDir = normalize(GetWorldSpaceViewDir(i.positionWS));

                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                Light main = GetMainLight(shadowCoord);
                half ndl = dot(n, main.direction);
                // mekano, „igračka” osvjetljenje: obmotan lambert + blagi prelaz u boju sjenke
                half wrapped = saturate((ndl + _Wrap) / (1 + _Wrap));
                half lit = smoothstep(0.0, 0.85, wrapped) * main.shadowAttenuation * main.distanceAttenuation;
                half3 shadowTint = lerp(_ShadowColor.rgb, 1, 0.25);
                half3 diffuse = albedo * lerp(shadowTint * 0.55, main.color, lit);

                // ambijent iz neba (sferni harmonici koje postavlja RenderSettings)
                half3 ambient = SampleSH(n) * albedo * 0.85;

                // plastični odsjaj
                float3 h = normalize(main.direction + viewDir);
                half spec = pow(saturate(dot(n, h)), 24 + 96 * _Gloss) * _Gloss * lit;

                // kontra svjetlo na ivicama (izdvaja likove od pozadine)
                half rim = pow(1 - saturate(dot(n, viewDir)), 3) * _RimStrength * (0.35 + 0.65 * lit);

                half3 color = diffuse + ambient + spec * main.color + rim * _RimColor.rgb;

                #if defined(_ADDITIONAL_LIGHTS)
                uint count = GetAdditionalLightsCount();
                for (uint li = 0u; li < count; ++li)
                {
                    Light l = GetAdditionalLight(li, i.positionWS);
                    half a = saturate(dot(n, l.direction) * 0.5 + 0.5) * l.distanceAttenuation;
                    color += albedo * l.color * a;
                }
                #endif

                // noćna svjetla: prozori, farovi, lampioni
                half glowMask = 1 - i.color.a;
                color += glowMask * _TrkaGlow * _TrkaGlowColor.rgb * 2.2 + glowMask * albedo * 0.15;

                color = MixFog(color, i.fogFactor);
                return half4(color, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 vert(Attributes v) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(v);
                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 nWS = TransformObjectToWorldNormal(v.normalOS);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 lightDir = normalize(_LightPosition - posWS);
                #else
                    float3 lightDir = _LightDirection;
                #endif
                float4 pos = TransformWorldToHClip(ApplyShadowBias(posWS, nWS, lightDir));
                #if UNITY_REVERSED_Z
                    pos.z = min(pos.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    pos.z = max(pos.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return pos;
            }

            half4 frag() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }
            half frag(Varyings i) : SV_Target { return i.positionCS.z; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                return o;
            }
            half4 frag(Varyings i) : SV_Target { return half4(normalize(i.normalWS), 0); }
            ENDHLSL
        }
    }

    // Ako URP nije uključen (projekat nije podešen), bar se vidi model umjesto roze boje.
    FallBack "Diffuse"
}
