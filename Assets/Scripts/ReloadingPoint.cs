using System;
using DefaultNamespace;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ReloadingPoint : MonoBehaviour
{
    public LoadType loadType;
    public float activationDuration;

    private ParticleSystem m_ParticleSystem;
    private float m_LastEnterTime;

    private void Start() {
        m_ParticleSystem = GetComponent<ParticleSystem>();
        m_LastEnterTime = Time.time - activationDuration;
    }

    private void Update() {
        var activatedFor = Time.time - m_LastEnterTime;
        var activationFraction = Mathf.Clamp01((activationDuration - activatedFor) / activationDuration);
        var particleSystemEmission = m_ParticleSystem.emission;
        particleSystemEmission.rateOverTimeMultiplier = 20 + activationFraction * 500;
    }

    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.CompareTag("Player")) {
            m_LastEnterTime = Time.time;
            other.gameObject.GetComponent<PlayerLoading>().Reload(loadType);
        }
    }
}