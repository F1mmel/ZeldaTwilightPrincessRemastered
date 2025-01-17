using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BMDRenderer : MonoBehaviour
{
    public UnityEngine.Texture Texture;
    public Vector3[] Vertices;
    
    void Start()
    {
        
    }

    void Update()
    {
        
    }
    
    void OnRenderObject()
    {
        if(Vertices == null) return;
        if(Vertices.Length == 0) return;
        
        Material mat = new Material(Shader.Find("Hidden/Internal-Colored"));
        mat.SetTexture("_MainTex", Texture);
        mat.SetPass(0);

        GL.PushMatrix();
        GL.MultMatrix(transform.localToWorldMatrix);

        GL.Begin(GL.TRIANGLES);
        
            foreach (Vector3 v in Vertices)
            {
                GL.Vertex(v);
            }

            GL.End();

        GL.PopMatrix();
    }
}
