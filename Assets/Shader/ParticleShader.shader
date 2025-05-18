Shader "Roxy/ParticleShader"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Size("Particle Size", Float) = 1.0
        [HDR]_Color("Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "IgnoreDepthPrepass" = "True"
            "IgnoreDepthTexture" = "True"
        }

        Pass
        {
            Name "ParticlePass"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma multi_compile_instancing
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Shader/Noise.hlsl"

            struct Particle
            {
                float3 position;
                float3 velocity;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);


            float4 _Color;
            StructuredBuffer<Particle> particles;
            float _Size;
            float _EnableRandomSize;
            float _RandomSizeRange;


            // uniform uint _BaseVertexIndex;


            struct Attributes
            {
                uint vertexID : SV_VertexID;
                uint instanceId : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float GetRandomSize(float sizeMin, float sizeMax, int id)
            {
                return random(id, sizeMin, sizeMax);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                uint particleIndex = input.instanceId;
                uint quadVertexIndex = input.vertexID % 3;

                float3 worldPos = particles[particleIndex].position;

                float2 triangleOffsets[3] = {
                    float2(-0.5, -0.5), // 左下
                    float2(0.0, 0.366025), // 顶点
                    float2(0.5, -0.5), // 右下
                };

                float size = _Size;


                if (_EnableRandomSize > 0)
                {
                    size = GetRandomSize(max(0, size - _RandomSizeRange), size + _RandomSizeRange, particleIndex);
                }

                float2 offset = triangleOffsets[quadVertexIndex] * size;

                float3 right = normalize(UNITY_MATRIX_I_V[0].xyz);
                float3 up = normalize(UNITY_MATRIX_I_V[1].xyz);

                float3 offsetWorld = right * offset.x + up * offset.y;
                float3 finalWorldPos = worldPos + offsetWorld;

                output.positionHCS = TransformWorldToHClip(finalWorldPos);
                output.uv = triangleOffsets[quadVertexIndex] + 0.5;

                return output;
            }


            half4 frag(Varyings input) : SV_Target
            {
                return _Color;
            }
            ENDHLSL
        }
    }
}