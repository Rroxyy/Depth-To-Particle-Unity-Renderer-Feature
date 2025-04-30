using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;


public class ParticleManager : MonoBehaviour
{
    [Header("Base Settings")] public GameObject particlePrefab;

    private List<ParticlesControl> particles;


    public static ParticleManager instance;

    [Header("Test Particle Control")] [SerializeField]
    private bool testEnable = false;

    [SerializeField] int count = 100;

    [SerializeField] private float round = 0.5f;
    [Space(10)] [SerializeField] private float interval = 1f;
    [SerializeField] private float timer;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(instance);
        }

        instance = this;
    }

    private void Start()
    {
        particles = new List<ParticlesControl>();
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (testEnable && timer > interval)
        {
            timer = 0;
            Vector3[] positions = new Vector3[Mathf.Max(0, count)];
            for (int i = 0; i < count; i++)
            {
                positions[i] = new Vector3(
                                   Random.Range(-round, round),
                                   Random.Range(-round, round),
                                   Random.Range(-round, round))
                               + transform.position;
            }

            AddParticle(positions);
        }
    }


    private void OnDestroy()
    {
        // 创建一个临时列表，用于保存需要释放的粒子
        List<ParticlesControl> particlesToRelease = new List<ParticlesControl>();

        foreach (var particle in particles)
        {
            if (particle != null)
                particlesToRelease.Add(particle); // 添加到临时列表
        }

        // 释放粒子
        foreach (var particle in particlesToRelease)
        {
            particle.Release();
        }

        // 如果需要清除 particles 集合，可以在这里执行
        particles.Clear();
    }


    public void AddParticle(Vector3[] positions)
    {
        if (positions == null || positions.Length <= 0)
            return;
        GameObject particleGameObject = Instantiate(particlePrefab, transform);
        ParticlesControl pc = particleGameObject.GetComponent<ParticlesControl>();

        pc.SetupParticle(positions, this);
        particles.Add(pc);
    }


    public void DestoryParticle(ParticlesControl particle)
    {
        particles.Remove(particle);
    }
}