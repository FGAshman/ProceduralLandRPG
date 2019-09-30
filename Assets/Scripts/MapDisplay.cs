using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapDisplay : MonoBehaviour
{
    //Reference to the renderer of the map
    public Renderer textureRender;

    //Reference to meshfilter and mesh renderer
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    public void DrawTexture (Texture2D texture)
    {
        //Allowing to view the texture outside of the game screen
        textureRender.sharedMaterial.mainTexture = texture;

        //Set the size of the plane as the same as the map
        textureRender.transform.localScale = new Vector3(texture.width, 1, texture.height);
    }

    public void DrawMesh (MeshData meshData, Texture2D texture)
    {
        //Mesh must be shared as there might be mesh generation outside of game mode
        meshFilter.sharedMesh = meshData.CreateMesh();
        meshRenderer.sharedMaterial.mainTexture = texture;
    }
}
