using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    //Enum used for measuring the local min/max, or global min/max
    public enum NormalizeMode {Local, Global}

    //Method to generate the noise map
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset, NormalizeMode normalizeMode)
    {
        //Creating a 2D float array (noise map) with the defined dimensions
        float[,] noiseMap = new float[mapWidth, mapHeight];

        //Creating a seed with random number generator
        System.Random prng = new System.Random(seed);

        Vector2[] octaveOffsets = new Vector2[octaves];

        float maxPossibleHeight = 0;
        float amplitude = 1;
        float frequency = 1;

        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-10000, 10000) + offset.x;
            float offsetY = prng.Next(-10000, 10000) - offset.y;

            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= persistance;
        }


        //Clamping the scale value to remove divide by 0 errors
        if (scale <= 0)
        {
            scale = 0.0001f;
        }

        //Used for normalising the noise map at the end of the loops
        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;

        //Calculating the values of the centre of the map to allow for the noise scale to zoom in on the centre (default top right)
        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;


        //Looping over the noise map
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;

                //Looping through all the octaves
                for (int i = 0; i < octaves; i++)
                {
                    //Setting up float values to sample over 
                    //Higher frequency means the height points will change more rapidly
                    float sampleX = (x - halfWidth + octaveOffsets[i].x) / scale * frequency;
                    float sampleY = (y - halfHeight + octaveOffsets[i].y) / scale * frequency;

                    //Generating the Perlin Value (Perlin noise is coherent noise - looks more like mountains)
                    //Multiply by 2 and -1 to give negative perlinValues
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    //Amplitude and frequecny increase after each octave as persistance and lacunarity are > 1
                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                //Updating the min/maxNoiseHeight where necessary
                if (noiseHeight > maxLocalNoiseHeight)
                {
                    maxLocalNoiseHeight = noiseHeight;
                } else if (noiseHeight < minLocalNoiseHeight)
                {
                    minLocalNoiseHeight = noiseHeight;
                }

                noiseMap[x, y] = noiseHeight;
                 
            }
        }

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                //Returns a value between 0 - 1 to normalise the noiseMap
                // minNoiseHeight returns 0, maxNoiseHeight returns 1
                if (normalizeMode == NormalizeMode.Local)
                {
                    //Ideal method for non endless terrains
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                } else
                {
                    float normalizedHeight = (noiseMap[x, y] + 1) /(2f * maxPossibleHeight/2.25f);
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
                }
            }
        }


        return noiseMap;
    }
}
