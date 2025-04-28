using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

public class RTToParticlePositions : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;

    [Header("CS")] [SerializeField] private ComputeShader depthToWorldShader;

    [Header("Instance")] [SerializeField] private Material material;
    [SerializeField] private Transform boundPosition;


    private List<Vector3> positions;

    private int kernel;

    private static readonly int RT = Shader.PropertyToID("rt");
    private static readonly int PositionsBuffer = Shader.PropertyToID("positionsBuffer");
    private static readonly int TEXSize = Shader.PropertyToID("texSize");
    private static readonly int CameraPosition = Shader.PropertyToID("cameraPosition");
    private static readonly int InverseProjection = Shader.PropertyToID("inverseProjection");
    private static readonly int InverseView = Shader.PropertyToID("inverseView");
    private static readonly int Near = Shader.PropertyToID("near");
    private static readonly int Far = Shader.PropertyToID("far");

    public void Setup(RTHandle rtHandle)
    {
        if (rtHandle == null)
        {
            Debug.LogError("RTHandle is null or CameraType is not Game");
            return;
        }

        kernel = depthToWorldShader.FindKernel("CSMain");

        StartFindPixelAsync(rtHandle);
    }

    public void StartFindPixelAsync(RTHandle rtHandle)
    {
        StartCoroutine(FindPixelInTextureByCS_Async(rtHandle));
    }


    private IEnumerator FindPixelInTextureByCS_Async(RTHandle rtHandle)
    {
        if (rtHandle == null || rtHandle.rt.width == 0 || rtHandle.rt.height == 0)
        {
            Debug.LogError("Texture size is 0");
            yield break;
        }


        RenderTexture rt = rtHandle.rt;

        int width = rt.width;
        int height = rt.height;
        int maxCount = width * height;

        ComputeBuffer appendBuffer = new ComputeBuffer(maxCount, sizeof(float) * 3, ComputeBufferType.Append);
        appendBuffer.SetCounterValue(0);

        ComputeBuffer countBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        
        depthToWorldShader.SetTexture(kernel, RT, rt);
        depthToWorldShader.SetBuffer(kernel, PositionsBuffer, appendBuffer);
        depthToWorldShader.SetVector(TEXSize, new Vector2(width, height));
        depthToWorldShader.SetVector(CameraPosition, mainCamera.transform.position);
        depthToWorldShader.SetMatrix(InverseProjection, mainCamera.projectionMatrix.inverse);
        depthToWorldShader.SetMatrix(InverseView, mainCamera.worldToCameraMatrix.inverse);
        depthToWorldShader.SetFloat(Near, mainCamera.nearClipPlane);
        depthToWorldShader.SetFloat(Far, mainCamera.farClipPlane);

        int threadGroupX = Mathf.CeilToInt(width / 8.0f);
        int threadGroupY = Mathf.CeilToInt(height / 8.0f);
        depthToWorldShader.Dispatch(kernel, threadGroupX, threadGroupY, 1);

        yield return StartCoroutine(WaitForAppendBuffer(appendBuffer, countBuffer));
    }

    private IEnumerator WaitForAppendBuffer(ComputeBuffer appendBuffer, ComputeBuffer countBuffer)
    {
        // 延迟，等待 GPU 完成写入
        yield return new WaitForEndOfFrame();

        // 拷贝计数,同步
        // 但是countBuffer 的数据并不在 CopyCount 执行完后立刻可用，而是需要等待 GPU 完成对 countBuffer 的写入。
        ComputeBuffer.CopyCount(appendBuffer, countBuffer, 0);

        //异步读取
        AsyncGPUReadback.Request(countBuffer, (countRequest) =>
        {
            if (countRequest.hasError)
            {
                Debug.LogError("Readback countBuffer failed.");
                return;
            }

            int actualCount = countRequest.GetData<int>()[0];

            if (actualCount == 0)
            {
                appendBuffer.Release();
                countBuffer.Release();
                return;
            }

            // 异步读取 appendBuffer 数据
            AsyncGPUReadback.Request(appendBuffer, actualCount * sizeof(float) * 3, 0, (dataRequest) =>
            {
                if (dataRequest.hasError)
                {
                    Debug.LogError("Readback appendBuffer failed.");
                    return;
                }

                Vector3[] resultPositions = dataRequest.GetData<Vector3>().ToArray();

                if (EditorApplication.isPlaying)
                {
                    //输出到粒子系统
                    ParticleManager.instance.AddParticle(resultPositions);
                }

                appendBuffer.Release();
                countBuffer.Release();
            });
        });
    }

    private void OnDestroy()
    {
    }

    private Ray GetScreenPointToRayByMe(Vector3 screenPoint)
    {
        Matrix4x4 inverseProjection = mainCamera.projectionMatrix.inverse;
        Matrix4x4 inverseView = mainCamera.worldToCameraMatrix.inverse;

        Vector2 ndc = new Vector2(
            (screenPoint.x / Screen.width) * 2f - 1f,
            (screenPoint.y / Screen.height) * 2f - 1f
        );

        float clipZ = 0f;

        Vector4 clipPos = new Vector4(ndc.x, ndc.y, clipZ, 1f);

        Vector4 eyePos = inverseProjection * clipPos;
        eyePos /= eyePos.w;

        Vector3 worldPos = (inverseView * eyePos);
        Vector3 dir = (worldPos - mainCamera.transform.position).normalized;

        return new Ray(mainCamera.transform.position, dir);
    }
}