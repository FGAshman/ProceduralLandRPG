using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FallOffGenerator
{
    public static float[,] GenerateFalloffMap (int size)
    {
        //Generating a new map
        float[,] map = new float[size, size];

        //Looping through coordinates in the square map
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                //Transforming the values between values of -1 to 1
                float x = i / (float)size * 2 - 1;
                float y = j / (float)size * 2 - 1;

                //Finding whether x or y is closer to the maximum value
                float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));

                map[i, j] = Evaluate(value);
            }
        }

        return map;
    }

    static float Evaluate(float value)
    {
        float a = 3;
        float b = 2.2f;

        //Using an equation to reduce the strength of the falloff Map
        return Mathf.Pow(value, a) / (Mathf.Pow(value, a) + Mathf.Pow(b - b * value, a));
    }
}
