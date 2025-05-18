using System;
using Unity.VisualScripting;
using UnityEngine;

namespace AkkoParticle
{
    public class MeshPlugins:MonoBehaviour
    {
        public Mesh mesh;

        [Header("Show Vertex")]
        public bool showVertex=false;
        public float radius=0.1f;

        [Header("Info")]
        public bool getInfo=false;
        public int vertexCount=0;
        public int triangleCount=0;


        private void OnValidate()
        {
            if (getInfo && mesh != null)
            {
                getInfo = false;

                var indexBuffer = mesh.GetIndexBuffer();
                int indexCount = (int)mesh.GetIndexCount(0); // 第 0 个 submesh

                Debug.Log($"Index Count: {indexCount}");
                Debug.Log(mesh.indexFormat);
                if (mesh.indexFormat == UnityEngine.Rendering.IndexFormat.UInt16)
                {
                    ushort[] indices = new ushort[indexCount];
                    indexBuffer.GetData(indices);

                    for (int i = 0; i < indices.Length; i += 3)
                    {
                        if (i + 2 < indices.Length)
                            Debug.Log($"Triangle[{i / 3}]: {indices[i]}, {indices[i + 1]}, {indices[i + 2]}");
                    }
                }
                else if (mesh.indexFormat == UnityEngine.Rendering.IndexFormat.UInt32)
                {
                    int[] indices = new int[indexCount];
                    indexBuffer.GetData(indices);

                    for (int i = 0; i < indices.Length; i += 3)
                    {
                        if (i + 2 < indices.Length)
                            Debug.Log($"Triangle[{i / 3}]: {indices[i]}, {indices[i + 1]}, {indices[i + 2]}");
                    }
                }

                Vector3[] vertices = mesh.vertices;
                for (int i = 0; i < vertices.Length; i++)
                {
                    Debug.Log($"Vertex: {vertices[i]}");
                }
                
                indexBuffer.Release();
                indexBuffer = null;
            }
        }

        private void OnDrawGizmos()
        {
            if(!showVertex)
                return;
            
            Vector3[] vertices = mesh.vertices;

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 worldPos = transform.TransformPoint(vertices[i]);
                
                Gizmos.color=Color.red;
                Gizmos.DrawSphere(worldPos, radius);
            }
        }
    }
}