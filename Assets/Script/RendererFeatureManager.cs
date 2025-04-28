using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RendererFeatureManager : MonoBehaviour
{
    public static RendererFeatureManager instance;

    public bool enableDissolveRendererFeature = false;

    public RTToParticlePositions rtToParticlePositions;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(this);
        }

        instance = this;
    }
}