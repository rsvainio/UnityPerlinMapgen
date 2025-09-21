using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

[CustomEditor(typeof(HexGrid))]
public class HexGridInspector : Editor
{
    float precipitationScale = 2f;
    float precipitationExponent = 2f;
    float altitudeScale = 3f;
    float altitudeExponent = 1.7f;
    float temperatureScale = 2f;
    float temperatureExponent = 1.7f;

    bool generatePrecipitationMap = true;
    bool generateAltitudeMap = true;
    bool generateTemperatureMap = true;
    bool generateRivers = false;
    //bool warmPoles = false;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        HexGrid grid = (HexGrid)target;

        precipitationScale = EditorGUILayout.Slider("Precipitation scale", precipitationScale, 1f, 10f);
        precipitationExponent = EditorGUILayout.Slider("Precipitation exponent", precipitationExponent, 0.1f, 5f);

        altitudeScale = EditorGUILayout.Slider("Altitude scale", altitudeScale, 1f, 10f);
        altitudeExponent = EditorGUILayout.Slider("Altitude exponent", altitudeExponent, 0.1f, 5f);

        temperatureScale = EditorGUILayout.Slider("Temperature scale", temperatureScale, 1f, 10f);
        temperatureExponent = EditorGUILayout.Slider("Temperature exponent", temperatureExponent, 0.1f, 5f);


        generatePrecipitationMap = EditorGUILayout.Toggle("Precipitation map", generatePrecipitationMap);
        generateAltitudeMap = EditorGUILayout.Toggle("Altitude map", generateAltitudeMap);
        generateTemperatureMap = EditorGUILayout.Toggle("Temperature map", generateTemperatureMap);
        generateRivers = EditorGUILayout.Toggle("Generate Rivers", generateRivers);
        //warmPoles = EditorGUILayout.Toggle("Warm poles", warmPoles);

        if (GUILayout.Button("Generate Grid"))
        {
            if (grid.tiles.Count == 0)
            {
                grid.Initialize();
            }
            else
            {
                grid.RegenerateGrid();
            }
        }

        if (GUILayout.Button("Clear Grid"))
        {
            if (grid.tiles.Count != 0)
            {
                foreach (HexTile tile in grid.tiles.Values)
                {
                    DestroyImmediate(tile.gameObject);
                }
                grid.tiles.Clear();
            }

            foreach (HexTile tile in grid.GetComponentsInChildren<HexTile>())
            {
                DestroyImmediate(tile.gameObject);
            }
        }

        if (GUILayout.Button("Generate Noisemaps"))
        {
            Dictionary<(int, int, int), float> precipitationMap, altitudeMap, temperatureMap;
            precipitationMap = altitudeMap = temperatureMap = null;
            if (generatePrecipitationMap)   { precipitationMap = MapGeneration.GenerateNoiseMap(grid, scale: this.precipitationScale, exponent: this.precipitationExponent); }
            if (generateAltitudeMap)        { altitudeMap = MapGeneration.GenerateNoiseMap(grid, scale: this.altitudeScale, exponent: this.altitudeExponent); }
            if (generateTemperatureMap) {
                temperatureMap = MapGeneration.GenerateNoiseMap(grid, scale: this.temperatureScale, exponent: this.temperatureExponent);
                try
                {
                    temperatureMap = MapGeneration.TemperatureRefinementPass(grid, temperatureMap, altitudeMap);
                } 
                catch (NullReferenceException)
                {
                    //generateTemperatureMap = false;
                    //temperatureMap = null;
                    UnityEngine.Debug.Log("No altitude map generated, skipping temperature refinement pass");
                }
            }

            foreach (HexTile tile in grid.tiles.Values)
            {
                (int, int, int) coordinates = tile.GetCoordinates().ToTuple();

                float precipitation, temperature, altitude;
                precipitation = temperature = altitude = 0f;
                if (generatePrecipitationMap)   { precipitation = precipitationMap[coordinates]; }
                if (generateAltitudeMap)        { altitude = altitudeMap[coordinates]; }
                if (generateTemperatureMap)     { temperature = temperatureMap[coordinates]; }
                tile.SetBiomeAttributes(precipitation, altitude, temperature);

                if (precipitation > 1f || altitude > 1f || temperature > 1f) // tiles that break the 0 - 1 normalization are coloured pure white
                {
                    tile.GetComponentInChildren<MeshRenderer>().material.color = new Color(255, 255, 255);
                }
                else
                {
                    if (altitude < 0.175f && generateAltitudeMap) // water level, might need tweaking
                    {
                        tile.GetComponentInChildren<MeshRenderer>().material.color = new Color(0, 0.7f, 0.9f);
                    }
                    else
                    {
                        tile.GetComponentInChildren<MeshRenderer>().material.color = new Color(temperature, altitude, precipitation);
                    }
                }

                // alternative gradient colouring courtesy of https://www.shadedrelief.com/hypso/hypso.html
                /*
                Color humid = new Color(160 / 255f, 195 / 255f, 177 / 255f);
                Color arid = new Color(237 / 255f, 216 / 255f, 197 / 255f);
                Color blend = Color.Lerp(arid, humid, precipitation);
                Color final = Color.Lerp(blend, new Color(1, 1, 1), altitude);
                tile.GetComponentInChildren<MeshRenderer>().material.color = final;
                */
            }

            if (generateRivers)
            {
                MapGeneration.GenerateRivers(grid);
                foreach (List<HexTile> river in grid.rivers)
                {
                    for (int i = 0; i < river.Count; i++)
                    {
                        float blueShade = Mathf.Lerp(0f, 1f, 1 - ((float)i / river.Count));
                        river[i].GetComponentInChildren<MeshRenderer>().material.color = new Color(0, 0, blueShade);
                    }
                }
            }

            //MapGeneration.AssignTerrains(grid);

            /*foreach (HexTile tile in grid.tiles.Values)
            {
                (int, int, int) coordinates = tile.GetCoordinates().ToTuple();
                float temperature = temperatureMap[coordinates];
                float altitude = altitudeMap[coordinates];
                tile.SetBiomeAttributes(0, altitude, temperature);
                tile.GetComponentInChildren<MeshRenderer>().material.color = new Color(temperature, 0f, 1f - temperature * 0.8f);
            }*/
        }
    }
}
