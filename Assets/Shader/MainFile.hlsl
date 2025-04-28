#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Assets/Shader/Noise.hlsl"

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

TEXTURE2D(_LightMap);
SAMPLER(sampler_LightMap);

TEXTURE2D(_RampMap);
SAMPLER(sampler_RampMap);

TEXTURE2D(_FaceLightMap);
SAMPLER(sampler_FaceLightMap);

TEXTURE2D(_FaceShadow);
SAMPLER(sampler_FaceShadow);

TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);

TEXTURE2D(_MetalMap);
SAMPLER(sampler_MetalMap);


CBUFFER_START(UnityPerMaterial)
    float _ShadowLine;
    float _ShadowWidth;
    float _Index;

    float _EmissionIntensity;

    float4 _BaseMap_ST;

    float4 _BaseColor;
    float _CustomMaterialType;
    float _UseCustomMaterialType;
    float _IsDay;

    float _IsFace;
    half4 _FaceDirection;

    float _ShadowOffset;
    float _ShadowSmoothness;


    half _SpecularSmoothness;
    half _NonmetallicIntensity;
    half _MetallicIntensity;

    float _RimOffset;
    float4 _RimColor;
    float _RimThreshold;
    float _RimIntensity;

    float4 _ShadowColor;

    float _DissolveHeight;
    float _DissolveWidth;

    float4 _DissolveColor;
    float _NoiseParameters;

CBUFFER_END


struct Vertex
{
    float3 positionOS : POSITION;
    float2 uv : TEXCOORD0;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float4 color : COLOR;
};

struct v2f
{
    float2 uv : TEXCOORD0;
    float3 normalWS : TEXCOORD1;
    float3 tangent : TEXCOORD2;
    float3 positionWS : TEXCOORD3;
    half3 tangentWS : TEXCOORD4;
    half3 bitangentWS : TEXCOORD5;
    float4 positionNDC:TEXCOORD6;
    float4 positionCS:SV_POSITION;
    half4 color : COLOR;
};


v2f vert(Vertex input)
{
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    v2f output = (v2f)0;
    output.positionWS = vertexInput.positionWS;
    output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
    output.normalWS = normalInput.normalWS;
    output.tangent = normalInput.tangentWS;
    output.positionCS = vertexInput.positionCS;
    output.tangentWS = normalInput.tangentWS;
    output.bitangentWS = normalInput.bitangentWS;
    output.positionNDC = vertexInput.positionNDC;
    output.color = input.color;


    return output;
}

half GetShadow(v2f input, half3 lightDirection, half aoFactor)
{
    //法线和光照方向越接近，值越高
    half NDotL = dot(input.normalWS, lightDirection);
    half halfLambert = 0.5 * NDotL + 0.5;

    //aoFactor越小越不受光影响(不易照亮)
    half shadow = saturate(2.0 * halfLambert * max(aoFactor, 0.35));
    //aoFactor>0.9 则没有阴影,好像没有aoFactor>0.6的
    return lerp(shadow, 1.0, step(0.8, aoFactor));
}

half GetFaceShadow(v2f input, half3 lightDirection)
{
    half3 F = SafeNormalize(half3(_FaceDirection.x, 0.0, _FaceDirection.z));
    F = TransformObjectToWorldDir(F);
    half3 L = SafeNormalize(half3(lightDirection.x, 0.0, lightDirection.z));
    half FDotL = dot(F, L);
    half FCrossL = cross(F, L).y;

    half2 shadowUV = input.uv;
    shadowUV.x = lerp(shadowUV.x, 1.0 - shadowUV.x, step(0.0, FCrossL));
    half faceShadowMap = SAMPLE_TEXTURE2D(_FaceLightMap, sampler_FaceLightMap, shadowUV).r;
    half faceShadow = step(-0.5 * FDotL + 0.5, faceShadowMap);

    half faceMask = SAMPLE_TEXTURE2D(_FaceShadow, sampler_FaceShadow, input.uv).a;
    half maskedFaceShadow = lerp(faceShadow, 1.0, faceMask);

    return maskedFaceShadow;
}

half3 GetShadowColor(half shadow, half material)
{
    if (shadow < _ShadowLine)
    {
        half2 rampUV = half2(.01, _Index / 10 + 0.05 + 0.5 * _IsDay);
        half3 shadowRamp = half3(1, 1, 1);

        if (shadow > _ShadowLine - _ShadowWidth)
        {
            float u = smoothstep(0, 1, (shadow - (_ShadowLine - _ShadowWidth)) / _ShadowWidth);
            rampUV = half2(u, _Index / 10 + 0.05 + 0.5 * _IsDay);

            #if _UseRampMap
            shadowRamp = SAMPLE_TEXTURE2D(_RampMap, sampler_RampMap, rampUV).xyz;
           
            #endif
            float3 setShadowColor = lerp(_ShadowColor, 1, smoothstep(0.9, 1.0, rampUV.x));


            return shadowRamp * setShadowColor;
        }

        // float u = smoothstep(0, .5, (shadow - (_ShadowLine - _ShadowWidth)) / _ShadowWidth);
        // rampUV = half2(.5, _Index / 10 + 0.05 + 0.5 * _IsDay);
        #if _UseRampMap
        shadowRamp = SAMPLE_TEXTURE2D(_RampMap, sampler_RampMap, rampUV);
        #endif


        // return 0;
        return shadowRamp * _ShadowColor;
    }
    else
    {
        return 1;
    }
}

