using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class OptimizedWater : MonoBehaviour
{
    [Min(0)] public float gravity = 8;
    [Min(0.1f)] public float forceRadius = 3f;
    [Min(0)] public float waterForce = 20f;
    [Min(0)] public float waterDamping = 0.2f;
    [Min(0)] public float boundaryForce = 0.2f;
    [Min(0)] public float boatForceStrength = 20;
    [Min(0.1f)] public float boatPartRadius = 1;
    [Min(0.1f)] public float totalBoatRadius = 10;
    [Min(0.01f)] public float boatDensity = 0.1f;

    private List<WaterBlob> waterBlobs;
    NativeArray<float3> velocities, positions, newVelocities, newPositions;
    NativeArray<float3> boatPositions;
    NativeArray<float3> boatForces;

    private float cellSide = 2f;
    private int cellSpace = 50;
    private int xCells, zCells;
    private NativeArray<int> grid;
    private int boatCellSpace = 20;
    private NativeArray<int> boatGrid;
    private bool printed;

    public Rigidbody boat;
    private WaterGrid m_WaterGrid;

    private void Start() {
        m_WaterGrid = GetComponent<WaterGrid>();
        waterBlobs = m_WaterGrid.WaterBlobs;

        velocities = new NativeArray<float3>(waterBlobs.Count, Allocator.Persistent);
        positions = new NativeArray<float3>(waterBlobs.Count, Allocator.Persistent);
        newVelocities = new NativeArray<float3>(waterBlobs.Count, Allocator.Persistent);
        newPositions = new NativeArray<float3>(waterBlobs.Count, Allocator.Persistent);

        var gridSize = (Vector3) m_WaterGrid.gridSize * m_WaterGrid.gridSpacing;
        xCells = Mathf.CeilToInt(gridSize.x / cellSide);
        zCells = Mathf.CeilToInt(gridSize.z / cellSide);
        grid = new NativeArray<int>(xCells * zCells * cellSpace, Allocator.Persistent);
        printed = false;
        for (int i = 0; i < xCells * zCells * cellSpace; i++) {
            grid[i] = 0;
        }
        boatGrid = new NativeArray<int>(xCells * zCells * boatCellSpace, Allocator.Persistent);
        for (int i = 0; i < xCells * zCells * boatCellSpace; i++) {
            boatGrid[i] = 0;
        }

        for (int i = 0; i < positions.Length; i++) {
            positions[i] = new float3(waterBlobs[i].transform.position);
        }
    }

    void PrintGrid() {
        var myStr = "Cell space: " + cellSpace + "\n";
        for (int z = 0; z < zCells; z++) {
            for (int x = 0; x < xCells; x++) {
                int cellStart = (z * xCells + x) * cellSpace;
                myStr += grid[cellStart] + "|";
            }

            myStr += "\n";
        }
        myStr += "\nBoat - Cell space: " + boatCellSpace + "\n";
        for (int z = 0; z < zCells; z++) {
            for (int x = 0; x < xCells; x++) {
                int cellStart = (z * xCells + x) * boatCellSpace;
                myStr += boatGrid[cellStart] + "|";
            }

            myStr += "\n";
        }

        Debug.Log(myStr);
    }

    void FixedUpdate() {
        var gridSize = (Vector3) m_WaterGrid.gridSize * m_WaterGrid.gridSpacing;
        positions[0] = waterBlobs[0].transform.position;

        for (int i = 0; i < xCells * zCells; i += 1) {
            grid[i * cellSpace] = 0;
            boatGrid[i * boatCellSpace] = 0;
        }

        var sortjob = new SortJob {
            positions = positions,
            grid = grid,
            xCells = xCells,
            zCells = zCells,
            halfXSize = gridSize.x / 2,
            halfZSize = gridSize.z / 2,
            cellSpace = cellSpace,
            cellSide = cellSide
        };
        sortjob.Schedule().Complete();
        
        int boatPartCount = boat.transform.childCount;
        boat.mass = boatPartCount * boatDensity;

        //Reallocate boat arrays if needed
        if (!boatPositions.IsCreated || boatPositions.Length != boatPartCount) {
            if (boatPositions.IsCreated)
                boatPositions.Dispose();
            boatPositions = new NativeArray<float3>(boatPartCount, Allocator.Persistent);
        }

        if (!boatForces.IsCreated || boatForces.Length != boatPartCount) {
            if (boatForces.IsCreated)
                boatForces.Dispose();
            boatForces = new NativeArray<float3>(boatPartCount, Allocator.Persistent);
        }

        //Init boat positions
        for (int i = 0; i < boatPositions.Length; i++) {
            boatPositions[i] = boat.transform.GetChild(i).position;
        }

        var sortboatjob = new SortJob {
            positions = boatPositions,
            grid = boatGrid,
            xCells = xCells,
            zCells = zCells,
            halfXSize = gridSize.x / 2,
            halfZSize = gridSize.z / 2,
            cellSpace = boatCellSpace,
            cellSide = cellSide
        };
        sortboatjob.Schedule().Complete();

        if (!printed) {
            PrintGrid();
            printed = true;
        }
        
        var waterjob = new WaterJob {
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

            grid = grid,
            xCells = xCells,
            zCells = zCells,
            cellSpace = cellSpace,
            cellSide = cellSide,
            boatGrid = boatGrid,
            boatCellSpace = boatCellSpace,

            velocities = velocities,
            positions = positions,
            newVelocities = newVelocities,
            newPositions = newPositions,

            boatPositions = boatPositions,
        };
        waterjob.Schedule(positions.Length, 256).Complete();

        var boatJob = new BoatJob {
            dt = Time.fixedDeltaTime,
            boatRadius = totalBoatRadius,
            boatPartRadius = boatPartRadius,
            boatForceStrength = boatForceStrength,
            boundaryForce = boundaryForce,

            worldPos = transform.position,
            gridSizeX = gridSize.x,
            gridSizeY = gridSize.y,
            gridSizeZ = gridSize.z,

            grid = grid,
            xCells = xCells,
            zCells = zCells,
            cellSpace = cellSpace,
            cellSide = cellSide,

            velocities = velocities,
            positions = positions,
            boatPositions = boatPositions,
            boatForces = boatForces,
        };
        boatJob.Schedule(boatPartCount, 1).Complete();

        for (int i = 0; i < waterBlobs.Count; i++) {
            waterBlobs[i].transform.position = newPositions[i];
        }

        for (int i = 0; i < boatForces.Length; i++) {
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

    private void OnDestroy() {
        if (boatForces.IsCreated)
            boatForces.Dispose();

        if (boatPositions.IsCreated)
            boatPositions.Dispose();

        velocities.Dispose();
        positions.Dispose();
        newVelocities.Dispose();
        newPositions.Dispose();

        grid.Dispose();
        boatGrid.Dispose();
    }

    [BurstCompile(CompileSynchronously = true)]
    private struct SortJob : IJob
    {
        [ReadOnly] public NativeArray<float3> positions;
        [ReadOnly] public int xCells;
        [ReadOnly] public int zCells;
        [ReadOnly] public float halfXSize;
        [ReadOnly] public float halfZSize;
        [ReadOnly] public float cellSide;
        [ReadOnly] public int cellSpace;

        public NativeArray<int> grid;

        public void Execute() {
            // Insertion sort, along z axis
            for (int i = 1; i < positions.Length; ++i) {
                int key = i;

                // Place key in the grid
                int gridX = (int) math.floor((positions[key].x + halfXSize) / cellSide);
                if (gridX < 0) gridX = 0;
                else if (gridX > xCells - 1) gridX = xCells - 1;
                int gridZ = (int) math.floor((positions[key].z + halfZSize) / cellSide);
                if (gridZ < 0) gridZ = 0;
                else if (gridZ > zCells - 1) gridZ = zCells - 1;
                int cellStart = (gridZ * xCells + gridX) * cellSpace;
                int stored = grid[cellStart];
                if (stored < cellSpace - 1) {
                    grid[cellStart + 1 + stored] = key;
                    grid[cellStart] = stored + 1;
                }
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


        [ReadOnly] public int xCells;
        [ReadOnly] public int zCells;
        [ReadOnly] public float cellSide;
        [ReadOnly] public int cellSpace;

        [ReadOnly] public NativeArray<int> grid;

        [ReadOnly] public NativeArray<int> boatGrid;
        [ReadOnly] public int boatCellSpace;
        
        [ReadOnly] public float boatRadius;
        [ReadOnly] public float boatPartRadius;
        [ReadOnly] public float boatForceStrength;

        [NativeDisableParallelForRestriction] [ReadOnly]
        public NativeArray<float3> velocities;

        [NativeDisableParallelForRestriction] [ReadOnly]
        public NativeArray<float3> positions;

        [NativeDisableParallelForRestriction] [WriteOnly]
        public NativeArray<float3> newVelocities;

        [NativeDisableParallelForRestriction] [WriteOnly]
        public NativeArray<float3> newPositions;


        [ReadOnly] public NativeArray<float3> boatPositions;

        public void Execute(int _i) {
            float3 gravityVector = new float3(0, -gravity * dt, 0);
            int i = _i;
            //for (int _i = 0; _i < sortedIndexes.Length; _i++)
            {
                float3 totalForce = gravityVector;

                // Contain above Y bottom
                float outsideY = (worldPos.y - gridSizeY / 2) - positions[i].y;
                if (outsideY > 0) {
                    if (outsideY > 3)
                        outsideY = 3;
                    totalForce.y += outsideY * outsideY * boundaryForce;

                    //Less bouncing
                    if (velocities[i].y < 0)
                        totalForce.y -= velocities[i].y * 0.8f * dt;
                }

                // Contain inside X range
                float outsideLeft = (worldPos.x - gridSizeX / 2) - positions[i].x;
                if (outsideLeft > 0) {
                    if (outsideLeft > 3)
                        outsideLeft = 3;
                    totalForce.x += outsideLeft * outsideLeft * boundaryForce;
                }

                float outsideRight = positions[i].x - (worldPos.x + gridSizeX / 2);
                if (outsideRight > 0) {
                    if (outsideRight > 3)
                        outsideRight = 3;

                    totalForce.x -= outsideRight * outsideRight * boundaryForce;
                }

                // Contain inside Z range
                float outsideBack = (worldPos.z - gridSizeZ / 2) - positions[i].z;
                if (outsideBack > 0) {
                    if (outsideBack > 3)
                        outsideBack = 3;
                    totalForce.z += outsideBack * outsideBack * boundaryForce;
                }

                float outsideFront = positions[i].z - (worldPos.z + gridSizeZ / 2);
                if (outsideFront > 0) {
                    if (outsideFront > 3)
                        outsideFront = 3;

                    totalForce.z -= outsideFront * outsideFront * boundaryForce;
                }

                // Getting the position in the grid for this blob
                int gridX = (int) math.floor((positions[i].x + gridSizeX / 2) / cellSide);
                if (gridX < 0) gridX = 0;
                else if (gridX > xCells - 1) gridX = xCells - 1;
                int gridZ = (int) math.floor((positions[i].z + gridSizeZ / 2) / cellSide);
                if (gridZ < 0) gridZ = 0;
                else if (gridZ > zCells - 1) gridZ = zCells - 1;

                // We loop over a square of 3x3 cells around the cell
                int startX = math.min(math.max(gridX - 1, 0), xCells - 1);
                int endX = math.min(math.max(gridX + 1, 0), xCells - 1);
                int startZ = math.min(math.max(gridZ - 1, 0), zCells - 1);
                int endZ = math.min(math.max(gridZ + 1, 0), zCells - 1);

                for (int x = startX; x <= endX; x++) {
                    for (int z = startZ; z <= endZ; z++) {
                        int cellStart = (z * xCells + x) * cellSpace;
                        int stored = grid[cellStart];
                        for (int _j = 1; _j <= stored; _j++) {
                            int j = grid[cellStart + _j];
                            // for (int _j = math.max(_i - 300, 0); _j < _i + 300 && _j < sortedIndexes.Length; _j++)
                            // {
                            //     int j = sortedIndexes[_j];

                            float3 otherPos = positions[j];
                            float3 deltaPos = otherPos - positions[i];
                            float distance = math.length(deltaPos);
                            if (distance <= 0 || distance > forceRadius) {
                                continue;
                            }

                            float3 direction = -deltaPos / distance;
                            float forceAmount01 = math.clamp((forceRadius - distance) / forceRadius, 0, 1);
                            float3 force = (direction * math.pow(forceAmount01, 0.5f) * waterForce) * dt;

                            //otherBody.AddForce(-force);
                            totalForce += force;
                        }

                        // Water affected by boat parts
                        cellStart = (z * xCells + x) * boatCellSpace;
                        stored = boatGrid[cellStart];
                        for (int _j = 1; _j <= stored; _j++) {
                            float3 otherPos = boatPositions[boatGrid[cellStart + _j]];
                            float3 deltaPos = otherPos - positions[i];
                            float distance = math.length(deltaPos);
                            if (distance <= 0 || distance > boatPartRadius)
                                continue;
                        
                            float3 direction = -deltaPos / distance;
                            float forceAmount01 = math.clamp((boatPartRadius - distance) / boatPartRadius, 0, 1);
                            totalForce += (direction * math.pow(forceAmount01, 0.5f) * boatForceStrength) * dt;
                        }
                    }
                }
                
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
    private struct BoatJob : IJobParallelFor
    {
        [ReadOnly] public float dt;
        [ReadOnly] public float boatRadius;
        [ReadOnly] public float boatPartRadius;
        [ReadOnly] public float boatForceStrength;

        [NativeDisableParallelForRestriction] [ReadOnly]
        public NativeArray<float3> velocities;

        [NativeDisableParallelForRestriction] [ReadOnly]
        public NativeArray<float3> positions;

        [ReadOnly] public float boundaryForce;
        [ReadOnly] public float3 worldPos;
        [ReadOnly] public float gridSizeX;
        [ReadOnly] public float gridSizeY;
        [ReadOnly] public float gridSizeZ;

        [ReadOnly] public int xCells;
        [ReadOnly] public int zCells;
        [ReadOnly] public float cellSide;
        [ReadOnly] public int cellSpace;

        [ReadOnly] public NativeArray<int> grid;
        
        [ReadOnly] public NativeArray<float3> boatPositions;
        [WriteOnly] public NativeArray<float3> boatForces;

        public void Execute(int i) {
            // Water affecting boat parts
            float3 totalForce = new float3(0, 0, 0);
            
            // Getting the position in the grid for this boat part
            int gridX = (int) math.floor((boatPositions[i].x + gridSizeX / 2) / cellSide);
            if (gridX < 0) gridX = 0;
            else if (gridX > xCells - 1) gridX = xCells - 1;
            int gridZ = (int) math.floor((boatPositions[i].z + gridSizeZ / 2) / cellSide);
            if (gridZ < 0) gridZ = 0;
            else if (gridZ > zCells - 1) gridZ = zCells - 1;
            
            // We loop over a square of 3x3 cells around the cell
            int startX = math.min(math.max(gridX - 1, 0), xCells - 1);
            int endX = math.min(math.max(gridX + 1, 0), xCells - 1);
            int startZ = math.min(math.max(gridZ - 1, 0), zCells - 1);
            int endZ = math.min(math.max(gridZ + 1, 0), zCells - 1);

            for (int x = startX; x <= endX; x++) {
                for (int z = startZ; z <= endZ; z++) {
                    int cellStart = (z * xCells + x) * cellSpace;
                    int stored = grid[cellStart];
                    for (int _j = 1; _j <= stored; _j++) {
                        int j = grid[cellStart + _j];
                    //    for (int j = 0; j < positions.Length; j++) {
                        float3 otherPos = positions[j];
                        float3 deltaPos = otherPos - boatPositions[i];
                        float distance = math.length(deltaPos);
                        if (distance <= 0 || distance > boatPartRadius)
                            continue;

                        float3 direction = -deltaPos / distance;
                        float forceAmount01 = math.clamp((boatPartRadius - distance) / boatPartRadius, 0, 1);
                        float3 force = (direction * math.pow(forceAmount01, 0.5f) * boatForceStrength) * dt;
                        if (direction.y < 0f) {
                            force = new Vector3(force.x, force.y * 5f, force.z);
                        }

                        totalForce += force;
                    }
                }
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
            if (outsideLeft > 0) {
                if (outsideLeft > 3)
                    outsideLeft = 3;
                totalForce.x += outsideLeft * outsideLeft * boundaryForce;
            }

            float outsideRight = boatPositions[i].x - (worldPos.x + gridSizeX / 2);
            if (outsideRight > 0) {
                if (outsideRight > 3)
                    outsideRight = 3;

                totalForce.x -= outsideRight * outsideRight * boundaryForce;
            }

            // Contain inside Z range
            float outsideBack = (worldPos.z - gridSizeZ / 2) - boatPositions[i].z;
            if (outsideBack > 0) {
                if (outsideBack > 3)
                    outsideBack = 3;
                totalForce.z += outsideBack * outsideBack * boundaryForce;
            }

            float outsideFront = boatPositions[i].z - (worldPos.z + gridSizeZ / 2);
            if (outsideFront > 0) {
                if (outsideFront > 3)
                    outsideFront = 3;

                totalForce.z -= outsideFront * outsideFront * boundaryForce;
            }

            boatForces[i] = totalForce;
        }
    }
}