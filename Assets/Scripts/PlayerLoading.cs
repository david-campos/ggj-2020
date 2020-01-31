using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityStandardAssets.Effects;

public class PlayerLoading : MonoBehaviour
{
    public int maxLoad;
    public Color loadedColor;
    public Color notLoadedColor;
    public Transform aimingOrigin;
    public GameObject aimingGhost;
    public float aimingAngle;
    public float aimDistance;
    public float shootingCooldown;
    public GameObject shootPrefab;

    private int m_LoadAmount = 0;
    private SkinnedMeshRenderer m_GlassesRenderer;
    private static readonly int Albedo = Shader.PropertyToID("_Color");
    private float m_NextShoot = 0;

    public int LoadAmount {
        get { return m_LoadAmount; }
    }

    public void Reload() {
        m_LoadAmount = maxLoad;
    }

    void Start() {
        m_GlassesRenderer = transform.GetChild(1).GetComponent<SkinnedMeshRenderer>();
        aimingOrigin.rotation = Quaternion.Euler(aimingAngle, 0, 0);
    }

    void Update() {
        if (m_LoadAmount > 0) {
            if (!m_GlassesRenderer.material.GetColor(Albedo).Equals(loadedColor)) {
                m_GlassesRenderer.material.SetColor(Albedo, loadedColor);
            }

#if UNITY_EDITOR
			// helper to visualise the ground check ray in the scene view
            var position = aimingOrigin.position;
            Debug.DrawLine(position, position + aimingOrigin.TransformDirection(Vector3.forward)* aimDistance);
#endif
            RaycastHit hitInfo;
            aimingGhost.SetActive(Physics.Raycast(aimingOrigin.position,
                aimingOrigin.TransformDirection(Vector3.forward), out hitInfo, aimDistance));
            if (aimingGhost.activeSelf) {
                aimingGhost.transform.position = hitInfo.point;
                float rotation = Time.deltaTime * 45;
                aimingGhost.transform.Rotate(rotation * 0.5f, rotation * 1.5f, rotation);
                if (Input.GetButton("FireB")) {
                    Fire();
                }
            }
        } else {
            aimingGhost.SetActive(false);
            if (!m_GlassesRenderer.material.GetColor(Albedo).Equals(notLoadedColor)) {
                m_GlassesRenderer.material.SetColor(Albedo, notLoadedColor);
            }
        }
    }

    private void Fire() {
        if (Time.time > m_NextShoot) {
            Instantiate(shootPrefab, aimingGhost.transform.position, aimingGhost.transform.rotation);
            m_LoadAmount -= 1;
            m_NextShoot = Time.time + shootingCooldown;
        }
    }
}