using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class OptimizedWater : MonoBehaviour
{
    [Min(0)]
    public float gravity = 8;
    [Min(0.1f)]
    public float forceRadius = 3f;
    [Min(0)]
    public float waterForce = 20f;
    [Min(0)]
    public float waterDamping = 0.2f;
    [Min(0)]
    public float boundaryForce = 0.2f;

    private List<WaterBlob> waterBlobs;

    NativeArray<int> sortedIndexes;
    NativeArray<float3> velocities, positions, newVelocities, newPositions;

    private void Start()
    {
        waterBlobs = GetComponent<WaterGrid>().WaterBlobs;

        sortedIndexes = new NativeArray<int>(waterBlobs.Count, Allocator.Persistent);
        velocities = new NativeArray<float3>(waterBlobs.Count, Allocator.Persistent);
        positions = new NativeArray<float3>(waterBlobs.Count, Allocator.Persistent);
        newVelocities = new NativeArray<float3>(waterBlobs.Count, Allocator.Persistent);
        newPositions = new NativeArray<float3>(waterBlobs.Count, Allocator.Persistent);

        for (int i = 0; i < positions.Length; i++)
        {
            sortedIndexes[i] = i;
            positions[i] = new float3(waterBlobs[i].transform.position);
        }
    }

    void FixedUpdate()
    {
        positions[0] = waterBlobs[0].transform.position;

        var sortjob = new SortJob
        {
            positions = positions,
            sortedIndexes = sortedIndexes
        };
        sortjob.Schedule().Complete();
        var gridSize = (Vector3)GetComponent<WaterGrid>().gridSize * GetComponent<WaterGrid>().gridSpacing;
        var waterjob = new WaterJob
        {
            dt = Time.fixedDeltaTime,
            gravity = gravity,
            forceRadius = forceRadius,
            waterForce = waterForce,
            waterDamping = waterDamping,
            boundaryForce = boundaryForce,
            worldPos = transform.position,
            gridSizeX = gridSize.x,
            gridSizeY = gridSize.y,
            gridSizeZ = gridSize.z,

            sortedIndexes = sortedIndexes,
            velocities = velocities,
            positions = positions,
            newVelocities = newVelocities,
            newPositions = newPositions,
        };
        waterjob.Schedule(sortedIndexes.Length, 256).Complete();

        for (int i = 0; i < waterBlobs.Count; i++)
        {
            waterBlobs[i].transform.position = newPositions[i];
        }

        
        //Swap buffers for next frame:
        var tmpPositions = newPositions;
        newPositions = positions;
        positions = tmpPositions;

        var tmpVelocities = newVelocities;
        newVelocities = velocities;
        velocities = tmpVelocities;
    }

    private void OnDestroy()
    {
        sortedIndexes.Dispose();
        velocities.Dispose();
        positions.Dispose();
        newVelocities.Dispose();
        newPositions.Dispose();
    }

    [BurstCompile(CompileSynchronously = true)]
    private struct SortJob : IJob
    {
        [ReadOnly]
        public NativeArray<float3> positions;

        public NativeArray<int> sortedIndexes;

        public void Execute()
        {
            // Insertion sort, along z axis
            for (int i = 1; i < sortedIndexes.Length; ++i)
            {
                int key = sortedIndexes[i];
                int j = i - 1;

                // Move elements of arr[0..i-1], 
                // that are greater than key, 
                // to one position ahead of 
                // their current position 
                while (j >= 0 && positions[sortedIndexes[j]].z > positions[key].z)
                {
                    sortedIndexes[j + 1] = sortedIndexes[j];
                    j--;
                }
                sortedIndexes[j + 1] = key;
            }
        }
    }

    // Using BurstCompile to compile a Job with burst
    // Set CompileSynchronously to true to make sure that the method will not be compiled asynchronously
    // but on the first schedule
    [BurstCompile(CompileSynchronously = true)]
    private struct WaterJob : IJobParallelFor
    {
        [ReadOnly] public float dt;
        [ReadOnly] public float gravity;
        [ReadOnly] public float forceRadius;
        [ReadOnly] public float waterForce;
        [ReadOnly] public float waterDamping;
        [ReadOnly] public float boundaryForce;
        [ReadOnly] public float3 worldPos;
        [ReadOnly] public float gridSizeX;
        [ReadOnly] public float gridSizeY;
        [ReadOnly] public float gridSizeZ;

        [ReadOnly] public NativeArray<int> sortedIndexes;
        [NativeDisableParallelForRestriction]
        [ReadOnly] public NativeArray<float3> velocities;
        [NativeDisableParallelForRestriction]
        [ReadOnly] public NativeArray<float3> positions;
        [NativeDisableParallelForRestriction]
        [WriteOnly] public NativeArray<float3> newVelocities;
        [NativeDisableParallelForRestriction]
        [WriteOnly] public NativeArray<float3> newPositions;

        public void Execute(int _i)
        {
            float3 gravityVector = new float3(0, -gravity * dt, 0);
            //for (int _i = 0; _i < sortedIndexes.Length; _i++)
            {
                int i = sortedIndexes[_i];
                float3 totalForce = gravityVector;

                // Contain above Y bottom
                float outsideY = (worldPos.y - gridSizeY / 2) - positions[i].y;
                if (outsideY > 0)
                {
                    if (outsideY > 3)
                        outsideY = 3;
                    totalForce.y += outsideY * outsideY * boundaryForce;

                    //Less bouncing
                    if (velocities[i].y < 0)
                        totalForce.y -= velocities[i].y * 0.8f * dt;
                }

                // Contain inside X range
                float outsideLeft = (worldPos.x - gridSizeX / 2) - positions[i].x;
                if (outsideLeft > 0)
                {
                    if (outsideLeft > 3)
                        outsideLeft = 3;
                    totalForce.x += outsideLeft * outsideLeft * boundaryForce;
                }
                float outsideRight = positions[i].x - (worldPos.x + gridSizeX / 2);
                if (outsideRight > 0)
                {
                    if (outsideRight > 3)
                        outsideRight = 3;

                    totalForce.x -= outsideRight * outsideRight * boundaryForce;
                }

                // Contain inside Z range
                float outsideBack = (worldPos.z - gridSizeZ / 2) - positions[i].z;
                if (outsideBack > 0)
                {
                    if (outsideBack > 3)
                        outsideBack = 3;
                    totalForce.z += outsideBack * outsideBack * boundaryForce;
                }
                float outsideFront = positions[i].z - (worldPos.z + gridSizeZ / 2);
                if (outsideFront > 0)
                {
                    if (outsideFront > 3)
                        outsideFront = 3;

                    totalForce.z -= outsideFront * outsideFront * boundaryForce;
                }

                // Force against nearby water
                for (int _j = math.max(_i - 100, 0); _j < _i + 100 && _j < sortedIndexes.Length; _j++)
                {
                    int j = sortedIndexes[_j];

                    float3 otherPos = positions[j];
                    float3 deltaPos = otherPos - positions[i];
                    float distance = math.length(deltaPos);
                    if (distance <= 0 || distance > forceRadius)
                        continue;

                    float3 direction = -deltaPos / distance;
                    float forceAmount01 = math.clamp((forceRadius - distance) / forceRadius, 0, 1);
                    float3 force = (direction * math.pow(forceAmount01, 0.5f) * waterForce) * dt;

                    //otherBody.AddForce(-force);
                    totalForce += force;
                }

                float3 damping = velocities[i] * math.length(velocities[i]) * waterDamping * dt;
                float3 velocity = velocities[i] + totalForce - damping;
                newPositions[i] = positions[i] + velocity * dt;
                newVelocities[i] = velocity;
            }
        }
    }
}