half3 GetSpecular(v2f input, half3 lightDirection, half3 albedo, half3 lightMap)
{
    //除非光线方向和view坐标完全相反，粗糙度足够高都会产生高光
    half3 V = GetWorldSpaceNormalizeViewDir(input.positionWS);
    half3 H = SafeNormalize(lightDirection + V);
    half NDotH = dot(input.normalWS, H);
    //0~1的高光
    half blinnPhong = pow(saturate(NDotH), _SpecularSmoothness);
    // return float3(blinnPhong, blinnPhong, blinnPhong);

    //normal.z实际上是深度信息
    half3 normalVS = TransformWorldToViewNormal(input.normalWS, true);
    half2 matcapUV = 0.5 * normalVS.xy + 0.5;
    //法线越与view坐标的方向贴近，就越有高光
    half3 metalMap = SAMPLE_TEXTURE2D(_MetalMap, sampler_MetalMap, matcapUV);
    // return metalMap;

    //lightMap.b是高光区域，值越高，越接近金属?
    //lightMap.r是区分高光类型(金属，非金属等),越高越接近金属
    half3 nonMetallic = albedo * step(1, lightMap.b + blinnPhong) * lightMap.r * _NonmetallicIntensity;
    half3 metallic = blinnPhong * lightMap.b * albedo * metalMap * _MetallicIntensity;
    //lightMap.r>0.9的是金属
    half3 specular = lerp(nonMetallic, metallic, step(0.9, lightMap.r));

    return specular;
}

half GetRim(v2f input)
{
    half3 normalVS = TransformWorldToViewNormal(input.normalWS, true);
    //屏幕空间uv
    float2 uv = input.positionNDC.xy / input.positionNDC.w;
    //
    float2 offset = float2(_RimOffset * normalVS.x / _ScreenParams.x, _RimOffset * normalVS.y / _ScreenParams.y);

    //该点的深度值
    float depth = LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
    //沿法线偏移后的深度值
    float offsetDepth = LinearEyeDepth(SampleSceneDepth(uv + offset), _ZBufferParams);
    //深度值之差大于某个数则认为是边缘
    half rim = smoothstep(0.0, _RimThreshold, offsetDepth - depth) * _RimIntensity;

    //法线与view坐标方向越接近90度则效果越好
    half3 V = GetWorldSpaceNormalizeViewDir(input.positionWS);
    half NDotV = dot(input.normalWS, V);
    half fresnel = pow(saturate(1.0 - NDotV), 5.0);

    return rim * fresnel;
}


half4 frag(v2f input) : SV_Target
{
    float noise = worleyNoise3D(input.positionWS*_NoiseParameters);
    // return half4(noise, noise, noise, 1);
    
    
    // clip(_DissolveHeight - _DissolveWidth + noise - input.positionWS.y);
    #if _EnableDissolve
    clip(_DissolveHeight+noise-input.positionWS.y);
    if (input.positionWS.y>_DissolveHeight-_DissolveWidth+noise)
    {
        return _DissolveColor;
    }
    #endif


    half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
    // half4 baseMap = tex2D(_BaseMap, input.uv);
    half3 albedo = baseMap.rgb * _BaseColor.rgb;
    half alpha = baseMap.a;
    // return float4(albedo,1);

    #if _NORMAL_MAP
    half3x3 tangentToWorld = half3x3(input.tangentWS, input.bitangentWS, input.normalWS);
    half4 normalMap = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv);
    half3 normalTS = UnpackNormal(normalMap);
    half3 normalWS = TransformTangentToWorld(normalTS, tangentToWorld, true);
    input.normalWS = normalWS;

    
    #endif


    Light mainLight = GetMainLight();
    half3 lightDirection = mainLight.direction;

    half aoFactor = .5;
    half material = .5;
    #if _UseLightMap
    half4 lightMap = SAMPLE_TEXTURE2D(_LightMap, sampler_LightMap, input.uv);
    // half4 lightMap = tex2D(_LightMap, input.uv);
    //材质,和阴影颜色有关
    material = lerp(lightMap.a, _CustomMaterialType, _UseCustomMaterialType);
    half4 res = half4(material, material, material, 1);
    // return res;

    //ao 长暗系数(缝隙,环境光遮蔽),越高（白）越受光影响(就不会出现阴影)
    aoFactor = lightMap.g * input.color.r;

    #endif


    #if _IS_FACE
        half shadow = GetFaceShadow(input, lightDirection);
    #else
    half shadow = GetShadow(input, lightDirection, aoFactor);
    #endif


    half3 shadowColor = GetShadowColor(shadow, material);
    // half3 shadowColor = GetShadowColor2(input, lightDirection, material, shadow);


    half3 specular = 0.0;
    //lightMap.b是高光区域，值越高，越接近金属?
    //lightMap.r是区分高光类型(金属，非金属等),越高越接近金属

    #if _SPECULAR && _UseLightMap
        specular = GetSpecular(input, lightDirection, albedo, lightMap.rgb);
    #endif


    half3 emission = 0.0;
    #if _EMISSION
        emission = albedo * _EmissionIntensity * alpha;
    #endif


    half3 rim = 0.0;
    #if _RIM
        rim = albedo * GetRim(input)*_RimColor;
    // return float4(rim,1);
    #endif


    half3 finalColor = albedo * shadowColor + specular + rim + emission;


    return half4(finalColor, 1);
}
