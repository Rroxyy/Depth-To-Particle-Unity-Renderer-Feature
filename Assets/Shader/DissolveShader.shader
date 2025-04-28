Shader "Roxy/DissolveShader"
{
    Properties
    {



        [Space(20)]
        [Header(Base)]
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", Float) = 0

        [Space(20)]
        [Header(Emission)]
        [Toggle(_EMISSION)] _UseEmission("Use Emission", Float) = 0
        _EmissionIntensity("Emission Intensity", Range(0, 1)) = 0

        [Space(20)]
        [Header(LightMap)]
        [Toggle(_UseLightMap)] _UseLightMap("Use Light Map", Float) = 0
        _LightMap("Light Map", 2D) = "white"{}
        [ToggleUI] _UseCustomMaterialType("Use Custom Material Type", Float) = 0
        _CustomMaterialType("Custom Material Type", Range(0, 1)) = 1


        [Space(20)]
        [Header(RampMap)]
        [Toggle(_UseRampMap)] _UseRamptMap("Use Ramp Map", Float) = 0
        _RampMap("RampMap", 2D) = "white"{}
        [ToggleUI] _IsDay("Is Day", Float) = 1
        [Header(Shadow)]
        _ShadowLine("ShadowLine", Range(0, 1)) = 0.5
        _ShadowWidth("ShadowWidth", Range(0, 1)) = 0.2
        _Index("Index", Range(0, 5)) = 0
        [HDR] _ShadowColor("Shadow Color", Color) = (1, 1, 1, 1)

        [Space(20)]
        [Header(Normal)]
        [Toggle(_NORMAL_MAP)] _UseNormalMap("Use Normal Map", Float) = 0
        [Normal] _NormalMap("Normal Map", 2D) = "bump" {}


        [Space(20)]
        [Header(Face)]
        [Toggle(_IS_FACE)] _IsFace("Is Face", Float) = 0
        _FaceDirection("Face Direction", Vector) = (0, 0, 1, 0)
        _FaceLightMap("Face Light Map", 2D) = "white" {}
        _FaceShadow("Face Shadow", 2D) = "white" {}


        [Space(20)]
        [Header(Specular Need LightMap)]
        [Toggle(_SPECULAR)] _UseSpecular("Use Specular", Float) = 0
        _SpecularSmoothness("Specular Smoothness", Range(0.01, 10)) = 1
        _NonmetallicIntensity("Nonmetallic Intensity", Range(0, 1)) = 1
        _MetallicIntensity("Metallic Intensity", Range(0, 20)) = 1
        _MetalMap("Metal Map", 2D) = "white" {}


        [Space(20)]
        [Header(Rim Light)]
        [Toggle(_RIM)] _UseRim("Use Rim", Float) = 0
        _RimColor("Rim Color", Color) = (1, 1, 1, 1)
        _RimOffset("Rim Offset", Float) = 1
        _RimThreshold("Rim Threshold", Float) = 1
        _RimIntensity("Rim Intensity", Float) = 1

        [Space(20)]
        [Header(OutLine)]
        [Toggle(_UseOutLine)] _UseOutLine("_UseOutLine", Float) = 0
        
        

        [Space(20)]
        [Header(Dissolve)]
        [Toggle(_EnableDissolve)]_EnableDissolve("Enable Dissolve", Float) = 0
        _DissolveHeight("_DissolveHeight", Float) = 0
        _DissolveWidth("_DissolveWidth", Float) = 0
        [HDR]_DissolveColor("DissolveColor",Color)=(1,1,1,1)
        [Header(Noise)]
        _NoiseParameters("Noise Parameters",Float)=1


    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }
        Pass
        {

            Tags
            {
                "LightMode" = "DissolvePass"
            }
            ZWrite On
            ZTest LEqual
            Cull Off
            Blend Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            
            #include "Assets/Shader/DissolveGenerateDepth.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "Forward"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            Cull [_Cull]
            ZWrite On
            Blend[_SrcBlend][_DstBlend]

            HLSLPROGRAM
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _LIGHT_LAYERS
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fragment _ _WRITE_RENDERING_LAYERS

            // Unity defined keywords
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            #pragma multi_compile_fog
            #pragma multi_compile_fragment _ DEBUG_DISPLAY


            #pragma shader_feature_local_fragment _NORMAL_MAP
            #pragma shader_feature_local_fragment _IS_FACE
            #pragma shader_feature_local_fragment _SPECULAR
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _RIM
            #pragma shader_feature_local_fragment _UseLightMap
            #pragma shader_feature_local_fragment _UseRampMap

            #pragma shader_feature_local_fragment _EnableDissolve


            #pragma vertex vert
            #pragma fragment frag

            #include "MainFile.hlsl"
            ENDHLSL

        }
        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }

            ZWrite On
            ColorMask R
            Cull[_Cull]

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags
            {
                "LightMode" = "DepthNormals"
            }

            ZWrite On
            Cull[_Cull]

            HLSLPROGRAM
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitDepthNormalsPass.hlsl"
            ENDHLSL
        }
        


    }
}