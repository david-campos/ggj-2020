using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class GlueAimingSphere : MonoBehaviour
{
    private HashSet<GameObject> m_CollidingPapers = new HashSet<GameObject>();
    private MeshRenderer m_MeshRenderer;
    private Color m_OriginalColor;
    private static readonly int Color = Shader.PropertyToID("_Color");

    public HashSet<GameObject> CollidingPapers => m_CollidingPapers;

    /**
     * Set the alpha (between 0.f and 1.f, precision of 0.01)
     */
    public void SetAlpha(float value) {
        if (Math.Abs(m_MeshRenderer.material.color.a - value) > 0.01) {
            Color newColor = m_OriginalColor;
            newColor.a = value;
            m_MeshRenderer.material.SetColor(Color, newColor);
        }
    }
    
    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Paper")) {
            m_CollidingPapers.Add(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other) {
        if (other.CompareTag("Paper")) {
            m_CollidingPapers.Remove(other.gameObject);
        }
    }

    private void OnDisable()
    {
        m_CollidingPapers.Clear();
    }

    private void Start() {
        m_MeshRenderer = GetComponent<MeshRenderer>();
        m_OriginalColor = m_MeshRenderer.material.color;
    }
}
