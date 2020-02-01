//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class WaterToucher : MonoBehaviour
//{
//    public float radius = 5.0f;
//    public float cacheRadius = 10.0f;

//    [SerializeField] WaterBlob[] cachedNearby = new WaterBlob[20];
//    [SerializeField] int cachedNearbyCount = 0;

//    // Start is called before the first frame update
//    void Awake()
//    {
//        //StartCoroutine(UpdateCache());
//    }

//    public IEnumerable<WaterBlob> NearbyWaterBlobs()
//    {
//        for (int i = 0; i < cachedNearbyCount; i++)
//        {
//            if (cachedNearby[i] == null)
//                continue;

//            if((cachedNearby[i].transform.position - transform.position).sqrMagnitude < radius * radius) {
//                yield return cachedNearby[i];
//            }
//        }
//    }

//    IEnumerator UpdateCache()
//    {
//        while (enabled && gameObject.activeInHierarchy)
//        {
//            cachedNearbyCount = 0;
//            if (WaterBlob.allWater != null)
//            {
//                foreach (var otherBlob in WaterBlob.allWater)
//                {
//                    if (otherBlob.gameObject == gameObject)
//                        continue;

//                    if (otherBlob == null)
//                        continue;

//                    if ((otherBlob.transform.position - transform.position).sqrMagnitude < cacheRadius * cacheRadius)
//                    {
//                        cachedNearby[cachedNearbyCount++] = otherBlob;
//                        if (cachedNearbyCount >= cachedNearby.Length - 1)
//                            break;
//                    }
//                }
//            }
//            yield return new WaitForSeconds(Random.Range(0.3f, 0.4f));
//        }
//    }
//}
