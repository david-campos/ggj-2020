using UnityEngine;
using UnityEngine.UIElements;

public class PaperBlobBehaviour : MonoBehaviour
{
    public float minDuration;
    public float maxDuration;
    public float inmuneProbability;
    public Color[] colorStates;
    private int currentColorState = -1;
    private float m_RemainingLife;
    private MeshRenderer m_MeshRenderer;
    private static readonly int Color = Shader.PropertyToID("_Color");
    private bool inmune;

    private bool IsOnWater() {
        return true;
    }

    void Start() {
        m_MeshRenderer = GetComponent<MeshRenderer>();
        m_RemainingLife = Random.Range(minDuration, maxDuration);
        inmune = Random.value < inmuneProbability;
    }

    void Update() {
        if (!inmune && IsOnWater()) {
            m_RemainingLife -= Time.deltaTime;
        }

        if (m_RemainingLife < 0) {
            Destroy(gameObject);
        }
        
        int desiredState = Mathf.Clamp(Mathf.FloorToInt(m_RemainingLife), 0, colorStates.Length - 1);
        if (currentColorState != desiredState) {
            m_MeshRenderer.material.SetColor(Color, colorStates[desiredState]);
            currentColorState = desiredState;
        }
    }
}