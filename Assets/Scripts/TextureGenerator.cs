using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureGenerator 
{
    public static Texture2D TextureFromColourMap (Color[] colourMap, int width, int height)
    {
        //Creating a new texture
        Texture2D texture = new Texture2D(width, height);

        //Fixing the initial blurriness of the colourMap
        texture.filterMode = FilterMode.Point;

        //Fixing the initial wrapping of the colourMap
        texture.wrapMode = TextureWrapMode.Clamp;

        //Setting the texture as the colours from the colourMap
        texture.SetPixels(colourMap);
        texture.Apply();

        return texture;
    }

    public static Texture2D TextureFromHeightMap(float[,] heightMap)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        //Generating colours for each noise value
        Color[] colourMap = new Color[width * height];

        //Looping through the colour map
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                //Finding the index of the colour map and setting them as a colour between black and white
                colourMap[y * width + x] = Color.Lerp(Color.black, Color.white, heightMap[x, y]);
            }
        }

        return TextureFromColourMap(colourMap, width, height);
    }
}
