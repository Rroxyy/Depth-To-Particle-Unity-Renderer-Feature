using System;
using System.Collections;
using System.Collections.Generic;
using AkkoParticle;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;


public class ParticleManager2 : MonoBehaviour
{
    [Header("Base")]
    [SerializeField] private Dictionary<ParticleParameters, ParticleBufferController> dict;

    [Header("Test")] 
    public bool testEnabled = false;
    public int maxParticleCount = 40;
    public Vector3 center;
    public float size;




    [Header("Default Settings")] 
    [SerializeField]
    public ParticleParameters defaultParticleParameters;


    private List<ParticleParameters> removeList;
    public static ParticleManager2 instance;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(instance);
        }

        instance = this;

        testEnabled = false;
        dict = new Dictionary<ParticleParameters, ParticleBufferController>();
        removeList = new List<ParticleParameters>();
    }


    private void Update()
    {
        if (testEnabled)
        {
            testEnabled = false;
            Vector3[] positions = new Vector3[maxParticleCount];
            for (int i = 0; i < maxParticleCount; i++)
            {
                positions[i] = center + new Vector3(
                    Random.Range(-size, size),
                    Random.Range(-size, size),
                    Random.Range(-size, size)
                );
            }

            // Particle[] particles = defaultParticleParameters.GetParticles(positions);
            ParticleBufferController particleBufferController = GetParticleBufferController(defaultParticleParameters);
            particleBufferController.AddParticles(positions);
        }
        UpdateParticleController();

    }

   


    #region Particle Buffer List Control

    public ParticleBufferController GetParticleBufferController(ParticleParameters particleParameters,bool enable = true)
    {
        ParticleBufferController pbc;
        if (!dict.ContainsKey(particleParameters))
        {
            pbc = gameObject.AddComponent<ParticleBufferController>();
            pbc.Initialize(particleParameters.material, particleParameters.mesh,
                particleParameters.CS,this,particleParameters);
            
            pbc.enableParticleSystem = enable;//自动启用
            dict.Add(particleParameters, pbc);
        }

        pbc = dict[particleParameters];
        pbc.controllerMaxLiveTime = particleParameters.enableRandomLifeTime
            ? particleParameters.lifeTime + particleParameters.randomLifeTimeRange
            : particleParameters.lifeTime;
        pbc.controllerMaxLiveTime = Mathf.Max(pbc.controllerMaxLiveTime, particleParameters.minContinueTime);
        pbc.controllerMaxLiveTime += 1.0f;//冗余
        return pbc;
    }

    public void RemoveParticleBuffer(ParticleParameters particleParameters)
    {
        removeList.Add(particleParameters);
    }

    private void UpdateParticleController()
    {
        if(dict.Count == 0)
            return;
        var keys = dict.Keys;
        foreach (var key in keys)
        {
            ParticleBufferController pbc=dict[key];
            pbc.UpdateInManager(Time.deltaTime);
        }

        if (removeList.Count > 0)
        {
            foreach (var pp in removeList)
            {
                dict.Remove(pp);
            }
        }
        removeList.Clear();
        
    }
    

    #endregion
}