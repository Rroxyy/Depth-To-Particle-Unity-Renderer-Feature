using System;
using UnityEngine;
using UnityEngine.Rendering;
using System.IO;
using UnityEditor;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

public static class RTHandlePlugins
{
    public const string DefaultSavePath = "Assets/Temp/";
    public const string DefaultFileName = "Saved.png";

    /// <summary>
    /// 保存 RenderTexture 到 PNG 文件（内部封装）
    /// </summary>
    private static void SaveRenderTextureAsPNG(RenderTexture rt, string fullPath)
    {
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA64, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();

        File.WriteAllBytes(fullPath, tex.EncodeToPNG());
        RenderTexture.active = currentRT;

        SetPNGImportSettings(tex, fullPath);

        if (Application.isEditor)
            Object.DestroyImmediate(tex);
        else
            Object.Destroy(tex);

        Debug.Log("RenderTexture 已保存到：" + fullPath);
    }

    /// <summary>
    /// 设置图片导入属性（避免压缩、禁用 Mipmap 等）
    /// </summary>
    private static void SetPNGImportSettings(Texture2D tex, string fullPath)
    {
        AssetDatabase.ImportAsset(fullPath); // 强制刷新资源
        var importer = AssetImporter.GetAtPath(fullPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.sRGBTexture = false;
            importer.maxTextureSize = Mathf.Max(tex.width, tex.height);
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }
    }

    /// <summary>
    /// 保存 RenderTexture 到 PNG 文件
    /// </summary>
    public static void SaveToPNG(RenderTexture rt, string filePath = DefaultSavePath, string fileName = DefaultFileName)
    {
        if (rt == null)
        {
            Debug.LogError("RenderTexture 无效，无法保存。");
            return;
        }

        string fullPath = Path.Combine(filePath, fileName);
        SaveRenderTextureAsPNG(rt, fullPath);
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 保存 RTHandle 到 PNG 文件
    /// </summary>
    public static void SaveToPNG(RTHandle rtHandle, string filePath = DefaultSavePath, string fileName = DefaultFileName)
    {
        if (rtHandle?.rt == null)
        {
            Debug.LogError("RTHandle 无效，无法保存图片。");
            return;
        }

        SaveToPNG(rtHandle.rt, filePath, fileName);
    }

    /// <summary>
    /// 保存 Texture2D 到 PNG 文件
    /// </summary>
    public static void SaveTextureToPNG(Texture2D texture, string filePath = DefaultSavePath, string fileName = DefaultFileName)
    {
        if (texture == null)
        {
            Debug.LogError("Texture2D 无效，无法保存。");
            return;
        }

        string fullPath = Path.Combine(filePath, fileName);
        File.WriteAllBytes(fullPath, texture.EncodeToPNG());
        SetPNGImportSettings(texture, fullPath);
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 将 RTHandle 转换为 Texture2D 并保存
    /// </summary>
    public static Texture2D RTHandleToTexture2D(RTHandle rtHandle, string saveFileName = DefaultFileName)
    {
        if (rtHandle?.rt == null)
        {
            Debug.LogError("RTHandle 无效，无法转换为 Texture2D。");
            return null;
        }

        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = rtHandle.rt;

        Texture2D tex = new Texture2D(rtHandle.rt.width, rtHandle.rt.height, TextureFormat.RGBAFloat, false);
        tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
        tex.Apply();

        RenderTexture.active = currentRT;

        // SaveTextureToPNG(tex, DefaultSavePath, saveFileName);
        return tex;
    }
    
    public static void RTHandleToTexture2DAsync(RTHandle rtHandle, Action<Texture2D> callback)
    {
        if (rtHandle?.rt == null)
        {
            Debug.LogError("RTHandle 无效，无法转换为 Texture2D。");
            return;
        }

        AsyncGPUReadback.Request(rtHandle.rt, 0, TextureFormat.RGBAFloat, request =>
        {
            if (request.hasError)
            {
                Debug.LogError("AsyncGPUReadback 请求失败。");
                return;
            }

            var rawData = request.GetData<Color>();
            Texture2D tex = new Texture2D(rtHandle.rt.width, rtHandle.rt.height, TextureFormat.RGBAFloat, false);
            tex.SetPixels(rawData.ToArray());
            tex.Apply();
            
            callback?.Invoke(tex);
        });
    }
}
