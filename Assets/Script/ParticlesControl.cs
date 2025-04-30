using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class ParticlesControl : MonoBehaviour
{
    [Header("Particle Enable")]
    public bool enableParticles = false;
    [Header("Size")]
    [SerializeField] private float particleSize = 1f;

    [SerializeField] private bool enableRandomSize = false;

    [SerializeField] private float randomSizeRange;
    private float setSize = 1f;
    [SerializeField] private AnimationCurve particleSizeCurve;




    [Space(20)]
    [Header("Particle Speed And Direction")]
    [SerializeField] private float particleSpeed = 1f;
    [SerializeField] private bool randomStartSpeed = false;
    [SerializeField] private float randomRangeOfSpeed = 0.5f;

    [Space(5)][SerializeField] private ParticleDirection particleDirection;
    [SerializeField] private float particleLifeTime = 1f;





    [Space(20)]
    [Header("Wind")]
    [SerializeField] private bool enableWind = false;

    [SerializeField] private float windStrength = 1f;

    [SerializeField] private Vector3 windDirection;


    [Space(20)]
    [Header("Color")]
    [GradientUsage(true)][SerializeField] private Gradient gradientColor;




    [Space(20)]
    [Header("Bound")]
    [SerializeField] private float boundSize;
    private Vector3 boundCenter;





    [Space(20)]
    public ComputeShader particleComputeShader; // 计算着色器
    public Material particleMaterial; // 用来绘制粒子的材质
    public Mesh mesh;
    private MaterialPropertyBlock materialPropertyBlock;


    private ComputeBuffer particleBuffer; // 存储粒子数据的ComputeBuffer
    private ComputeBuffer argsBuffer;

    GraphicsBuffer meshTriangles;
    GraphicsBuffer meshPositions;
    GraphicsBuffer commandBuf;
    GraphicsBuffer.IndirectDrawArgs[] commandData;
    const int commandCount = 1;
    private RenderParams rp;

    private int particleCount = 100;
    private float timer = 0f;
    private int kernelHandle;
    private ParticleManager particleManager;
    private float randomSeed;//随机数种子


    public enum ParticleDirection
    {
        Up,
        RandomUp,
        Zero
    }

    // 粒子结构体
    struct Particle
    {
        public Vector3 position;
        public Vector3 velocity;
    }


    void Start()
    {
        if (particleMaterial == null || particleComputeShader == null)
        {
            Debug.LogWarning("Particle Particle Material and Compute Shader are null!");
        }

        kernelHandle = particleComputeShader.FindKernel("CSMain");
        particleMaterial = new Material(particleMaterial);
        randomSeed = Random.Range(0f, 1f);
        boundCenter = Vector3.zero;

        materialPropertyBlock = new MaterialPropertyBlock();

    }

    void Update()
    {
        if (!enableParticles)
            return;
        timer += Time.deltaTime;

        if (timer >= particleLifeTime)
        {
            enableParticles = false;
            Release();
            return;
        }


        // StartCoroutine(IEnumeratorUpdate());
        UpdateParticle();
    }

    public void SetupParticle(Vector3[] positions, ParticleManager pm)
    {
        if (positions == null || positions.Length == 0)
        {
            Release();
            return;
        }
        particleManager = pm;
        particleCount = positions.Length;

        Particle[] particles;
        Vector3 direction = GetDirectionByPD(particleDirection);
        
        // 用 Linq 创建粒子数组
        particles = positions.Select(pos => new Particle
        {
            position = pos,
            velocity = GetFinalDirection(direction)
        }).ToArray();

        // 释放旧 buffer，重新分配
        if (particleBuffer != null) particleBuffer.Release();
        particleBuffer = new ComputeBuffer(particleCount, 2 * 3 * sizeof(float));
        particleBuffer.SetData(particles);
        

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        meshTriangles = new GraphicsBuffer(GraphicsBuffer.Target.Structured, mesh.triangles.Length, sizeof(int));
        meshTriangles.SetData(mesh.triangles);
        meshPositions = new GraphicsBuffer(GraphicsBuffer.Target.Structured, mesh.vertices.Length, 3 * sizeof(float));
        meshPositions.SetData(mesh.vertices);

        commandBuf = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, commandCount, GraphicsBuffer.IndirectDrawArgs.size);
        commandData = new GraphicsBuffer.IndirectDrawArgs[commandCount];
        
        commandData[0].vertexCountPerInstance = mesh.GetIndexCount(0);
        commandData[0].instanceCount = (uint)particleCount;
        commandBuf.SetData(commandData);
        
        rp = new RenderParams(particleMaterial);
        rp.worldBounds = new Bounds(boundCenter, Vector3.one * boundSize);

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        setSize = particleSize;
        enableParticles = true;
        timer = 0f;
    }

    private void UpdateParticle()
    {
        UpdatePosition();
        UpdateMateial();
        
        rp.matProps = materialPropertyBlock;
        Graphics.RenderPrimitivesIndirect(rp, MeshTopology.Triangles, commandBuf, commandCount);
    }
    
    

    private void UpdateMateial()
    {
        float t = timer / particleLifeTime;
        UpdateSize(t);
        // 设置粒子数据到材质中
        materialPropertyBlock.SetBuffer("particles", particleBuffer);
        materialPropertyBlock.SetFloat("_Size", setSize);
        materialPropertyBlock.SetFloat("_EnableRandomSize", enableRandomSize == true ? 1f : -1f);
        materialPropertyBlock.SetFloat("_RandomSizeRange", randomSizeRange * particleSizeCurve.Evaluate(t) * 0.01f);
        materialPropertyBlock.SetColor("_Color", gradientColor.Evaluate(t));
    }

    private void UpdatePosition()
    {
        particleComputeShader.SetBuffer(kernelHandle, "particles", particleBuffer);


        particleComputeShader.SetInt("particleCount", particleCount);
        particleComputeShader.SetFloat("stage", timer / particleLifeTime);

        particleComputeShader.SetBool("enableWind", enableWind);
        particleComputeShader.SetFloat("windStrength", windStrength);
        particleComputeShader.SetVector("windDirection", windDirection);

        int threadsPerGroup = 256;
        int groups = Mathf.CeilToInt((float)particleCount / threadsPerGroup); // 计算需要的线程组数量
        groups = Mathf.Max(groups, 1);
        particleComputeShader.Dispatch(kernelHandle, groups, 1, 1);
    }

    private void UpdateSize(float t)
    {
        setSize = particleSizeCurve.Evaluate(t) * particleSize;

        setSize *= 0.01f;
    }




    private Vector3 GetFinalDirection(Vector3 direction)
    {
        if (!randomStartSpeed)
        {
            direction *= particleSpeed;
        }
        else
        {
            float speed = Random.Range(particleSpeed - randomRangeOfSpeed, randomRangeOfSpeed + particleSpeed);
            direction *= speed;
        }

        return direction * 0.001f;
    }


    private static Vector3 GetDirectionByPD(ParticleDirection particleDirection)
    {
        Vector3 direction = Vector3.zero;
        if (particleDirection == ParticleDirection.Up)
        {
            direction = new Vector3(0, 1, 0);
        }
        else if (particleDirection == ParticleDirection.RandomUp)
        {
            float x = Random.Range(-1, 1);
            float y = Random.Range(5, 10);
            float z = Random.Range(-1, 1);

            direction = new Vector3(x, y, z).normalized;
        }
        else if (particleDirection == ParticleDirection.Zero)
        {
            ;
        }

        return direction;
    }


    void OnDestroy()
    {
        Release();

    }

    public void Release()
    {
        if (particleBuffer != null)
        {
            if (particleBuffer != null)
                particleBuffer.Release();
            if (argsBuffer != null)
                argsBuffer.Release();
            particleManager.DestoryParticle(this);
            Destroy(gameObject);

        }
        
        meshTriangles?.Dispose();
        meshTriangles = null;
        meshPositions?.Dispose();
        meshPositions = null;
        commandBuf?.Dispose();
        commandBuf = null;
        
    }
}