using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class GluableBehaviour : MonoBehaviour
{
    public Texture gluedTexture;
    
    private bool glued;
    private MeshRenderer m_MeshRenderer;
    private static readonly int MainTex = Shader.PropertyToID("_MainTex");
    
    public void Glue() {
        glued = true;
    }

    public bool Glued => glued;

    void Start() {
        m_MeshRenderer = GetComponent<MeshRenderer>();
        glued = false;
    }

    void Update() {
        if (glued && m_MeshRenderer.material.mainTexture != gluedTexture) {
            m_MeshRenderer.material.SetTexture(MainTex, gluedTexture);
        }
    }
}