using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class BillboardText : MonoBehaviour
{
    private Camera m_Camera;
    void Start() {
        m_Camera  = Camera.main;
    }

    void Update() {
        if (m_Camera) {
            transform.LookAt(m_Camera.transform);
        }
    }
}