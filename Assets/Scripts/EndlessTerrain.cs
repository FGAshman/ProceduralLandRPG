using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    //Variables

    //Setting a variable which defines the distance the player can move before any chunks are updated
    const float viewerMoveThresholdChunkUpdate = 25f;
    const float sqrviewerMoveThresholdChunkUpdate = viewerMoveThresholdChunkUpdate * viewerMoveThresholdChunkUpdate;

    const float scale = 1;

    //Array of LOD levels
    public LODInfo[] detailLevels;

    //How far the player can see
    public static float maxViewDist;

    //Getting a reference to the players transform to be able to figure out their position
    public Transform viewer;
    public static Vector2 viewerPosition;
    Vector2 viewerPositionOld;

    //Chunk size and number of chunks that are visible
    int chunkSize;
    int chunksVisibleInViewDistance;

    //Keeping a dictionary of chunk coordinates to prevent duplicates
    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk> ();

    //Creating a list of terrain chunks to disable any chunks that are no longer visible
    static List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    static MapGenerator mapGenerator;

    //Assigning a material for the mesh to be made of
    public Material mapMaterial;

    void Start()
    {
        //Assigning the mapGenerator
        mapGenerator = FindObjectOfType<MapGenerator>();

        //Stating the max view distance
        maxViewDist = detailLevels[detailLevels.Length - 1].visibleDistanceThreshold;

        //Defining the size of the chunks as mapChunkSize - 1 as each chunk is actually 240 x 240
        chunkSize = MapGenerator.mapChunkSize - 1;
        chunksVisibleInViewDistance = Mathf.RoundToInt(maxViewDist / chunkSize);

        //Ensuring the first chunks are drawn
        UpdateVisibleChunks();
    }

    void Update()
    {
        //Finding the viewer position and changing the visible chunks where necessary
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z) / scale;

        if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrviewerMoveThresholdChunkUpdate)
        {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
        }


        
    }

    void UpdateVisibleChunks()
    {
        //Going through the chunks that visible in the last update, then disabling them
        for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++)
        {
            terrainChunksVisibleLastUpdate[i].SetVisible(false);
        }

        //Clearing the list
        terrainChunksVisibleLastUpdate.Clear();


        //Getting the coordinate that the player is currently standing on
        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        //Looping through the surrounding chunks
        for (int yOffset = -chunksVisibleInViewDistance; yOffset <= chunksVisibleInViewDistance; yOffset++)
        {
            for (int xOffset = -chunksVisibleInViewDistance; xOffset <= chunksVisibleInViewDistance; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                //Keeping a dictionary of chunk coordinates to prevent duplicates
                if (terrainChunkDictionary.ContainsKey (viewedChunkCoord))
                {
                    //Updating the dictionary if the viewed chunk has already been seen
                    terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();

                } else {
                    //If the terrain chunk is not in the dictionary, add this new chunk to the dictionary
                    terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, detailLevels, transform, mapMaterial));
                }
            }
        }
    }

    public class TerrainChunk
    {
        //Getting the position of the player
        Vector2 position;

        //Keeping track of the meshes
        GameObject meshObject;

        //Can find the point on the perimeter that is closest to another point using Bounds struct
        Bounds bounds;

        //Generating mesh renderers and mesh filters to allow meshes to created
        MeshRenderer meshRenderer;
        MeshFilter meshFilter;

        //Creating arrays to allow for dynamic LODs
        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;

        MapData mapData;
        bool mapDataReceived;

        //Setting an int to keep track of the previous LOD index, to prevent chunks being updated when they don't need to be
        int previousLODIndex = -1;

        //Constructor
        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material)
        {
            this.detailLevels = detailLevels;
            position = coord * size;

            //Setting out the bounds and finding the smallest square distance
            bounds = new Bounds(position, Vector2.one * size);

            //Using a Vector3 as this is being created in 3D space
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            //Instantiating a new game object object
            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshRenderer.material = material;

            meshObject.transform.position = positionV3 * scale;
            meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one * scale;

            //Setting the default state of the chunk to be invisible
            SetVisible(false);

            //Generating a new LOD mesh array
            lodMeshes = new LODMesh[detailLevels.Length];

            //Looping through the lodMeshes array
            for (int i = 0; i < detailLevels.Length; i++)
            {
                lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateTerrainChunk);
            }

            mapGenerator.RequestMapData(position, OnMapDataReceived);
        }

        void OnMapDataReceived (MapData mapData)
        {
            this.mapData = mapData;
            mapDataReceived = true;

            Texture2D texture = TextureGenerator.TextureFromColourMap(mapData.colourMap, MapGenerator.mapChunkSize, MapGenerator.mapChunkSize);
            meshRenderer.material.mainTexture = texture;

            UpdateTerrainChunk();
        }

        void OnMeshDataReceived (MeshData meshData)
        {
            meshFilter.mesh = meshData.CreateMesh();
        }

        //Setting an Update method to check whether the mesh is within the max view distance
        //If above the maxViewDist then the mesh should be disabled, below maxViewDist the mesh is enabled
        public void UpdateTerrainChunk()
        {
            //Is only useful if the mapData has actually been received, therefore the large if statement
            if (mapDataReceived)
            {
                //Finding the distance to the nearest bound, with sqrt as we do not need the SqrDistance
                float viewerDistanceFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));

                //Setting whether the mesh will be visible based on the maxViewDist
                bool visible = viewerDistanceFromNearestEdge <= maxViewDist;

                if (visible)
                {
                    int lodIndex = 0;

                    //Don't need to look at the last one as the visible bool will always be false
                    for (int i = 0; i < detailLevels.Length - 1; i++)
                    {
                        //If the distance to the next chunk is outside a certain distance, increase the LOD level
                        if (viewerDistanceFromNearestEdge > detailLevels[i].visibleDistanceThreshold)
                        {
                            lodIndex = i + 1;
                        } //If inside the set distance, the LOD level is correct, therefore will break out of the loop
                        else
                        {
                            break;
                        }
                    }

                    if (lodIndex != previousLODIndex)
                    {
                        LODMesh lodMesh = lodMeshes[lodIndex];

                        if (lodMesh.hasMesh)
                        {
                            previousLODIndex = lodIndex;
                            meshFilter.mesh = lodMesh.mesh;
                        }
                        else if (!lodMesh.hasRequestedMesh)
                        {
                            lodMesh.RequestMesh(mapData);
                        }
                    }

                    terrainChunksVisibleLastUpdate.Add(this);
                }

                //Defining whether the meshObject is visible or not
                SetVisible(visible);
            }
        }

        //Method to set the active state of the mesh depending on the visible bool defined in Update
        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }

        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }



    }

    //Class used to create the array of LODs for each mesh, and will be used to fetch the mesh from the map generator
    class LODMesh
    {
        //Variables
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        int lod;
        System.Action updateCallback;

        //Constructor
        public LODMesh (int lod, System.Action updateCallback)
        {
            this.lod = lod;
            this.updateCallback = updateCallback;
        }

        void OnMeshDataReceived (MeshData meshData)
        {
            //Creating the mesh, and then confirming it through the bool
            mesh = meshData.CreateMesh();
            hasMesh = true;

            updateCallback();
        }

        //Requesting the mesh when required
        public void RequestMesh (MapData mapData)
        {
            hasRequestedMesh = true;

            mapGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived);
        }
    }

    [System.Serializable] //in order to show up in the inspector
    public struct LODInfo
    {
        public int lod;
        //Float saying where the visible distance of the active LOD is - outside this distance the LOD will switch to a lower resolution LOD
        public float visibleDistanceThreshold;
    }




}
