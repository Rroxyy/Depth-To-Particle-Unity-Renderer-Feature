using System.IO;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace AkkoParticle
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Particle
    {
        public Vector3 position;
        public Vector3 direction;
        public float velocity;

        /// <summary>
        /// Age,LifeTime,Size
        /// </summary>
        public Vector3 als;

        /// <summary>
        /// Active: >0 alive
        /// </summary>
        public uint active;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ParticleStatic
    {
        public uint useGravity;
        public Vector3 gravity;

        public uint useVelocityCurve;
        public uint velocityCurveCount;

        public uint overwriteMode;
        public uint renderMode;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct InitializeParameters
    {
        public Vector3 dirction;
        public uint enableRandomDirection;
        public Vector3 randomDirectionRange;

        public float size;
        public uint enableRandomSize;
        public float randomSizeRange;

        public float velocity;
        public uint enableRandomVelocity;
        public float randomVelocityRange;

        public float lifeTime;
        public uint enableRandomLifeTime;
        public float randomLifeTimeRange;
    };


    public enum OverwriteMode : uint
    {
        NoOverwrite = 0u,
        Overwrite = 1u,
    }

    public enum RenderMode : uint
    {
        ClipMode = 0u,
        AppendBufferMode = 1u
    }

    [CreateAssetMenu(fileName = "NewParticleParameters", menuName = "Akko/Particle Parameters")]
    public class ParticleParameters : ScriptableObject
    {
        [Header("Base")] public Material material;
        public Mesh mesh;
        public ComputeShader CS;
        public float minContinueTime; //取值为粒子的最大存活时间和这个参数的max
        public OverwriteMode overwriteMode;
        public RenderMode renderMode;


        [Space] public int maxParticleCount;

        [Header("Color")] public bool useGradientColor;
        [GradientUsage(true)] public Gradient gradientColor;
        public int colorTexWidth = 32;


        [Header("Velocity Setting")] public float velocity;
        public bool enableRandomVelocity;
        public float randomVelocityRange;

        [Space] public bool useVelocityCurve;
        public AnimationCurve velocityCurve;
        public int velocityCurveArrayLenght;
        public float[] velocityArray;


        [Header("Direction Setting")] public Vector3 dirction;
        public bool enableRandomDirection;
        public Vector3 randomDirectionRange;


        [Header("Size Setting")] public float size;
        public bool enableRandomSize;
        public float randomSizeRange;

        [Space] public bool useSizeCurve;
        public AnimationCurve particleSizeCurve;
        public int sizeTexWidth;

        [Header("Life Time")] public float lifeTime;
        public bool enableRandomLifeTime;
        public float randomLifeTimeRange;

        [Header("Gravity Setting")] public bool useGravity;
        public Vector3 gravity;

        [Header("Bounds")] public Vector3 boundsCenter;
        public float boundsSize;

        #region GetParticleStatic

        public ParticleStatic GetParticleStatic()
        {
            ParticleStatic particleStatic = new ParticleStatic();
            particleStatic.useGravity = useGravity == true ? (uint)1 : 0;
            particleStatic.gravity = gravity;

            particleStatic.useVelocityCurve = useVelocityCurve == true ? (uint)1 : 0;
            particleStatic.velocityCurveCount = (uint)(useVelocityCurve ? velocityArray.Length : 0);

            particleStatic.overwriteMode = (uint)overwriteMode;
            particleStatic.renderMode = (uint)renderMode;

            return particleStatic;
        }

        #endregion

        #region GetInitializeParameters

        public InitializeParameters GetInitializeParameters()
        {
            InitializeParameters initializeParameters = new InitializeParameters();

            // Direction
            initializeParameters.dirction = dirction;
            initializeParameters.enableRandomDirection = enableRandomDirection ? 1u : 0u;
            initializeParameters.randomDirectionRange = randomDirectionRange;

            // Size
            initializeParameters.size = size;
            initializeParameters.enableRandomSize = enableRandomSize ? 1u : 0u;
            initializeParameters.randomSizeRange = randomSizeRange;

            // Velocity
            initializeParameters.velocity = velocity;
            initializeParameters.enableRandomVelocity = enableRandomVelocity ? 1u : 0u;
            initializeParameters.randomVelocityRange = randomVelocityRange;

            // LifeTime
            initializeParameters.lifeTime = lifeTime;
            initializeParameters.enableRandomLifeTime = enableRandomLifeTime ? 1u : 0u;
            initializeParameters.randomLifeTimeRange = randomLifeTimeRange;

            return initializeParameters;
        }

        #endregion

        #region Proprocess

        private string defaultTexturePath;
        private const string defaultSizeTexName = "SizeTex.exr";
        private const string defaultColorTexName = "ColorTex.exr";

        private const string defaultVerticesTexName = "VerticesTex.exr";
        private const string defaultIndicesTexName = "IndicesTex.exr";

        public void Preprocess()
        {
            defaultTexturePath = AssetDatabase.GetAssetPath(this);
            defaultTexturePath = System.IO.Path.GetDirectoryName(defaultTexturePath);

            string sizeTexPath = System.IO.Path.Combine(defaultTexturePath, defaultSizeTexName);
            string colorTexPath = System.IO.Path.Combine(defaultTexturePath, defaultColorTexName);

            string verticesTexPath = System.IO.Path.Combine(defaultTexturePath, defaultVerticesTexName);
            string indicesTexPath = System.IO.Path.Combine(defaultTexturePath, defaultIndicesTexName);

            GenerateColorTexture(colorTexPath);
            GenerateVelocityArray();
            GenerateSizeTexture(sizeTexPath);

            GenerateVerticesTexture(verticesTexPath);
            GenerateIndicesTexture(indicesTexPath);
            AssetDatabase.Refresh();
        }

        private void GenerateIndicesTexture(string indicesTexPath)
        {
            Vector3[] vertices = mesh.vertices;
            int[] indices = mesh.triangles;
            int triangleCount = indices.Length / 3;
            material.SetFloat("_TriangleCount", triangleCount);

            Texture2D indexTex = new Texture2D(triangleCount, 1, TextureFormat.RGBAFloat, false, true);
            indexTex.filterMode = FilterMode.Point;
            indexTex.wrapMode = TextureWrapMode.Clamp;

            for (int i = 0; i < triangleCount; i++)
            {
                int i0 = indices[i * 3 + 0];
                int i1 = indices[i * 3 + 1];
                int i2 = indices[i * 3 + 2];

                indexTex.SetPixel(i, 0, new Color(i0, i1, i2, 1));
            }

            indexTex.Apply();
            SaveTextureToFile(indexTex, indicesTexPath);

            AssetDatabase.ImportAsset(indicesTexPath); //强制 Unity 重新导入指定路径的资源文件
            FixTextureImportSetting(indicesTexPath, true);

            AssignTextureToMaterial(indicesTexPath, "_IndicesTex"); //绑定贴图
        }

        private void GenerateVerticesTexture(string verticesTexPath)
        {
            Vector3[] vertices = mesh.vertices;
            material.SetFloat("_VerticesLength", vertices.Length);

            Texture2D vertexTex = new Texture2D(vertices.Length, 1, TextureFormat.RGBAFloat, false, true);
            vertexTex.filterMode = FilterMode.Point;
            vertexTex.wrapMode = TextureWrapMode.Clamp;

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 v = vertices[i];
                vertexTex.SetPixel(i, 0, new Color(v.x, v.y, v.z, 1.0f));
            }

            vertexTex.Apply();
            SaveTextureToFile(vertexTex, verticesTexPath);

            AssetDatabase.ImportAsset(verticesTexPath); //强制 Unity 重新导入指定路径的资源文件
            FixTextureImportSetting(verticesTexPath, true);

            AssignTextureToMaterial(verticesTexPath, "_VerticesTex"); //绑定贴图
        }

        private void GenerateVelocityArray()
        {
            if (!useVelocityCurve)
                return;
            if (velocityCurveArrayLenght <= 1)
            {
                Debug.LogError("Velocity Array Lenght must be greater than 1");
                return;
            }

            velocityArray = new float[velocityCurveArrayLenght];

            for (int x = 0; x < velocityCurveArrayLenght; x++)
            {
                float t = x / (float)(velocityCurveArrayLenght - 1);
                float value = velocityCurve.Evaluate(t);
                velocityArray[x] = value;
            }
        }

        private void GenerateColorTexture(string colorTexPath)
        {
            material.SetFloat("_UseColorCurveTex", useGradientColor ? 1f : 0f);
            if (!useGradientColor)
            {
                return;
            }

            if (colorTexWidth <= 1)
            {
                Debug.LogWarning("Color texture size must be greater than 1");
                return;
            }

            Texture2D colorTexture = new Texture2D(colorTexWidth, 1, TextureFormat.RGBAFloat, false, true);
            colorTexture.filterMode = FilterMode.Bilinear;
            colorTexture.wrapMode = TextureWrapMode.Clamp;

            for (int x = 0; x < colorTexWidth; x++)
            {
                float t = x / (float)(colorTexWidth - 1);
                Color value = gradientColor.Evaluate(t);
                colorTexture.SetPixel(x, 0, value);
            }

            colorTexture.Apply();
            SaveTextureToFile(colorTexture, colorTexPath); //保存贴图

            AssetDatabase.ImportAsset(colorTexPath); //强制 Unity 重新导入指定路径的资源文件
            FixTextureImportSetting(colorTexPath); //设置贴图格式

            AssignTextureToMaterial(colorTexPath, "_ColorCurveTex"); //material绑定贴图
        }

        private void GenerateSizeTexture(string saveSizePath)
        {
            material.SetFloat("_UseSizeCurve", useSizeCurve ? 1f : 0f);
            if (!useSizeCurve)
            {
                return;
            }

            if (sizeTexWidth <= 1)
            {
                Debug.LogWarning("Size texture size can't be less than 1");
                return;
            }

            Texture2D texture = new Texture2D(sizeTexWidth, 1, TextureFormat.RGBAFloat, false, true);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            for (int x = 0; x < sizeTexWidth; x++)
            {
                float t = x / (float)(sizeTexWidth - 1);
                float value = particleSizeCurve.Evaluate(t);
                texture.SetPixel(x, 0, new Color(value, 0, 0, 0));
            }

            texture.Apply();
            SaveTextureToFile(texture, saveSizePath);

            AssetDatabase.ImportAsset(saveSizePath); //强制 Unity 重新导入指定路径的资源文件
            FixTextureImportSetting(saveSizePath); //设置 Texture 参数

            AssignTextureToMaterial(saveSizePath, "_SizeTex"); //绑定贴图
        }

        private void AssignTextureToMaterial(string texturePath, string materialTexName)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            material.SetTexture(materialTexName, texture);
        }

        private static void SaveTextureToFile(Texture2D tex, string assetPath)
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

        private static void FixTextureImportSetting(string assetPath, bool isMeshTexture = false)
        {
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            if (importer == null)
            {
                Debug.LogError("TextureImporter not found for: " + assetPath);
                return;
            }

            importer.sRGBTexture = false;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 8192;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.npotScale = TextureImporterNPOTScale.None;

            if (isMeshTexture)
            {
                importer.filterMode = FilterMode.Point;
            }
            else
            {
                importer.filterMode = FilterMode.Bilinear;
            }

            TextureImporterPlatformSettings platformSettings = importer.GetDefaultPlatformTextureSettings();
            platformSettings.format = TextureImporterFormat.RGBAFloat;
            importer.SetPlatformTextureSettings(platformSettings);

            importer.SaveAndReimport();
        }

        #endregion
    }
}