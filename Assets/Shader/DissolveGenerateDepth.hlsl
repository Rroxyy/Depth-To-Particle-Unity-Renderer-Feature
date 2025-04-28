#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Assets/Shader/Noise.hlsl"
CBUFFER_START(UnityPerMaterial)
    float _DissolveHeight;
    float _DissolveWidth;


    float _NoiseParameters;

CBUFFER_END

struct vert
{
    float3 positionOS : POSITION;
};

struct frag
{
    float4 positionCS:SV_POSITION;
    float3 positionWS:TEXCOORD0;
    float4 positionSS:TEXCOORD1;
};

frag Vert(vert input)
{
    frag output;
    output.positionCS = TransformObjectToHClip(input.positionOS);
    output.positionWS = TransformObjectToWorld(input.positionOS);
    output.positionSS = ComputeScreenPos(output.positionCS);

    return output;
}

half4 Frag(frag input): SV_Target
{
    float3 cameraPos = _WorldSpaceCameraPos;
    float3 temp = cameraPos - input.positionWS;
    float distance = length(temp);
    float normalizedDepth = saturate(distance / 5000);

    float noise = worleyNoise3D(input.positionWS * _NoiseParameters);

    _DissolveHeight -= .2f; //实际取样的时候比溶解效果快一点
    clip(_DissolveHeight+noise-input.positionWS.y);
    clip(input.positionWS.y - _DissolveHeight + _DissolveWidth - noise);

    return half4(normalizedDepth, normalizedDepth, normalizedDepth, 1);
}
