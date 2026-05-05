Shader "Custom/InflatableFloor"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.9, 0.1, 0.5, 1.0)
        _SeamCount("Seam Count (Density)", Float) = 10.0
        _SeamSharpness("Seam Sharpness", Float) = 4.0
        _BulgeAmount("Bulge Amount (外鼓)", Float) = 0.04
        _SinkAmount("Sink Amount (内凹)", Float) = 0.05
        _SeamDarkness("Seam Darkness AO", Range(0, 1)) = 0.4
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 300

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : NORMAL;
                float2 uv           : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _SeamCount;
                float _SeamSharpness;
                float _BulgeAmount;
                float _SinkAmount;
                float _SeamDarkness;
            CBUFFER_END

            // 计算接缝的遮罩 (水平方向 UV.x)
            float GetSeamMask(float2 uv)
            {
                float wave = abs(sin(uv.x * 3.14159 * _SeamCount));
                return pow(wave, _SeamSharpness);
            }

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                float seamMask = GetSeamMask(input.uv);
                
                // 顶点位移：接缝处内凹，段中央外鼓
                float bulgeFactor = 1.0 - seamMask;
                float displacement = bulgeFactor * _BulgeAmount - seamMask * _SinkAmount;
                float3 newPosOS = input.positionOS.xyz + normalize(input.normalOS) * displacement;

                output.positionWS = TransformObjectToWorld(newPosOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float seamMask = GetSeamMask(input.uv);
                
                // 接缝暗化 AO
                float aoFactor = lerp(1.0, 1.0 - _SeamDarkness, seamMask);
                float3 albedo = _BaseColor.rgb * aoFactor;

                // 简单的基础光照 (主光 + 环境光)
                Light mainLight = GetMainLight();
                float3 lightColor = mainLight.color;
                float3 lightDir = normalize(mainLight.direction);
                float3 normal = normalize(input.normalWS);

                // 漫反射 (半兰伯特)
                float ndotl = max(0.0, dot(normal, lightDir));
                float3 diffuse = albedo * lightColor * ndotl;

                // 环境光
                float3 ambient = SampleSH(normal) * albedo;

                return half4(diffuse + ambient, _BaseColor.a);
            }
            ENDHLSL
        }
        
        // 阴影投射 Pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float _SeamCount;
                float _SeamSharpness;
                float _BulgeAmount;
                float _SinkAmount;
            CBUFFER_END

            float GetSeamMask(float2 uv)
            {
                float wave = abs(sin(uv.x * 3.14159 * _SeamCount));
                return pow(wave, _SeamSharpness);
            }

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                float seamMask = GetSeamMask(input.uv);
                float bulgeFactor = 1.0 - seamMask;
                float displacement = bulgeFactor * _BulgeAmount - seamMask * _SinkAmount;
                float3 newPosOS = input.positionOS.xyz + normalize(input.normalOS) * displacement;

                output.positionCS = TransformObjectToHClip(newPosOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
