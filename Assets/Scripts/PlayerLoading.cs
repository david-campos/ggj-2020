using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PlayerLoading : MonoBehaviour
{
    public int maxLoad;
    public Color loadedColor;
    public Color notLoadedColor;
    
    private int loadAmount = 0;
    private SkinnedMeshRenderer glassesRenderer;
    private static readonly int Albedo = Shader.PropertyToID("_Color");

    public int LoadAmount {
        get { return loadAmount; }
        set { loadAmount = value; }
    }

    public void Reload() {
        loadAmount = maxLoad;
    }
    
    void Start() {
        glassesRenderer = transform.GetChild(1).GetComponent<SkinnedMeshRenderer>();
    }

    void Update() {
        if (loadAmount > 0) {
            if (!glassesRenderer.material.GetColor(Albedo).Equals(loadedColor)) {
                glassesRenderer.material.SetColor(Albedo, loadedColor);
            }
        } else {
            if (!glassesRenderer.material.GetColor(Albedo).Equals(notLoadedColor)) {
                glassesRenderer.material.SetColor(Albedo, notLoadedColor);
            }
        }
    }
}