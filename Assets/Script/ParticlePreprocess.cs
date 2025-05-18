using System;
using System.IO;
using UnityEditor;
using UnityEngine;


namespace AkkoParticle
{
    public class ParticlePreprocess : MonoBehaviour
    {
        public ParticleParameters particleParameters;
        [Header("Preprocess")] public bool test = false;
        private string defaultTexturePath;
        private const string defaultSizeName = "SizeTex.exr";

        private void OnValidate()
        {
            if (test)
            {
                defaultTexturePath = AssetDatabase.GetAssetPath(particleParameters);
                defaultTexturePath = System.IO.Path.GetDirectoryName(defaultTexturePath);

                test = false;
                GenerateSizeTexture(defaultTexturePath + defaultSizeName);
            }
        }

        
        public void GenerateSizeTexture(string savePath)
        {
            if(!particleParameters.useSizeCurve)
                return;
            int texWidth = particleParameters.sizeTexWidth;
            Texture2D texture = new Texture2D(texWidth, 1, TextureFormat.RGBAHalf, false, true);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            int width = texture.width;
            for (int x = 0; x < width; x++)
            {
                float t = x / (float)(width - 1);
                float value = particleParameters.particleSizeCurve.Evaluate(t);
                texture.SetPixel(x, 0, new Color(value, 0, 0, 0));
            }

            texture.Apply();
            // Texture2DPlugins.SaveTextureToFile(texture, path);
            SaveTextureToFile(texture, savePath);
            
        }

        public static void SaveTextureToFile(Texture2D tex, string assetPath)
        {
            if (tex == null)
            {
                return;
            }

            if (!tex.isReadable)
            {
                Debug.LogError("Texture is not readable!");
                return;
            }

            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
            string folderPath = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string extension = Path.GetExtension(assetPath).ToLower();
            byte[] data;

            if (extension == ".exr")
            {
                data = tex.EncodeToEXR(Texture2D.EXRFlags.OutputAsFloat);
            }
            else if (extension == ".png")
            {
                data = tex.EncodeToPNG();
            }
            else
            {
                Debug.LogError("Unsupported texture format: " + extension);
                return;
            }

            if (data == null)
            {
                Debug.LogError("Failed to encode texture.");
                return;
            }

            File.WriteAllBytes(fullPath, data);
            Debug.Log("Saved texture to: " + fullPath);
        }
    }
}