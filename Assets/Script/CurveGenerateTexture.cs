using System.IO;
using Unity.Mathematics; // ✅ 改为标准IO
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace AkkoParticle
{
    public class CurveGenerateTexture : MonoBehaviour
    {
        [Header("Base")] 
        [SerializeField] private ParticleParameters particleParameters;
        [SerializeField] private bool preprocessing;
        
        private Material material;

        private const int defaultSize = 32;
        private const string defaultTexturePath = "Assets/Particle Parameters/";
        private const string defaultSizeName = "curveSize.exr";
        private const string defaultColorName ="curveColor.exr" ;
        private const string defaultVerticesName = "Vertices.exr";
        private const string defaultIndexName = "Indices.exr";
        void OnValidate()
        {
            if (preprocessing)
            {
                preprocessing = false;

                if (particleParameters == null)
                {
                    Debug.Log("No particle parameters set");
                    return;
                }
                
                Preprocessing(particleParameters);
                GenerateVerticesTexture();
                GenerateIndicesTexture();

            }
            
        }


        private AnimationCurve sizeCurve;
        private AnimationCurve velocityCurve;
        private Gradient gradientColor;
        private bool useSizeCurve;
        private bool useGradientColor;
        private bool useVelocity;

        /// <summary>
        /// Preprocessing color,size and velocity curve
        /// </summary>
        public void  Preprocessing(ParticleParameters particleParameters, int width = defaultSize)
        {
            Texture2D sizeTexture = new Texture2D(width, 1, TextureFormat.RGBAHalf, false, true);
            sizeTexture.wrapMode = TextureWrapMode.Clamp;
            sizeTexture.filterMode = FilterMode.Bilinear;

            Texture2D colorTexture = new Texture2D(width, 1, TextureFormat.RGBAHalf, false, true);
            colorTexture.wrapMode = TextureWrapMode.Clamp;
            colorTexture.filterMode = FilterMode.Bilinear;

            useSizeCurve = particleParameters.useSizeCurve;
            sizeCurve = particleParameters.particleSizeCurve;

            useGradientColor = particleParameters.useGradientColor;
            gradientColor = particleParameters.gradientColor;

            useVelocity = particleParameters.useVelocityCurve;
            velocityCurve = particleParameters.velocityCurve;

            for (int x = 0; x < width; x++)
            {
                float t = x / (float)(width - 1);
                float sizeValue = useSizeCurve ? sizeCurve.Evaluate(t) : 1;
                float velocityValue = useVelocity ? velocityCurve.Evaluate(t) : 1;
                Color color = useGradientColor ? gradientColor.Evaluate(t) : Color.white;

                if (useVelocity)
                {
                    particleParameters.velocityArray[x] = velocityValue;
                }

                sizeTexture.SetPixel(x, 0, new Color(sizeValue, 0, 0, 1));

                if (useGradientColor)
                {
                    colorTexture.SetPixel(x, 0, color);
                }
            }

            sizeTexture.Apply();
            colorTexture.Apply();
            SaveTextureToFile(sizeTexture, defaultTexturePath+defaultSizeName);
            SaveTextureToFile(colorTexture, defaultTexturePath+defaultColorName);
        }

        public static void SaveTextureToFile(Texture2D tex, string assetPath)
        {
            if (tex == null)
            {
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

            AssetDatabase.ImportAsset(assetPath);
            FixTextureImportSetting(assetPath);
            AssetDatabase.Refresh();
        }


        public static void FixTextureImportSetting(string assetPath)
        {
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            if (importer != null)
            {
                importer.sRGBTexture = false;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 8192;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.textureType = TextureImporterType.Default;
                importer.alphaSource = TextureImporterAlphaSource.None;
                
                TextureImporterPlatformSettings platformSettings = importer.GetDefaultPlatformTextureSettings();
                platformSettings.format = TextureImporterFormat.RGBAFloat;
                importer.SetPlatformTextureSettings(platformSettings);


                importer.SaveAndReimport();

            }
            else
            {
                Debug.LogError("TextureImporter not found for: " + assetPath);
            }
        }


        public void GenerateVerticesTexture()
        {
            Mesh mesh = particleParameters.mesh;
            
            //vertex
            Vector3[] vertices = mesh.vertices;
            Debug.Log("Vertices.Length: "+vertices.Length);
            particleParameters.material.SetFloat("_VerticesLength",vertices.Length);

            Texture2D vertexTex = new Texture2D(vertices.Length, 1, TextureFormat.RGBAFloat, false, true);
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 v = vertices[i];
                // 存 position.xyz 到 rgb，a 可备用或设为 1
                vertexTex.SetPixel(i, 0, new Color(v.x, v.y, v.z, 1.0f));
            }
            vertexTex.Apply();
            SaveTextureToFile(vertexTex, defaultTexturePath+defaultVerticesName);
            
        }

        public void GenerateIndicesTexture()
        {
            Mesh mesh = particleParameters.mesh;
            int[] indices = mesh.triangles;
            
            // Debug.Log("Indices.Length: "+indices.Length);
            // Debug.Log("Indices.realLength: "+mesh.GetIndexCount(0));
            int triangleCount = indices.Length / 3;
            Debug.Log("triangle count: "+triangleCount);
            particleParameters.material.SetFloat("_TriangleCount",triangleCount);

            Texture2D indexTex = new Texture2D(triangleCount, 1, TextureFormat.RGBAFloat, false, true);

            for (int i = 0; i < triangleCount; i++)
            {
                int i0 = indices[i * 3 + 0];
                int i1 = indices[i * 3 + 1];
                int i2 = indices[i * 3 + 2];

                indexTex.SetPixel(i, 0, new Color(i0, i1, i2, 1));
            }
            indexTex.Apply();
            SaveTextureToFile(indexTex, defaultTexturePath+defaultIndexName);
        }
    }
}