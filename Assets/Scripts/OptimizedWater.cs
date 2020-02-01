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
    [Min(0)]
    public float boatForceStrength = 20;
    [Min(0.1f)]
    public float boatPartRadius = 1;
    [Min(0.1f)]
    public float totalBoatRadius = 10;
    [Min(0.01f)]
    public float boatDensity = 0.1f;

    private List<WaterBlob> waterBlobs;
    NativeArray<int> sortedIndexes;
    NativeArray<float3> velocities, positions, newVelocities, newPositions;
    NativeArray<float3> boatPositions;
    NativeArray<float3> boatForces;

    public Rigidbody boat;

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

        int boatPartCount = boat.transform.childCount;
        boat.mass = boatPartCount * boatDensity;

        //Reallocate boat arrays if needed
        if (!boatPositions.IsCreated || boatPositions.Length != boatPartCount)
        {
            if (boatPositions.IsCreated)
                boatPositions.Dispose();
            boatPositions = new NativeArray<float3>(boatPartCount, Allocator.Persistent);
        }
        if (!boatForces.IsCreated || boatForces.Length != boatPartCount)
        {
            if (boatForces.IsCreated)
                boatForces.Dispose();
            boatForces = new NativeArray<float3>(boatPartCount, Allocator.Persistent);
        }
        
        //Init boat positions
        for(int i = 0; i < boatPositions.Length; i++)
        {
            boatPositions[i] = boat.transform.GetChild(i).position;
        }

        var gridSize = (Vector3)GetComponent<WaterGrid>().gridSize * GetComponent<WaterGrid>().gridSpacing;
        var waterjob = new WaterJob
        {
            dt = Time.fixedDeltaTime,
            gravity = gravity,
            forceRadius = forceRadius,
            waterForce = waterForce,
            waterDamping = waterDamping,
            boundaryForce = boundaryForce,

            boatRadius = totalBoatRadius,
            boatPartRadius = boatPartRadius,
            boatForceStrength = boatForceStrength,

            worldPos = transform.position,
            gridSizeX = gridSize.x,
            gridSizeY = gridSize.y,
            gridSizeZ = gridSize.z,

            sortedIndexes = sortedIndexes,
            velocities = velocities,
            positions = positions,
            newVelocities = newVelocities,
            newPositions = newPositions,

            boatPositions = boatPositions,
        };
        waterjob.Schedule(sortedIndexes.Length, 256).Complete();

        var boatJob = new BoatJob
        {
            dt = Time.fixedDeltaTime,
            boatRadius = totalBoatRadius,
            boatPartRadius = boatPartRadius,
            boatForceStrength = boatForceStrength,
            boundaryForce = boundaryForce,

            worldPos = transform.position,
            gridSizeX = gridSize.x,
            gridSizeY = gridSize.y,
            gridSizeZ = gridSize.z,

            sortedIndexes = sortedIndexes,
            velocities = velocities,
            positions = positions,
            boatPositions = boatPositions,
            boatForces = boatForces,
        };
        boatJob.Schedule(boatPartCount, 1).Complete();

        for (int i = 0; i < waterBlobs.Count; i++)
        {
            waterBlobs[i].transform.position = newPositions[i];
        }
        for(int i = 0; i < boatForces.Length; i++)
        {
            boat.AddForceAtPosition(boatForces[i], boat.transform.GetChild(i).position, ForceMode.Impulse);
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
        if (boatForces.IsCreated)
            boatForces.Dispose();

        if (boatPositions.IsCreated)
            boatPositions.Dispose();

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

        [ReadOnly] public float boatRadius;
        [ReadOnly] public float boatPartRadius;
        [ReadOnly] public float boatForceStrength;

        [ReadOnly] public NativeArray<int> sortedIndexes;
        [NativeDisableParallelForRestriction]
        [ReadOnly] public NativeArray<float3> velocities;
        [NativeDisableParallelForRestriction]
        [ReadOnly] public NativeArray<float3> positions;
        [NativeDisableParallelForRestriction]
        [WriteOnly] public NativeArray<float3> newVelocities;
        [NativeDisableParallelForRestriction]
        [WriteOnly] public NativeArray<float3> newPositions;

        
        [ReadOnly] public NativeArray<float3> boatPositions;

        public void Execute(int _i)
        {
            float3 gravityVector = new float3(0, -gravity * dt, 0);
            int i = sortedIndexes[_i];
            //for (int _i = 0; _i < sortedIndexes.Length; _i++)
            {
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

                // Water affecting nearby water
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

                // Water affected by boat parts
                float3 forceFromBoat = new float3(0, 0, 0);
                for (int b = 0; b < boatPositions.Length; b++)
                {
                    float3 otherPos = boatPositions[b];
                    float3 deltaPos = otherPos - positions[i];
                    float distance = math.length(deltaPos);
                    if (distance <= 0 || distance > boatPartRadius)
                        continue;

                    float3 direction = -deltaPos / distance;
                    float forceAmount01 = math.clamp((boatPartRadius - distance) / boatPartRadius, 0, 1);
                    forceFromBoat += (direction * math.pow(forceAmount01, 0.5f) * boatForceStrength) * dt;
                }

                totalForce += forceFromBoat;

                float3 damping = velocities[i] * math.length(velocities[i]) * waterDamping * dt;
                float3 velocity = velocities[i] + totalForce - damping;
                newPositions[i] = positions[i] + velocity * dt;
                newVelocities[i] = velocity;
            }
        }
    }


    // Using BurstCompile to compile a Job with burst
    // Set CompileSynchronously to true to make sure that the method will not be compiled asynchronously
    // but on the first schedule
    [BurstCompile(CompileSynchronously = true)]
    private struct BoatJob: IJobParallelFor
    {
        [ReadOnly] public float dt;
        [ReadOnly] public float boatRadius;
        [ReadOnly] public float boatPartRadius;
        [ReadOnly] public float boatForceStrength;

        [ReadOnly] public NativeArray<int> sortedIndexes;
        [NativeDisableParallelForRestriction]
        [ReadOnly] public NativeArray<float3> velocities;
        [NativeDisableParallelForRestriction]
        [ReadOnly] public NativeArray<float3> positions;
        [ReadOnly] public float boundaryForce;
        [ReadOnly] public float3 worldPos;
        [ReadOnly] public float gridSizeX;
        [ReadOnly] public float gridSizeY;
        [ReadOnly] public float gridSizeZ;

        [ReadOnly] public NativeArray<float3> boatPositions;
        [WriteOnly] public NativeArray<float3> boatForces;

        public void Execute(int i)
        {
            // Water affecting boat parts
            float3 totalForce = new float3(0, 0, 0);
            for (int _j = 0; _j < sortedIndexes.Length; _j++)
            {
                int j = sortedIndexes[_j];

                float3 otherPos = positions[j];
                float3 deltaPos = otherPos - boatPositions[i];
                float distance = math.length(deltaPos);
                if (distance <= 0 || distance > boatPartRadius)
                    continue;

                float3 direction = -deltaPos / distance;
                float forceAmount01 = math.clamp((boatPartRadius - distance) / boatPartRadius, 0, 1);
                float3 force = (direction * math.pow(forceAmount01, 0.5f) * boatForceStrength) * dt;

                totalForce += force;
            }
            // Contain above Y bottom
            //float outsideY = (worldPos.y - gridSizeY / 2) - boatPositions[i].y;
            //if (outsideY > 0)
            //{
            //    if (outsideY > 3)
            //        outsideY = 3;
            //    totalForce.y += outsideY * outsideY * boundaryForce;
                
            //}

            // Contain inside X range
            float outsideLeft = (worldPos.x - gridSizeX / 2) - boatPositions[i].x;
            if (outsideLeft > 0)
            {
                if (outsideLeft > 3)
                    outsideLeft = 3;
                totalForce.x += outsideLeft * outsideLeft * boundaryForce;
            }
            float outsideRight = boatPositions[i].x - (worldPos.x + gridSizeX / 2);
            if (outsideRight > 0)
            {
                if (outsideRight > 3)
                    outsideRight = 3;

                totalForce.x -= outsideRight * outsideRight * boundaryForce;
            }

            // Contain inside Z range
            float outsideBack = (worldPos.z - gridSizeZ / 2) - boatPositions[i].z;
            if (outsideBack > 0)
            {
                if (outsideBack > 3)
                    outsideBack = 3;
                totalForce.z += outsideBack * outsideBack * boundaryForce;
            }
            float outsideFront = boatPositions[i].z - (worldPos.z + gridSizeZ / 2);
            if (outsideFront > 0)
            {
                if (outsideFront > 3)
                    outsideFront = 3;

                totalForce.z -= outsideFront * outsideFront * boundaryForce;
            }

            boatForces[i] = totalForce;
        }
    }
}
