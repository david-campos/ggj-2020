using System.Collections;
using System.Collections.Generic;
using DefaultNamespace;
using UnityEditor;
using UnityEngine;
using UnityStandardAssets.Characters.ThirdPerson;
using UnityStandardAssets.Effects;

[RequireComponent(typeof(ThirdPersonUserControl))]
public class PlayerLoading : MonoBehaviour
{
    public int maxLoad;
    public Color glueColor;
    public Color paperColor;
    public Color notLoadedColor;
    public Transform aimingOrigin;
    public GameObject aimingGhostPaper;
    public GameObject aimingGhostGlue;
    public float aimingAngle;
    public float aimDistance;
    public float shootingCooldown;
    public GameObject shootPrefab;

    private int m_LoadAmount = 0;
    private SkinnedMeshRenderer m_GlassesRenderer;
    private static readonly int Albedo = Shader.PropertyToID("_Color");
    private float m_NextShoot = 0;
    private LoadType m_LoadType;
    private string m_FireButton;

    public int LoadAmount {
        get { return m_LoadAmount; }
    }

    public void Reload(LoadType loadType) {
        m_LoadAmount = maxLoad;
        m_LoadType = loadType;
    }

    void Start() {
        m_GlassesRenderer = transform.GetChild(1).GetComponent<SkinnedMeshRenderer>();
        aimingOrigin.rotation = Quaternion.Euler(aimingAngle, 0, 0);
        m_FireButton = "Fire" + GetComponent<ThirdPersonUserControl>().player;
    }

    void Update() {
        if (m_LoadAmount > 0) {
#if UNITY_EDITOR
            var position = aimingOrigin.position;
            Debug.DrawLine(position, position + aimingOrigin.TransformDirection(Vector3.forward) * aimDistance);
#endif
            RaycastHit hitInfo;
            bool hit = Physics.Raycast(aimingOrigin.position,
                aimingOrigin.TransformDirection(Vector3.forward), out hitInfo, aimDistance);
            
            if (m_LoadType == LoadType.PAPER) PaperAming(hit, hitInfo);
            else GlueAming(hit, hitInfo);
        } else {
            aimingGhostPaper.SetActive(false);
            aimingGhostGlue.SetActive(false);
            if (!m_GlassesRenderer.material.GetColor(Albedo).Equals(notLoadedColor)) {
                m_GlassesRenderer.material.SetColor(Albedo, notLoadedColor);
            }
        }
    }

    private void PaperAming(bool hit, RaycastHit hitInfo) {
        if (!m_GlassesRenderer.material.GetColor(Albedo).Equals(paperColor)) {
            m_GlassesRenderer.material.SetColor(Albedo, paperColor);
        }

        aimingGhostGlue.SetActive(false);
        aimingGhostPaper.SetActive(hit);
        if (hit) {
            aimingGhostPaper.transform.position = hitInfo.point;
            float rotation = Time.deltaTime * 45;
            aimingGhostPaper.transform.Rotate(rotation * 0.5f, rotation * 1.5f, rotation);
            if (Input.GetButton(m_FireButton)) {
                Fire();
            }
        }
    }

    private void GlueAming(bool hit, RaycastHit hitInfo) {
        if (!m_GlassesRenderer.material.GetColor(Albedo).Equals(glueColor)) {
            m_GlassesRenderer.material.SetColor(Albedo, glueColor);
        }
        
        aimingGhostPaper.SetActive(false);
        aimingGhostGlue.SetActive(hit);
        if (hit) {
            aimingGhostGlue.transform.position = hitInfo.point;
            if (Input.GetButton(m_FireButton)) {
                Fire();
            }
        }
    }

    private void Fire() {
        if (Time.time > m_NextShoot) {
            Instantiate(shootPrefab, aimingGhostPaper.transform.position, aimingGhostPaper.transform.rotation);
            m_LoadAmount -= 1;
            m_NextShoot = Time.time + shootingCooldown;
        }
    }
}