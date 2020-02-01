using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoatSetup : MonoBehaviour
{
    void Start() {
        StartCoroutine(Setup());
    }

    IEnumerator Setup() {
        yield return new WaitForSeconds(3);
        var position = transform.position;
        position = new Vector3(position.x, 4f, position.z);
        transform.position = position;
        yield return new WaitForSeconds(2);
        position = new Vector3(position.x, 1f, position.z);
        transform.position = position;
        GetComponent<Rigidbody>().isKinematic = false;
    }
}
