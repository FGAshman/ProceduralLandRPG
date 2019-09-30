using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//Include using UnityEditor
using UnityEditor;

//Stating that the editor is a CustomEditor to allow for the button to be visible - and is type MapGenerator to allow it to create a map once pressed 
[CustomEditor (typeof(MapGenerator))]

public class MapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MapGenerator mapGen = (MapGenerator)target;

        //If any value is changed then auto update
        if (DrawDefaultInspector())
        {
            //if autoUpdate is set to true
            if (mapGen.autoUpdate)
            {
                mapGen.DrawMapInEditor();
            }
        }

        //Adding in a button which can generate a map using code from MapGenerator.cs
        if (GUILayout.Button ("Generate"))
        {
            mapGen.DrawMapInEditor();
        }
    }
}
