using AkkoParticle;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ParticleParameters))]
public class ParticleParametersEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 保留原有的 Inspector
        DrawDefaultInspector();

        ParticleParameters script = (ParticleParameters)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("粒子初始化", EditorStyles.boldLabel);

        if (GUILayout.Button("Apply"))
        {
            script.Preprocess();
        }
    }
}