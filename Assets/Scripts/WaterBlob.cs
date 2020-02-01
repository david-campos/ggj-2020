using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterBlob : MonoBehaviour
{
    //static float forceRadius = 3f;
    //static float waterforce = 300f;

    // Start is called before the first frame update
    void Awake()
    {
        //StartCoroutine(UpdateWaterPhysics());
    }

    // Update is called once per frame
    //IEnumerator UpdateWaterPhysics()
    //{
    //    while(gameObject && enabled && gameObject.activeInHierarchy)
    //    {
    //        float customDt = Random.Range(0.05f, 0.08f);
    //        attractNearbyWater(customDt);
    //        yield return new WaitForSeconds(customDt);
    //    }
    //}

    //void attractNearbyWater(float customDt)
    //{
    //    foreach (var nearbyWater in GetComponent<WaterToucher>().NearbyWaterBlobs())
    //    {
    //        Vector3 deltaPos = nearbyWater.transform.position - transform.position;
    //        float distance = deltaPos.magnitude;
    //        Vector3 direction = -deltaPos / distance;
    //        float forceAmount01 = Mathf.Clamp01(Mathf.InverseLerp(forceRadius, 0, distance / 2));
    //        Vector3 force = (direction * Mathf.Pow(forceAmount01, 0.4f) * waterforce /*+ velocityForce*/) * customDt;

    //        var otherBody = nearbyWater.GetComponent<Rigidbody>();
    //        otherBody.AddForce(-force);
    //        GetComponent<Rigidbody>().AddForce(force);
    //    }
    //}
}
