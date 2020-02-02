using System;
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
    public GameObject floatingText;

    private float m_LoadAmount = 0;
    private SkinnedMeshRenderer m_GlassesRenderer;
    private MeshRenderer m_AimingPaperRenderer;
    private static readonly int Albedo = Shader.PropertyToID("_Color");
    private float m_NextShoot = 0;
    private LoadType m_LoadType;
    private string m_FireButton;
    private GlueAimingSphere m_GlueAimingSphere;
    private GameObject m_Boat;
    private string m_Player;

    public string Player => m_Player;

    public void Reload(LoadType loadType) {
        m_LoadAmount = maxLoad;
        m_LoadType = loadType;
    }

    public bool CanReload {
        set {
            if (value) {
                floatingText.GetComponent<TextMesh>().text = "B to reload";
            }
            floatingText.SetActive(value);
        }
    }

    void Start() {
        m_GlassesRenderer = transform.GetChild(1).GetComponent<SkinnedMeshRenderer>();
        m_AimingPaperRenderer = aimingGhostPaper.GetComponent<MeshRenderer>();
        aimingOrigin.rotation = Quaternion.Euler(aimingAngle, 0, 0);
        m_Player = GetComponent<ThirdPersonUserControl>().player;
        m_FireButton = "Fire" + m_Player;
        m_GlueAimingSphere = aimingGhostGlue.GetComponent<GlueAimingSphere>();
        m_Boat = GameObject.Find("Boat");
        if (!m_Boat) {
            Debug.LogError("No game object with name 'Boat' found.");
        }
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
            aimingGhostPaper.transform.position = hitInfo.point + hitInfo.normal * 0.1f;
            var gluableBehaviour = hitInfo.collider.gameObject.GetComponent<GluableBehaviour>();
            float rotation = Time.deltaTime * 45;
            aimingGhostPaper.transform.Rotate(rotation * 0.5f, rotation * 1.5f, rotation);
            if (!gluableBehaviour || !gluableBehaviour.Glued) {
                SetAimingPaperAlpha(0.05f);
            } else {
                SetAimingPaperAlpha(0.5f);
                if (Input.GetButton(m_FireButton)) {
                    FirePaper();
                }
            }
        }
    }

    private void SetAimingPaperAlpha(float alpha) {
        var materialColor = m_AimingPaperRenderer.material.color;
        if (Math.Abs(materialColor.a - alpha) > 0.01) {
            materialColor.a = alpha;
            m_AimingPaperRenderer.material.color = materialColor;
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
            m_GlueAimingSphere.SetAlpha(0.3f * m_LoadAmount / maxLoad);
            if (Input.GetButton(m_FireButton)) {
                FireGlue();
            }
        }
    }

    private void FirePaper() {
        if (Time.time > m_NextShoot) {
            Instantiate(shootPrefab, aimingGhostPaper.transform.position, aimingGhostPaper.transform.rotation,
                m_Boat.transform);
            m_LoadAmount -= 1;
            m_NextShoot = Time.time + shootingCooldown;
        }
    }

    private void FireGlue() {
        if (Time.time > m_NextShoot) {
            foreach (var paper in m_GlueAimingSphere.CollidingPapers) {
                if (paper) {
                    var behaviour = paper.GetComponent<GluableBehaviour>();
                    if (!behaviour.Glued) {
                        behaviour.Glue();
                        m_LoadAmount -= 0.1f;
                    }
                }
            }
        }
    }
}