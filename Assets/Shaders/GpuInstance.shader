Shader "Custom/GpuInstance"
{
    Properties
    {
        _Color ("Base Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "MainPass"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM
            #define _SPECULAR_COLOR
            #pragma vertex vert
            #pragma fragment frag
            // #pragma shader_feature _FORWARD_PLUS
            #pragma shader_feature _CLUSTER_LIGHT_LOOP
            #pragma shader_feature_fragment _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma shader_feature_fragment _ADDITIONAL_LIGHT_SHADOWS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/Shaders/Common.hlsl"
            #include "Assets/Shaders/Quaternion.hlsl"

            StructuredBuffer<float3> positions;
            StructuredBuffer<float3> velocities;

            struct Attributes
            {
                float4 positionLS : POSITION;
                float3 normalLS : NORMAL;
                uint instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };


            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 pos = positions[input.instanceID];
                float3 vel = velocities[input.instanceID];

                float3 forward = float3(0, 0, 1);

                float4 q = from_to_rotation(forward, safe_normalize(vel));
                float3 rotated = rotate_vector(input.positionLS.xyz, q);
                float3 rotated_normals = rotate_vector(input.normalLS, q);

                float3 world_pos = TransformObjectToWorld(rotated) + pos;


                output.positionHCS = TransformObjectToHClip(world_pos);
                output.normalWS = TransformObjectToWorldNormal(rotated_normals);
                output.positionWS = world_pos;
                return output;
            }

            half4 frag(Varyings v) : SV_Target
            {
                InputData lighting = (InputData)0;
                lighting.positionWS = v.positionWS;
                lighting.normalWS = normalize(v.normalWS);
                lighting.viewDirectionWS = GetWorldSpaceViewDir(v.positionWS);
                lighting.shadowCoord = TransformWorldToShadowCoord(v.positionWS);

                SurfaceData surface = (SurfaceData)0;
                surface.albedo = _Color;
                surface.alpha = 1;
                surface.smoothness = .9;
                surface.specular = .9;
                return UniversalFragmentBlinnPhong(lighting, surface) + unity_AmbientSky;
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"

            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            ColorMask 0

            HLSLPROGRAM
            #define _SPECULAR_COLOR
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "Assets/Shaders/Common.hlsl"
            #include "Assets/Shaders/Quaternion.hlsl"

            StructuredBuffer<float3> positions;
            StructuredBuffer<float3> velocities;

            float3 _LightDirection;

            struct Attributes
            {
                float4 positionLS : POSITION;
                float3 normalLS : NORMAL;
                uint instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            float4 GetShadowPositionHCLip(Attributes input)
            {
                float3 pos = positions[input.instanceID];
                float3 vel = velocities[input.instanceID];
                float3 forward = float3(0, 0, 1);

                float4 q = from_to_rotation(forward, safe_normalize(vel));
                float3 rotated = rotate_vector(input.positionLS.xyz, q);
                float3 rotated_normals = rotate_vector(input.normalLS, q);

                float3 world_pos = TransformObjectToWorld(rotated) + pos;

                float3 position_ws = world_pos;
                float3 normal_ws = TransformObjectToWorldNormal(rotated_normals);
                float4 position_cs = TransformWorldToHClip(ApplyShadowBias(position_ws, normal_ws, _LightDirection));
                return position_cs;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = GetShadowPositionHCLip(input);
                return output;
            }

            half4 frag(Varyings v) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}