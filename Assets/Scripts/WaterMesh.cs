using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterMesh : MonoBehaviour
{
    public float cellSize = 0.5f;
    public WaterGrid waterGrid;
    public WaterCamera waterCamera;
    public Material waterMaterial;

    float meshSizeX, meshSizeZ;
    int verticesX, verticesZ;
    Texture2D waterTexture2D;
    Vector3[] vertices;

    // Start is called before the first frame update
    void Start()
    {
        if (GetComponent<MeshRenderer>() == null)
            gameObject.AddComponent<MeshRenderer>();
        if (GetComponent<MeshFilter>() == null)
            gameObject.AddComponent<MeshFilter>();

        CreateWaterMesh();
    }

    void CreateWaterMesh()
    {
        meshSizeX = waterGrid.gridSize.x * waterGrid.gridSpacing;
        meshSizeZ = waterGrid.gridSize.z * waterGrid.gridSpacing;


        MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = waterMaterial ? waterMaterial : new Material(Shader.Find("Standard"));
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var mesh = new Mesh();

        var vertexList = new List<Vector3>();
        verticesX = 0;
        verticesZ = 0;
        for (float x = -meshSizeX / 2; x <= meshSizeX / 2; x += cellSize)
        {
            for (float z = -meshSizeZ / 2; z <= meshSizeZ / 2; z += cellSize)
            {
                vertexList.Add(new Vector3(x, 0, z));

                if (verticesX == 0)
                    verticesZ++;
            }
            verticesX++;
        }
        vertices = vertexList.ToArray();
        mesh.vertices = vertices;

        var triangles = new List<int>();
        for (int indexX = 0; indexX < verticesX - 1; indexX++)
        {
            for (int indexZ = 0; indexZ < verticesZ - 1; indexZ++)
            {
                triangles.Add(indexX * verticesZ + indexZ);                 // x,   z
                triangles.Add((indexX + 1) * verticesZ + (indexZ + 1));     // x+1, z+1
                triangles.Add((indexX + 1) * verticesZ + indexZ);           // x+1, z

                triangles.Add(indexX * verticesZ + indexZ);                 // x,   z
                triangles.Add(indexX * verticesZ + (indexZ + 1));           // x,   z+1
                triangles.Add((indexX + 1) * verticesZ + (indexZ + 1));     // x+1, z+1
            }
        }
        mesh.triangles = triangles.ToArray();

        GetComponent<MeshFilter>().mesh = mesh;
        waterCamera.GetComponent<Camera>().targetTexture = new RenderTexture(verticesX, verticesZ, 0);

        waterTexture2D = new Texture2D(verticesX, verticesZ);
    }

    private void FixedUpdate()
    {
        var cam = waterCamera.GetComponent<Camera>();
        if (cam.targetTexture)
        {
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture.active = cam.targetTexture;

            waterTexture2D.ReadPixels(new Rect(0, 0, verticesX, verticesZ), 0, 0);
            waterTexture2D.Apply();

            int i = 0;
            for (int indexX = 0; indexX < verticesX; indexX++)
            {
                for (int indexZ = 0; indexZ < verticesZ; indexZ++)
                {
                    var newVertexPos = vertices[i];
                    float waterHeight01 = waterTexture2D.GetPixel(indexX, indexZ).r;
                    newVertexPos.y = (waterHeight01 - 1) * waterCamera.GetComponent<Camera>().farClipPlane;
                    vertices[i] = newVertexPos;
                    i++;
                }
            }

            var mesh = GetComponent<MeshFilter>().mesh;
            mesh.vertices = vertices;
            mesh.RecalculateNormals();

            RenderTexture.active = previousActive;
        }
    }
}
