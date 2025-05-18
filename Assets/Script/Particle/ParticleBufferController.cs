using System;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace AkkoParticle
{
    public class ParticleBufferController : MonoBehaviour
    {
        [Header("Base")] public bool enableParticleSystem = false;
        public bool stopParticleSystem = false;
        public float controllerMaxLiveTime;
        [SerializeField] private int maxParticleCount = 10;

        [SerializeField] private Material material;
        [SerializeField] private Mesh mesh;
        [SerializeField] private ComputeShader computeShader;


        [Header("Size Array")] [SerializeField]
        private float[] sizeArr;


        [Header("Bounds")] [SerializeField] private Bounds bounds;


        private ComputeBuffer particleBuffer;
        private ComputeBuffer aliveBuffer; //使用append buffer渲染时
        private ComputeBuffer headIndexBuffer;
        private ComputeBuffer velocityCurveBuffer;
        private ComputeBuffer particleStaticBuffer;
        private ComputeBuffer initializeParametersBuffer;
        private ComputeBuffer addParticlePositionsBuffer;


        private ComputeBuffer emptyBuffer;

        private MaterialPropertyBlock materialPropertyBlock;

        private GraphicsBuffer commandBuf;
        private GraphicsBuffer.IndirectDrawArgs[] commandData;
        private const int commandCount = 1;
        private RenderParams rp;

        private readonly int particleStride = Marshal.SizeOf<Particle>();
        private readonly int staticStride = Marshal.SizeOf<ParticleStatic>();
        private readonly int initializeStride = Marshal.SizeOf<InitializeParameters>();

        private int kernelHandle;
        private int addParticleHandle;
        private int initializeHandle;

        private ParticleManager2 pm;
        private ParticleParameters particleParameters;
        private ParticleStatic particleStatic;

        public enum OverwriteMode
        {
            ForceOverwrite,
            NoOverwrite,
        }


        public void Initialize(Material _material, Mesh _mesh, ComputeShader _computeShader,
            ParticleManager2 _manager, ParticleParameters _parameters)
        {
            Release();
            particleParameters = _parameters;

            particleStatic = particleParameters.GetParticleStatic();
            pm = _manager;
            material = _material;
            mesh = _mesh;
            computeShader = Instantiate(_computeShader);
            maxParticleCount = particleParameters.maxParticleCount;

            enableParticleSystem = false;
            kernelHandle = computeShader.FindKernel("UpdateParticle");
            addParticleHandle = computeShader.FindKernel("AddParticle");
            initializeHandle = computeShader.FindKernel("InitializeParticle");


            materialPropertyBlock = new MaterialPropertyBlock();

            bounds = new Bounds(particleParameters.boundsCenter, Vector3.one * particleParameters.boundsSize);
            rp = new RenderParams(material);
            rp.worldBounds = bounds;


            InitializeParticle();

            //append buffer
            if (particleStatic.renderMode == (uint)RenderMode.AppendBufferMode)
            {
                aliveBuffer = new ComputeBuffer(maxParticleCount, particleStride, ComputeBufferType.Append);
            }


            //头指针
            if (headIndexBuffer != null) headIndexBuffer.Release();
            headIndexBuffer = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.Raw);
            uint[] headInit = new uint[] { 0 };
            headIndexBuffer.SetData(headInit);

            //velocity buffer
            if (particleParameters.useVelocityCurve)
            {
                velocityCurveBuffer = new ComputeBuffer(particleParameters.velocityArray.Length, sizeof(float));
                velocityCurveBuffer.SetData(particleParameters.velocityArray);
            }

            //particle static buffer
            //gravity ,whether use velocity curve
            particleStaticBuffer = new ComputeBuffer(1, staticStride);
            particleStaticBuffer.SetData(new ParticleStatic[] { particleStatic });

            //empty buffer
            emptyBuffer = new ComputeBuffer(1, sizeof(float));
            emptyBuffer.SetData(new float[] { 1f });

            //InitializeParameters
            InitializeParameters initializeParameters = particleParameters.GetInitializeParameters();
            initializeParametersBuffer = new ComputeBuffer(1, initializeStride);
            initializeParametersBuffer.SetData(new InitializeParameters[] { initializeParameters });


            CommandBufferInitialize();
        }

        private void InitializeParticle()
        {
            Particle[] particles = new Particle[maxParticleCount];
            if (particleBuffer != null) particleBuffer.Release();
            particleBuffer = new ComputeBuffer(maxParticleCount, particleStride);
            particleBuffer.SetData(particles);

            computeShader.SetInt("maxParticleCount", maxParticleCount);
            computeShader.SetBuffer(initializeHandle, "particles", particleBuffer);

            int threadsPerGroup = 256;
            int groups = Mathf.CeilToInt((float)maxParticleCount / threadsPerGroup);
            groups = Mathf.Max(groups, 1);
            computeShader.Dispatch(initializeHandle, groups, 1, 1);
        }


        private void CommandBufferInitialize()
        {
            commandBuf = new GraphicsBuffer(
                GraphicsBuffer.Target.IndirectArguments, // 类型是“间接绘制指令”
                commandCount, // 要放几条指令（比如多个mesh，或多个批次）
                GraphicsBuffer.IndirectDrawArgs.size // 每条指令多大（字节数）
            );

            commandData = new GraphicsBuffer.IndirectDrawArgs[commandCount];

            commandData[0].vertexCountPerInstance = (uint)mesh.triangles.Length; //索引数量
            commandData[0].instanceCount = (uint)maxParticleCount; //实例数量
            commandData[0].startVertex = 0;
            commandData[0].startInstance = 0;

            commandBuf.SetData(commandData);
        }


        public void AddParticles(Vector3[] positions)
        {
            if (positions == null || positions.Length == 0)
            {
                return;
            }

            if (stopParticleSystem)
            {
                return;
            }

            int addParticleCount = positions.Length;
            
            addParticlePositionsBuffer = new ComputeBuffer(addParticleCount, sizeof(float) * 3);
            addParticlePositionsBuffer.SetData(positions);
            
            AddParticles(addParticlePositionsBuffer, addParticleCount);

            addParticlePositionsBuffer?.Release();
            addParticlePositionsBuffer = null;
        }

        public void AddParticles(ComputeBuffer positionsBuffer, int positionsCount)
        {
            if (stopParticleSystem)
            {
                return;
            }

            int addParticleCount = positionsCount;

            computeShader.SetBuffer(addParticleHandle, "particles", particleBuffer);
            computeShader.SetBuffer(addParticleHandle, "particleStaticBuffer", particleStaticBuffer);
            computeShader.SetBuffer(addParticleHandle, "headIndexBuffer", headIndexBuffer);
            computeShader.SetBuffer(addParticleHandle, "initializeParameters", initializeParametersBuffer);
            computeShader.SetBuffer(addParticleHandle, "addParticlePositions", positionsBuffer);


            computeShader.SetInt("maxParticleCount", maxParticleCount);
            computeShader.SetInt("addParticleCount", addParticleCount);

            int threadsPerGroup = 256;
            int groups = Mathf.CeilToInt((float)addParticleCount / threadsPerGroup); // 计算需要的线程组数量
            groups = Mathf.Max(groups, 1);
            computeShader.Dispatch(addParticleHandle, groups, 1, 1);
        }

        public void UpdateInManager(float deltaTime)
        {
            if (!enableParticleSystem)
                return;

            if (!stopParticleSystem)
            {
                controllerMaxLiveTime -= deltaTime;
            }

            if (controllerMaxLiveTime < 0)
            {
                Release();
                pm.RemoveParticleBuffer(particleParameters);
                Destroy(this);
                return;
            }

            if (particleStatic.renderMode == (uint)RenderMode.AppendBufferMode)
            {
                aliveBuffer.SetCounterValue(0);
            }

            //Compute Shader Set
            computeShader.SetBuffer(kernelHandle, "particles", particleBuffer);
            computeShader.SetBuffer(kernelHandle, "aliveBuffer",
                particleStatic.renderMode == (uint)RenderMode.AppendBufferMode ? aliveBuffer : emptyBuffer);
            computeShader.SetBuffer(kernelHandle, "particleStaticBuffer", particleStaticBuffer);
            computeShader.SetBuffer(kernelHandle, "velocityCurve",
                particleParameters.useVelocityCurve ? velocityCurveBuffer : emptyBuffer);

            computeShader.SetInt("particleCount", maxParticleCount);
            computeShader.SetFloat("deltaTime", stopParticleSystem ? 0 : Time.deltaTime);
            
            int threadsPerGroup = 256;
            int groups = Mathf.CeilToInt((float)maxParticleCount / threadsPerGroup);
            groups = Mathf.Max(groups, 1);
            computeShader.Dispatch(kernelHandle, groups, 1, 1);

            if (particleStatic.renderMode == (uint)RenderMode.AppendBufferMode)
            {
                //更新粒子实例数量
                GraphicsBuffer.CopyCount(aliveBuffer, commandBuf, sizeof(uint));
            }

            //material Set
            materialPropertyBlock.SetBuffer("particles",
                particleStatic.renderMode == (uint)RenderMode.AppendBufferMode ? aliveBuffer : particleBuffer);
            rp.matProps = materialPropertyBlock;


            Graphics.RenderPrimitivesIndirect(rp, MeshTopology.Triangles, commandBuf, commandCount);
        }

        private void OnDestroy()
        {
            Release();
        }

        public void Release()
        {
            particleBuffer?.Release();
            particleBuffer = null;
            aliveBuffer?.Release();
            aliveBuffer = null;
            commandBuf?.Release();
            commandBuf = null;
            headIndexBuffer?.Release();
            headIndexBuffer = null;
            velocityCurveBuffer?.Release();
            velocityCurveBuffer = null;
            particleStaticBuffer?.Release();
            particleStaticBuffer = null;
            emptyBuffer?.Release();
            emptyBuffer = null;
            initializeParametersBuffer?.Release();
            initializeParametersBuffer = null;
            addParticlePositionsBuffer?.Release();
            addParticlePositionsBuffer = null;

            rp.matProps = null;

            if (computeShader != null)
            {
                Destroy(computeShader);
                computeShader = null;
            }
        }
    }
}