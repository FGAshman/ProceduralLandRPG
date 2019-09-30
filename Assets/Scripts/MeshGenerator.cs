using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator  
{
    public static MeshData GenerateTerrainMesh (float[,] heightMap, float heightMultiplier, AnimationCurve _heightCurve, int levelOfDetail)
    {
        AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys);
        //Getting the width and height of the 2D array
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        //Centering the mesh on the screen
        float topLeftX = (width - 1) / -2f;
        float topLeftZ = (height - 1) / 2f;

        //Defining simplification implements to iterate through on the heightMap
        //Also defining that if the LOD = 0, then the meshSimplificationIncrement = 1 (i.e. no difference from previous code)
        int meshSimplificationIncrement = (levelOfDetail == 0)?1 : levelOfDetail * 2;
        int verticesPerLine = (width - 1) / meshSimplificationIncrement + 1;

        //Creating the MeshData variable
        MeshData meshData = new MeshData(verticesPerLine, verticesPerLine);

        int vertexIndex = 0;

        //Looping through the heightMap
        for (int y = 0; y < height; y += meshSimplificationIncrement)
        {
            for (int x = 0; x < width; x += meshSimplificationIncrement)
            {
                //Creating the vertices of the mesh
                //Multiply the y value by heightMultiplier to make the mesh less flat
                meshData.vertices[vertexIndex] = new Vector3(topLeftX + x, heightCurve.Evaluate(heightMap[x, y]) * heightMultiplier, topLeftZ - y);

                meshData.uvs[vertexIndex] = new Vector2(x / (float)width, y / (float)height);

                //Ignoring the right and bottom edge vertices of the map to generate mesh triangles
                if (x < width - 1 && y < height - 1)
                {
                    //Adding the triangles to the meshData
                    meshData.AddTriangles(vertexIndex, vertexIndex + verticesPerLine + 1, vertexIndex + verticesPerLine);
                    meshData.AddTriangles(vertexIndex + verticesPerLine + 1, vertexIndex, vertexIndex + 1);
                }

                
                //Incrimenting the vertexIndex
                vertexIndex++;
            }
        }

        //Don't return the mesh itself to stop the game freezing up when generating new chunks (through threading)
        return meshData;
    }
}

public class MeshData
{
    public Vector3[] vertices;
    public int[] triangles;

    //Creating a Vector2 array to be able to add textures to the meshes
    public Vector2[] uvs;

    int triangleIndex;

    //Creating a constructor
    public MeshData(int meshWidth, int meshHeight)
    {
        //Initialising the variables
        vertices = new Vector3[meshWidth * meshHeight];

        //Creating the uvs to be able to add textures to the mesh
        uvs = new Vector2[meshWidth * meshHeight];

        triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6];

    }

    //Creating a convinient method of adding triangles
    public void AddTriangles (int a, int b, int c)
    {
        triangles[triangleIndex] = a;
        triangles[triangleIndex + 1] = b;
        triangles[triangleIndex + 2] = c;

        triangleIndex += 3;
    }

    Vector3[] CalculateNormals()
    {
        Vector3[] vertexNormals = new Vector3[vertices.Length];
        int triangleCount = triangles.Length / 3;

        //i is the number of the triangle, therefore 3i is the index of the triangle array
        for (int i = 0; i < triangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = triangles[normalTriangleIndex];
            int vertexIndexB = triangles[normalTriangleIndex + 1];
            int vertexIndexC = triangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndicies(vertexIndexA, vertexIndexB, vertexIndexC);

        }
    }

    Vector3 [] SurfaceNormalFromIndicies (int indexA, int indexB, int indexC)
    {
        Vector3 pointA = vertices[indexA];
        Vector3 pointB = vertices[indexB];
        Vector3 pointC = vertices[indexC];

        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;

        return Vector3.Cross(sideAB, sideAC).normalized;
    }

    public Mesh CreateMesh ()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        //Using recalculate normals to not have problems with lighting
        mesh.RecalculateNormals();

        return mesh;

    }
}
