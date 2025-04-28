using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class ParticlesControl : MonoBehaviour
{
    [Header("Particle Enable")] public bool enableParticles = false;
    [Header("Size")] [SerializeField] private float particleSize = 1f;

    [FormerlySerializedAs("randomizeSize")] [SerializeField]
    private bool enableRandomSize = false;

    [SerializeField] private float randomSizeRange;
    private float setSize = 1f;
    [SerializeField] private AnimationCurve particleSizeCurve;

    [Space(20)] [Header("Particle Speed And Direction")] [SerializeField]
    private float particleSpeed = 1f;

    [SerializeField] private bool randomStartSpeed = false;
    [SerializeField] private float randomRangeOfSpeed = 0.5f;

    [Space(5)] [SerializeField] private ParticleDirection particleDirection;
    [SerializeField] private float particleLifeTime = 1f;

    [Space(20)] [Header("Wind")] [SerializeField]
    private bool enableWind = false;

    [FormerlySerializedAs("windForce")] [SerializeField]
    private float windStrength = 1f;

    [SerializeField] private Vector3 windDirection;

    [Space(20)] [Header("Bound")] [SerializeField]
    private Transform positionCenter;

    [SerializeField] private float boundSize;


    [Space(20)] public ComputeShader particleComputeShader; // 计算着色器
    public Material particleMaterial; // 用来绘制粒子的材质


    private ComputeBuffer particleBuffer; // 存储粒子数据的ComputeBuffer
    private int particleCount = 100;
    private float timer = 0f;
    private int kernelHandle;
    private ParticleManager particleManager;
    private float randomSeed;


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

        UpdateParticle();
    }

    private void UpdateParticle()
    {
        if (particleBuffer == null || particleCount == 0 || particleBuffer.count == 0)
        {
            Debug.LogWarning("WTF?");
            return;
        }
        // 执行计算着色器，更新粒子数据

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

        float t = timer / particleLifeTime;

        UpdateSize(t);

        // 设置粒子数据到材质中
        particleMaterial.SetBuffer("particles", particleBuffer); // 将粒子缓冲区传递到材质中
        particleMaterial.SetFloat("_Size", setSize);
        particleMaterial.SetFloat("_EnableRandomSize", enableRandomSize == true ? 1f : -1f);
        particleMaterial.SetFloat("_RandomSizeRange", randomSizeRange * particleSizeCurve.Evaluate(t)*0.01f);


        // 渲染粒子
        Graphics.DrawProcedural(particleMaterial,
            new Bounds(positionCenter.position, new Vector3(boundSize, boundSize, boundSize)),
            MeshTopology.Triangles, particleCount * 3);
    }

    private void UpdateSize(float t)
    {
        setSize = particleSizeCurve.Evaluate(t) * particleSize;

        setSize *= 0.01f;
    }

    public void SetupParticle(Vector3[] positions, ParticleManager pm)
    {
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


        setSize = particleSize;
        enableParticles = true;
        timer = 0f;
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
            particleBuffer.Release();
            particleManager.DestoryParticle(this);
            Destroy(gameObject);
        }
    }
}