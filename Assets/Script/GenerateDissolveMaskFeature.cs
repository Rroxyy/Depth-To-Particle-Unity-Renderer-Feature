using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

public class GenerateDissolveMaskFeature : ScriptableRendererFeature
{
    [Space(20)] public ShowType showType;

    public enum ShowType
    {
        baseType,
    }

    public RenderPassEvent renderEvent = RenderPassEvent.AfterRendering;
    public CameraType cameraType = CameraType.Game;
    public RTToParticlePositions rtToParticlePositions;
    public LayerMask layerMask = -1;

    public bool captureScreenshot = false;


    private FilteringSettings filteringSettings;

    //这个类定义在后面
    DissolvePass dissolvePass;


    public override void Create()
    {
        if (RendererFeatureManager.instance == null)
        {
            return;
        }
        rtToParticlePositions = RendererFeatureManager.instance.rtToParticlePositions;
        
        dissolvePass = new DissolvePass(renderEvent, rtToParticlePositions);
        var renderQueueRange = RenderQueueRange.all;
        filteringSettings = new FilteringSettings(renderQueueRange, layerMask);
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        if (RendererFeatureManager.instance == null)
            return;
        if (!RendererFeatureManager.instance.enableDissolveRendererFeature)
            return;

        rtToParticlePositions = RendererFeatureManager.instance.rtToParticlePositions;
        
        if (renderingData.cameraData.cameraType != cameraType)
        {
            return;
        }


        if (showType == ShowType.baseType)
        {
            dissolvePass.ConfigureInput(ScriptableRenderPassInput.Color);

            dissolvePass.SetTarget(renderer.cameraColorTargetHandle, captureScreenshot, filteringSettings,
                showType);
        }
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (RendererFeatureManager.instance == null)
            return;
        if (!RendererFeatureManager.instance.enableDissolveRendererFeature)
            return;

        rtToParticlePositions = RendererFeatureManager.instance.rtToParticlePositions;


        if (renderingData.cameraData.cameraType != cameraType)
        {
            return;
        }

        renderer.EnqueuePass(dissolvePass);
    }

    class DissolvePass : ScriptableRenderPass
    {
        RTHandle cameraColorTarget;
        FilteringSettings filteringSettings;
        RTToParticlePositions rtToParticlePositions;
        ShowType showType;
        bool captureScreenshot = false;

        public DissolvePass(RenderPassEvent renderEvent, RTToParticlePositions rtToParticlePositions)
        {
            this.rtToParticlePositions = rtToParticlePositions;
            renderPassEvent = renderEvent;
        }

        public void SetTarget(RTHandle colorHandle, bool captureScreenshot,
            FilteringSettings filteringSettings, ShowType showType)
        {
            cameraColorTarget = colorHandle;
            this.captureScreenshot = captureScreenshot;
            this.filteringSettings = filteringSettings;
            this.showType = showType;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ConfigureTarget(cameraColorTarget);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (rtToParticlePositions == null)
            {
                Debug.Log("no script");
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get("DissovleMaskPass");
            Profiler.BeginSample("DissovleMaskPass");

            var source = cameraColorTarget;

            var descriptor = renderingData.cameraData.cameraTargetDescriptor;

            var sortingCriteria = SortingCriteria.CommonOpaque;
            var drawSettings =
                CreateDrawingSettings(new ShaderTagId("UniversalForward"), ref renderingData, sortingCriteria);

            if (showType == ShowType.baseType)
            {
                drawSettings =
                    CreateDrawingSettings(new ShaderTagId("DissolvePass"), ref renderingData, sortingCriteria);
                descriptor.depthBufferBits = 0;
                descriptor.colorFormat = RenderTextureFormat.ARGBFloat;
                descriptor.enableRandomWrite = true;
                descriptor.msaaSamples = 1;
                RTHandle depthRT = RTHandles.Alloc(
                    in descriptor,
                    filterMode: FilterMode.Point,
                    wrapMode: TextureWrapMode.Clamp,
                    isShadowMap: false,
                    anisoLevel: 0,
                    mipMapBias: 0f,
                    name: "_DissolveRT"
                );

                //设置画布
                cmd.SetRenderTarget(depthRT.rt);
                cmd.ClearRenderTarget(true, true, Color.clear);

                //立即执行
                context.ExecuteCommandBuffer(cmd);
                context.Submit();
                cmd.Clear();
                
                //画到画布上
                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filteringSettings);

                //还原
                cmd.SetRenderTarget(source);
                context.ExecuteCommandBuffer(cmd);
                context.Submit();
                cmd.Clear();

                /////////////////////////////////////////////////////////
                /////////////////////////////////////////////////////////
                /////////////////////////////////////////////////////////

                //传出rt
                rtToParticlePositions.Setup(depthRT);

                /////////////////////////////////////////////////////////
                /////////////////////////////////////////////////////////
                /////////////////////////////////////////////////////////

                // 保存图像
                if (captureScreenshot)
                {
                    RTHandlePlugins.SaveToPNG(depthRT, RTHandlePlugins.DefaultSavePath, "tempRT.png");
                    captureScreenshot = false;
                }

                RTHandles.Release(depthRT);
            }

            
            CommandBufferPool.Release(cmd);
            Profiler.EndSample();
        }
        
    }
}