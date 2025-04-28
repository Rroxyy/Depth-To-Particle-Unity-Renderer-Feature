using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleManager : MonoBehaviour
{
    [Header("Base Settings")] 
    public GameObject particlePrefab;
    
    private List<ParticlesControl> particles;


    public static ParticleManager instance;

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
        GameObject particleGameObject = Instantiate(particlePrefab,transform);
        ParticlesControl pc=particleGameObject.GetComponent<ParticlesControl>();
        
        pc.SetupParticle(positions,this);
        particles.Add(pc);
    }
    
    
    

    public void DestoryParticle(ParticlesControl particle)
    {
        particles.Remove(particle);
    }
}