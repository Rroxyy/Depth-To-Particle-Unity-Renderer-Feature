Shader "Roxy/ParticleShader2"
{
    Properties
    {
        [Header(Size)]
        _BaseSize("Base Size",Float)=1
        [Toggle(_USE_SIZE_CURVE)]_UseSizeCurve("Use Size Curve",Float)=0
        _SizeTex("Size Curve Texture",2D)="white" {}

        [Header(Color)]
        [HDR]_BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [Toggle(_USE_COLOR_CURVE_TEX)]_UseColorCurveTex("Use Color Curve Texture",Float)=0
        _ColorCurveTex("Color Curve Texture",2D)="White"{}


        [Header(Mesh Info)]
        _VerticesTex("Vertices Texture",2D)="White"{}
        _VerticesLength("Vertices Lenght",Float)=0
        [Space(5)]
        _IndicesTex("Indices Texture",2D)="White"{}
        _TriangleCount("Triangle Count",Float)=0

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
            Cull Back
            HLSLPROGRAM
            #pragma multi_compile_instancing
            #pragma shader_feature_local_vertex _USE_SIZE_CURVE
            #pragma shader_feature_local_fragment _USE_COLOR_CURVE_TEX
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Shader/Noise.hlsl"
            #include "Assets/Shader/Particle.hlsl"
            

            float _BaseSize;
            float4 _BaseColor;
            StructuredBuffer<Particle> particles;
            
            Texture2D _SizeTex;
            SamplerState sampler_SizeTex;

            Texture2D _ColorCurveTex;
            SamplerState sampler_ColorCurveTex;

            Texture2D _VerticesTex;
            SamplerState sampler_VerticesTex;

            Texture2D _IndicesTex;
            SamplerState sampler_IndicesTex;

            float _VerticesLength;
            float _TriangleCount;


            struct Attributes
            {
                uint vertexID : SV_VertexID;
                uint instanceId : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                uint enable:TEXCOORD0;
                float t:TEXCOORD1;
            };

            float EvaluateSizeCurve(float t)
            {
                float2 uv = float2(t, 0.5); 
                float value = _SizeTex.SampleLevel(sampler_SizeTex, uv, 0).x;
                return value;
            }
            
            float3 GetObjectPosFromVertexID(uint vertexID)
            {
                // 当前三角形索引
                uint triID = vertexID / 3;

                float u = (float)(triID + 0.5) / (float)_TriangleCount;
                float3 indices = _IndicesTex.SampleLevel(sampler_IndicesTex, float2(u, 0.5f), 0).rgb;

                int i0 = (int)indices.x;
                int i1 = (int)indices.y;
                int i2 = (int)indices.z;

                int vertIndex = 0;
                uint remainder = vertexID % 3;
                if (remainder == 0) vertIndex = i0;
                else if (remainder == 1) vertIndex = i1;
                else vertIndex = i2;
                
                float vertU = (vertIndex + 0.5) / _VerticesLength;
                float3 pos = _VerticesTex.SampleLevel(sampler_VerticesTex, float2(vertU, 0.5f), 0).xyz;

                return pos;
            }


            Varyings vert(Attributes input)
            {
                Varyings output;

                uint particleIndex = input.instanceId;
                Particle particle = particles[particleIndex];

                output.enable = particle.active;

                float curveSize = 1.0f;


                float t = float(particle.als.x) / particle.als.y;

                #if _USE_SIZE_CURVE
                curveSize = EvaluateSizeCurve(t);
                #endif
                

                float3 worldPos = particle.position;
                
                float3 localPos = GetObjectPosFromVertexID(input.vertexID);
                float3 offset = localPos * curveSize * _BaseSize;

                float3 finalWorldPos = offset + worldPos;


                output.positionHCS = TransformWorldToHClip(finalWorldPos);
                output.t = t;

                return output;
            }


            half4 frag(Varyings input) : SV_Target
            {
                clip(input.enable < 1 ? -1 : 1);

                #if _USE_COLOR_CURVE_TEX
                float2 uv=float2(input.t,0.5);
                half4 color = SAMPLE_TEXTURE2D(_ColorCurveTex,sampler_ColorCurveTex,uv);
                return color;
                #endif

                return _BaseColor;
            }
            ENDHLSL
        }
    }
}