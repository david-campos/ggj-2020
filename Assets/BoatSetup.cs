using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoatSetup : MonoBehaviour
{
    void Start() {
        StartCoroutine(Setup());
    }

    IEnumerator Setup() {
        var rigidbody = GetComponent<Rigidbody>();
        var drag = rigidbody.drag;
        rigidbody.drag = 10f;
        yield return new WaitForSeconds(10);
        rigidbody.drag = drag;
    }
}
