#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Assets/Shader/Noise.hlsl"

struct vertex
{
    float3 vertex : POSITION;
};

struct v2f
{
    float4 positionCS : SV_POSITION;
    float3 positionWS:TEXCOORD0;
    float4 positionSS : TEXCOORD1;
    float3 positionVS : TEXCOORD2;
};

TEXTURE2D(_CameraDepthTexture);
SAMPLER(sampler_CameraDepthTexture);

half4 _BaseColor;
half4 _EdgeColor;
float _EdgeWidth;

float _NoiseParameter;
float _NoisePow;
float _NoiseThreshold;
half4 _NoiseColor;

float _Temp;

v2f vert(vertex v)
{
    v2f o;
    o.positionCS = TransformObjectToHClip(v.vertex.xyz);
    o.positionWS = TransformObjectToWorld(v.vertex);
    o.positionSS = ComputeScreenPos(o.positionCS);
    o.positionVS = TransformWorldToView(o.positionWS);

    return o;
}

half4 frag(v2f i) : SV_Target
{
    // discard;
    float2 uv = i.positionSS.xy / i.positionSS.w;

    // 从 _CameraDepthTexture 采样的 raw 深度
    float rawDepth = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, uv);
    float sceneDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
    
    float selfDepth = -i.positionVS.z; // 注意 VS 中 -Z 是朝向摄像机的

    // 为了视觉一致，使用深度比例缩放
    float adaptiveEdgeWidth = _EdgeWidth * selfDepth;

    // 计算深度差异
    float depthDiff = sceneDepth - selfDepth;
    depthDiff = depthDiff < 0 ? 1 : depthDiff;
    if (depthDiff>adaptiveEdgeWidth)
    {
        discard;
    }
    

    float3 cameraPos = _WorldSpaceCameraPos;
    float3 temp = cameraPos - i.positionWS;
    float distance = length(temp);
    float normalizedDepth = saturate(distance / 5000);
   
    return  half4(normalizedDepth, normalizedDepth, normalizedDepth, 1);
}
