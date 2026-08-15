Shader "Custom/MonkeyFur URP"
{
    Properties
    {
        [Header(Main Texture)]
        _MainTex ("Main Albedo Texture", 2D) = "white" {}
        _MainTiling ("Main Tiling", Vector) = (1,1,0,0)
        _MainOffset ("Main Offset", Vector) = (0,0,0,0)

        [Header(Mask 1)]
        _Mask1 ("Mask 1 (White = Color 1)", 2D) = "black" {}
        _Mask1Tiling ("Mask 1 Tiling", Vector) = (1,1,0,0)
        _Mask1Offset ("Mask 1 Offset", Vector) = (0,0,0,0)

        [Header(Mask 2)]
        _Mask2 ("Mask 2 (White = Color)", 2D) = "black" {}
        _Mask2Tiling ("Mask 2 Tiling", Vector) = (1,1,0,0)
        _Mask2Offset ("Mask 2 Offset", Vector) = (0,0,0,0)

        [Header(Colors)]
        _Color1 ("Color 1", Color) = (0.95, 0.3, 0.3, 1)
        _Color ("Color", Color) = (0.2, 0.6, 1.0, 1)
        _ColorStrength ("Color Solidness", Range(0, 2)) = 1.7
        _Saturation ("Saturation Boost", Range(0, 2)) = 1.2

        [Header(Ambient Occlusion)]
        _AOTex ("Ambient Occlusion Texture", 2D) = "white" {}
        _AOTiling ("AO Tiling", Vector) = (1,1,0,0)
        _AOOffset ("AO Offset", Vector) = (0,0,0,0)
        _AOStrength ("AO Strength", Range(0, 2)) = 1.0

        [Header(Plush Material)]
        _Roughness ("Roughness (Higher = Softer Plush)", Range(0,1)) = 0.96
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uvMain     : TEXCOORD0;
                float2 uvMask1    : TEXCOORD1;
                float2 uvMask2    : TEXCOORD2;
                float2 uvAO       : TEXCOORD3;
                float3 normalWS   : TEXCOORD4;
                float3 positionWS : TEXCOORD5;
                float fogCoord    : TEXCOORD6;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);        SAMPLER(sampler_MainTex);
            TEXTURE2D(_Mask1);          SAMPLER(sampler_Mask1);
            TEXTURE2D(_Mask2);          SAMPLER(sampler_Mask2);
            TEXTURE2D(_AOTex);          SAMPLER(sampler_AOTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Mask1_ST;
                float4 _Mask2_ST;
                float4 _AOTex_ST;

                float4 _Color1;
                float4 _Color;
                half _ColorStrength;
                half _Saturation;
                half _Roughness;
                half _Metallic;
                half _AOStrength;
            CBUFFER_END

            Varyings vert (Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS   = normalInput.normalWS;

                output.uvMain  = TRANSFORM_TEX(input.uv, _MainTex);
                output.uvMask1 = TRANSFORM_TEX(input.uv, _Mask1);
                output.uvMask2 = TRANSFORM_TEX(input.uv, _Mask2);
                output.uvAO    = TRANSFORM_TEX(input.uv, _AOTex);

                output.fogCoord = ComputeFogFactor(vertexInput.positionCS.z);

                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                // Sample textures
                half4 mainColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uvMain);
                half mask1      = SAMPLE_TEXTURE2D(_Mask1, sampler_Mask1, input.uvMask1).r;
                half mask2      = SAMPLE_TEXTURE2D(_Mask2, sampler_Mask2, input.uvMask2).r;
                half ao         = SAMPLE_TEXTURE2D(_AOTex, sampler_AOTex, input.uvAO).r;

                // Color blending (Mask 2 has priority)
                half3 col = mainColor.rgb;
                col = lerp(col, _Color1.rgb, mask1 * _ColorStrength);
                col = lerp(col, _Color.rgb, mask2 * _ColorStrength);

                // Saturation boost
                half lum = dot(col, half3(0.3, 0.59, 0.11));
                col = lerp(lum.xxx, col, _Saturation);

                // Surface Data
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo     = col;
                surfaceData.metallic   = _Metallic;
                surfaceData.smoothness = 1.0 - _Roughness;
                surfaceData.occlusion  = lerp(1.0, ao, _AOStrength);
                surfaceData.alpha      = mainColor.a;

                // Input Data for proper lighting in Unity 6 URP
                InputData inputData = (InputData)0;
                inputData.positionWS          = input.positionWS;
                inputData.normalWS            = normalize(input.normalWS);
                inputData.viewDirectionWS     = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.bakedGI             = SampleSH(inputData.normalWS);
                inputData.shadowCoord         = TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord            = input.fogCoord;

                half4 finalColor = UniversalFragmentPBR(inputData, surfaceData);
                finalColor.rgb = MixFog(finalColor.rgb, input.fogCoord);

                return finalColor;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}