using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DissolveCtrl : MonoBehaviour
{
    public Material dissolveMat;
    
    [Header("Settings")]
    public bool startDissolve=false;
    
    public float dissolveTime=2f;
    public float timer = 0f;
    
    public Transform topTransform;
    public Transform bottomTransform;
    
    void Start()
    {
        timer=dissolveTime;
    }
    
    void Update()
    {
        if (startDissolve)
        {
            timer-=Time.deltaTime;
            if (timer < 0)
            {
                timer=dissolveTime;
            }
        
            float t=timer/dissolveTime;
            float y=bottomTransform.transform.position.y+(topTransform.position.y-bottomTransform.position.y)*t;
        
            dissolveMat.SetFloat("_DissolveHeight",y);
        }
        
    }
}
