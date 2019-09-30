using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    //Creating an enum to not override the original Noise.cs code
    public enum DrawMode {NoiseMap, ColourMap, Mesh, FallOffMap};
    public DrawMode drawMode;
    //Variables

    //Creating a constant value for the chunk size of the map
    //241 as width - 1 must be divisible by i where i is any integer
    public const int mapChunkSize = 241;

    //Setting the normalized mode 
    public Noise.NormalizeMode normalizedMode;

    //Defining an LOD to have different levels of detail to smooth the performance of the game
    [Range(0, 6)]
    public int editorPreviewLOD;

    public float noiseScale;

    //Adding in a button to allow the map to autoUpdate outside of game mode
    public bool autoUpdate;

    //Variables for defining the perlin noise values
    public int octaves;
    [Range(0, 1)]
    public float persistance;
    public float lacunarity;

    //Creating new seeds
    public int seed;
    public Vector2 offset;

    //Adding in multipliers to make the mesh less flat, and then an AnimationCurve to smooth out the water textures
    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;

    //Choosing whether to apply the falloff map or not
    public bool useFalloffMap;
    float[,] falloffMap;

    public TerrainType[] regions;

    //Creating a queue variable for the threading to work
    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    void Awake()
    {
        falloffMap = FallOffGenerator.GenerateFalloffMap(mapChunkSize);
    }

    public void DrawMapInEditor ()
    {
        MapData mapData = GenerateMapData(Vector2.zero);

        //Referencing the MapDisplay method
        MapDisplay display = FindObjectOfType<MapDisplay>();

        //Saying which drawMode we are in and whether to draw the colours or not
        if (drawMode == DrawMode.NoiseMap)
        {
            //Displaying the noise map
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
        }
        else if (drawMode == DrawMode.ColourMap)
        {
            //Displaying the colour map
            display.DrawTexture(TextureGenerator.TextureFromColourMap(mapData.colourMap, mapChunkSize, mapChunkSize));
        }
        else if (drawMode == DrawMode.Mesh)
        {
            //Displaying the mesh map
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, editorPreviewLOD), TextureGenerator.TextureFromColourMap(mapData.colourMap, mapChunkSize, mapChunkSize));
        }
        else if (drawMode == DrawMode.FallOffMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(FallOffGenerator.GenerateFalloffMap(mapChunkSize)));
        }
    }

    //Implimenting threading to allow for smoother fps when loading chunks
    public void RequestMapData (Vector2 centre, Action<MapData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(centre, callback);
        };

        new Thread(threadStart).Start();
    }

    void MapDataThread (Vector2 centre, Action<MapData> callback)
    {
        MapData mapData = GenerateMapData(centre);

        //Locking the thread so that while this thread is being called, no other can also be called (will have to 'wait its turn')
        lock (mapDataThreadInfoQueue)
        {
            //Implementing the queue
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    //Creating another threading component for the mesh data
    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate 
        {
            MeshDataThread(mapData, lod, callback);
        };

        new Thread(threadStart).Start();

    }

    void MeshDataThread (MapData mapData, int lod, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, lod);

        lock(meshDataThreadInfoQueue)
        {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    void Update()
    {
        if (mapDataThreadInfoQueue.Count > 0)
        {
            //If the queue has something in it then loop through the elements
            for (int i = 0; i < mapDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }

        if (meshDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < meshDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    //Generating the map data
    MapData GenerateMapData(Vector2 centre)
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, seed, noiseScale, octaves, persistance, lacunarity, centre + offset, normalizedMode);

        //Creating a 1D colour map to store the colours of each region
        Color[] colourMap = new Color[mapChunkSize * mapChunkSize];

        //Looping through the regions to see what height each region falls within to assign a colour
        for (int y = 0; y < mapChunkSize; y++)
        {
            for (int x = 0; x < mapChunkSize; x++)
            {
                if (useFalloffMap)
                {
                    noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);
                }
                float currentHeight = noiseMap[x, y];

                for (int i = 0; i < regions.Length; i++)
                {
                    if (currentHeight >= regions[i].height)
                    {
                        colourMap[y * mapChunkSize + x] = regions[i].colour;
                        
                    } else
                    {
                        break;
                    }
                }
            }
        }

        return new MapData(noiseMap, colourMap);


        
    }

    void OnValidate()
    {
        //Clamping some important values to not allow them to be below a certain number
        if (lacunarity < 1)
        {
            lacunarity = 1;
        }

        if (octaves < 0)
        {
            octaves = 0;
        }

        falloffMap = FallOffGenerator.GenerateFalloffMap(mapChunkSize);

        
    }

    //Making the struct generic with <T> to allow it to also work with meshData
    struct MapThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}

//Creating a struct class to colour the terrain at different heights 
//Also make it Serializable so it will show up in the inspector in Unity
[System.Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color colour;
}

public struct MapData
{
    public readonly float[,] heightMap;
    public readonly Color[] colourMap;

    //Automatically generating a constructor using Ctrl + .
    public MapData(float[,] heightMap, Color[] colourMap)
    {
        this.heightMap = heightMap;
        this.colourMap = colourMap;
    }
}
