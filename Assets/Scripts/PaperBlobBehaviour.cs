using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(MeshRenderer))]
public class PaperBlobBehaviour : MonoBehaviour
{
    public float minDuration;
    public float maxDuration;
    public Color[] colorStates;
    public bool immune;
    public GameObject particlesOnDestroyPrefab;

    private int currentColorState = -1;
    private float m_RemainingLife;
    private MeshRenderer m_MeshRenderer;
    private static readonly int Color = Shader.PropertyToID("_Color");

    private bool IsOnWater() {
        return true;
    }

    public float RemainingLife {
        get { return m_RemainingLife; }
        set { m_RemainingLife = value; }
    }

    void Start() {
        m_MeshRenderer = GetComponent<MeshRenderer>();
        m_RemainingLife = Random.Range(minDuration, maxDuration);
    }

    void Update() {
        if (!immune && IsOnWater()) {
            m_RemainingLife -= Time.deltaTime;
        }

        if (m_RemainingLife < 0) {
            var particles = Instantiate(particlesOnDestroyPrefab, transform.position,
                particlesOnDestroyPrefab.transform.rotation);
            var material = particles.GetComponent<ParticleSystemRenderer>().material;
            material.color = m_MeshRenderer.material.color;
            material.mainTexture = m_MeshRenderer.material.mainTexture;
            var colliders = Physics.OverlapSphere(transform.position, 2.0f);
            foreach (var collider in colliders) {
                var paperBlob = collider.GetComponent<PaperBlobBehaviour>();
                if (paperBlob && !paperBlob.immune && paperBlob.RemainingLife + 1f > colorStates.Length * 10f) {
                    var distanceSqr = (collider.transform.position - transform.position).sqrMagnitude;
                    var lifeBefore = paperBlob.RemainingLife;
                    paperBlob.RemainingLife = Mathf.Max(
                        paperBlob.RemainingLife - (8f - distanceSqr) * 20f,
                        colorStates.Length * 10f 
                    );
                }
            }

            Destroy(gameObject);
            gameObject.SetActive(false);
        }

        int desiredState = Mathf.Clamp(Mathf.FloorToInt(m_RemainingLife / 10f), 0, colorStates.Length - 1);
        if (currentColorState != desiredState) {
            m_MeshRenderer.material.SetColor(Color, colorStates[desiredState]);
            currentColorState = desiredState;
        }
    }
}