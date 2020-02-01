using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterGrid : MonoBehaviour
{
    public float boundaryStrength = 10;
    public float boundaryStrengthBottom = 20;
    public float gridSpacing = 0.8f;
    public Vector3Int gridSize;
    public WaterBlob blobPrefab;

    List<WaterBlob> blobs = new List<WaterBlob>();
    public List<WaterBlob> WaterBlobs { get { return blobs; } }

    private void Awake()
    {
        if (blobPrefab == null)
        {
            return;
        }
        
        blobs = new List<WaterBlob>();
        
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                for (int z = 0; z < gridSize.z; z++)
                {
                    WaterBlob blob = Instantiate(blobPrefab, transform);
                    blob.transform.localPosition = gridSpacing * new Vector3(
                        x - gridSize.x * 0.5f,
                        y - gridSize.y * 0.5f,
                        z - gridSize.z * 0.5f
                    ) + Random.insideUnitSphere * 0.1f;
                    blob.transform.localRotation = Quaternion.identity;

                    //if (x == 0 || x == gridSize.x - 1 || 
                    //    y == 0 ||
                    //    z == 0 || z == gridSize.z - 1)
                    blobs.Add(blob);
                }
            }
        }
    }

    private void FixedUpdate()
    {
        //foreach(var blob in blobs)
        //{
        //    // Contain above Y bottom
        //    float outsideY = -gridSize.y * gridSpacing * 0.5f - blob.transform.localPosition.y;
        //    if (outsideY > 0)
        //    {
        //        if (outsideY > 3)
        //            outsideY = 3;
        //        blob.AddForce(transform.up * boundaryStrengthBottom * outsideY);
        //    }

        //    // Contain inside X range
        //    float outsideLeft = -gridSize.x * gridSpacing * 0.5f - blob.transform.localPosition.x;
        //    if (outsideLeft > 0)
        //    {
        //        if (outsideLeft > 3)
        //            outsideLeft = 3;
        //        blob.AddForce(transform.right * boundaryStrength * outsideLeft);
        //    }
        //    float outsideRight = blob.transform.localPosition.x - gridSize.x * gridSpacing * 0.5f;
        //    if (outsideRight > 0)
        //    {
        //        if (outsideRight > 3)
        //            outsideRight = 3;
        //        blob.AddForce(-transform.right * boundaryStrength * outsideRight);
        //    }

        //    // Contain inside Z range
        //    float outsideBack = -gridSize.z * gridSpacing * 0.5f - blob.transform.localPosition.z;
        //    if (outsideBack > 0)
        //    {
        //        if (outsideBack > 3)
        //            outsideBack = 3;
        //        blob.AddForce(transform.forward * boundaryStrength * outsideBack);
        //    }
        //    float outsideFront = blob.transform.localPosition.z - gridSize.z * gridSpacing * 0.5f;
        //    if (outsideFront > 0)
        //    {
        //        if (outsideFront > 3)
        //            outsideFront = 3;
        //        blob.AddForce(-transform.forward * boundaryStrength * outsideFront);
        //    }
        //}
    }
}
