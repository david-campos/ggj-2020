using System;
using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ReloadingPoint : MonoBehaviour
{
    public LoadType loadType;
    public float activationDuration;

    private ParticleSystem m_ParticleSystem;
    private float m_LastReloadTime;
    private HashSet<PlayerLoading> m_PlayersInside;

    private void Start() {
        m_PlayersInside = new HashSet<PlayerLoading>();
        m_ParticleSystem = GetComponent<ParticleSystem>();
        m_LastReloadTime = Time.time - activationDuration;
    }

    private void Update() {
        foreach (var player in m_PlayersInside) {
            if (Input.GetButton("Reload" + player.Player)) {
                player.Reload(loadType);
                m_LastReloadTime = Time.time;
            }
        }

        var activatedFor = Time.time - m_LastReloadTime;
        var activationFraction = Mathf.Clamp01((activationDuration - activatedFor) / activationDuration);
        var particleSystemEmission = m_ParticleSystem.emission;
        particleSystemEmission.rateOverTimeMultiplier = 20 + activationFraction * 500;
    }

    private void OnDestroy() {
        foreach (var playerLoading in m_PlayersInside) {
            playerLoading.CanReload = false;
        }
        m_PlayersInside.Clear();
    }

    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.CompareTag("Player")) {
            var player = other.GetComponent<PlayerLoading>();
            if (player) {
                m_PlayersInside.Add(player);
                player.CanReload = true;
            }
        }
    }

    private void OnTriggerExit(Collider other) {
        var player = other.GetComponent<PlayerLoading>();
        if (player) {
            m_PlayersInside.Remove(player);
            player.CanReload = false;
        }
    }
}