using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

[CustomEditor(typeof(HexGrid))]
public class HexGridInspector : Editor
{
    bool mapGenerationFoldout = true;
    bool mountainGenerationFoldout = false;

    // map generation parameters
    float precipitationScale = 2f;
    float precipitationExponent = 2f;
    float altitudeScale = 5f;
    float altitudeExponent = 1.7f;
    int altitudeAmplitudes = 4;
    float temperatureScale = 2f;
    float temperatureExponent = 1.7f;

    bool generatePrecipitationMap = true;
    bool generateAltitudeMap = true;
    bool generateTemperatureMap = true;
    bool generateRivers = false;
    bool generateElevationFeatures = false;

    // mountain generation parameters
    float mountainScale = 2f;
    float mountainXScale = 3.5f;
    float mountainYScale = 0.8f;
    int mountainAngle = 90;
    int mountainRangeCount = 1;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();        
        HexGrid grid = (HexGrid)target;

        mapGenerationFoldout = EditorGUILayout.Foldout(mapGenerationFoldout, "Map Generation", true);
        if (mapGenerationFoldout)
        {
            precipitationScale = EditorGUILayout.Slider("Precipitation scale", precipitationScale, 1f, 10f);
            precipitationExponent = EditorGUILayout.Slider("Precipitation exponent", precipitationExponent, 0.1f, 5f);

            altitudeScale = EditorGUILayout.Slider("Altitude scale", altitudeScale, 1f, 10f);
            altitudeExponent = EditorGUILayout.Slider("Altitude exponent", altitudeExponent, 0.1f, 5f);
            altitudeAmplitudes = EditorGUILayout.IntSlider("Altitude amplitudes", altitudeAmplitudes, 1, 10);

            temperatureScale = EditorGUILayout.Slider("Temperature scale", temperatureScale, 1f, 10f);
            temperatureExponent = EditorGUILayout.Slider("Temperature exponent", temperatureExponent, 0.1f, 5f);


            generatePrecipitationMap = EditorGUILayout.Toggle("Precipitation map", generatePrecipitationMap);
            generateAltitudeMap = EditorGUILayout.Toggle("Altitude map", generateAltitudeMap);
            generateTemperatureMap = EditorGUILayout.Toggle("Temperature map", generateTemperatureMap);
            generateRivers = EditorGUILayout.Toggle("Generate Rivers", generateRivers);
            generateElevationFeatures = EditorGUILayout.Toggle("Generate Elevation Features", generateElevationFeatures);
            //warmPoles = EditorGUILayout.Toggle("Warm poles", warmPoles);

            if (GUILayout.Button("Generate Grid")) { generatedGrid(grid); }

            if (GUILayout.Button("Clear Grid")) { clearGrid(grid); }

            if (GUILayout.Button("Generate Noisemaps"))
            {
                Dictionary<(int, int, int), float> precipitationMap, altitudeMap, temperatureMap;
                precipitationMap = altitudeMap = temperatureMap = null;
                if (generatePrecipitationMap) { precipitationMap = MapGeneration.GeneratePrecipitationMap(grid, scale: this.precipitationScale, exponent: this.precipitationExponent); }
                if (generateAltitudeMap)
                {
                    altitudeMap = MapGeneration.GenerateAltitudeMap(grid, scale: this.altitudeScale, exponent: this.altitudeExponent,
                                                  amplitudeCount: this.altitudeAmplitudes, generateElevationFeatures: this.generateElevationFeatures);
                }

                if (generateTemperatureMap)
                {
                    temperatureMap = MapGeneration.GenerateTemperatureMap(grid, scale: this.temperatureScale, exponent: this.temperatureExponent);
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

                if (generateElevationFeatures) { MapGeneration.GenerateElevationFeatures(grid, altitudeMap); }

                foreach (HexTile tile in grid.tiles.Values)
                {
                    (int, int, int) coordinates = tile.GetCoordinates().ToTuple();

                    float precipitation, temperature, altitude;
                    precipitation = temperature = altitude = 0f;
                    if (generatePrecipitationMap) { precipitation = precipitationMap[coordinates]; }
                    if (generateAltitudeMap) { altitude = altitudeMap[coordinates]; }
                    if (generateTemperatureMap) { temperature = temperatureMap[coordinates]; }
                    tile.SetBiomeAttributes(precipitation, altitude, temperature);

                    if (precipitation > 1f || altitude > 1f || temperature > 1f) // tiles that break the 0 - 1 normalization are coloured pure white
                    {
                        tile.GetComponentInChildren<MeshRenderer>().material.color = new Color(1f, 1f, 1f);
                    }
                    else
                    {
                        if (altitude <= 0.175f && generateAltitudeMap) // water level, might need tweaking
                        {
                            Color colorBlend = Color.Lerp(new Color(0.5294118f, 0.8078432f, 0.9215687f), new Color(0f, 0f, 0.5450981f), 1f - altitude / 0.175f);
                            tile.GetComponentInChildren<MeshRenderer>().material.color = colorBlend;
                        }
                        else if (altitude >= 0.7f && generateAltitudeMap)
                        {
                            if (altitude >= 0.9f && generateAltitudeMap)
                            {
                                tile.GetComponentInChildren<MeshRenderer>().material.color = new Color(1f, 1f, 1f);
                            }
                            else if (altitude >= 0.8f && generateAltitudeMap)
                            {
                                tile.GetComponentInChildren<MeshRenderer>().material.color = new Color(0.8f, 0.8f, 0.8f);
                            }
                            else
                            {
                                tile.GetComponentInChildren<MeshRenderer>().material.color = new Color(0.6f, 0.6f, 0.6f);
                            }
                        }
                        else
                        {
                            tile.GetComponentInChildren<MeshRenderer>().material.color = new Color(temperature, altitude, precipitation);
                            //tile.GetComponentInChildren<MeshRenderer>().material.color = new Color(1f - altitude, altitude, 0f);
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

        mountainGenerationFoldout = EditorGUILayout.Foldout(mountainGenerationFoldout, "Mountain Generation", true);
        if (mountainGenerationFoldout)
        {
            mountainScale = EditorGUILayout.Slider("Mountain scale", mountainScale, 1f, 10f);
            mountainXScale = EditorGUILayout.Slider("Mountain X scale", mountainScale, 0.1f, 10f);
            mountainYScale = EditorGUILayout.Slider("Mountain Y scale", mountainScale, 0.1f, 10f);
            mountainAngle = EditorGUILayout.IntSlider("Mountain angle", mountainAngle, 1, 360);
            mountainRangeCount = EditorGUILayout.IntSlider("Mountain range count", mountainRangeCount, 1, 10);

            if (GUILayout.Button("Generate Grid")) { generatedGrid(grid); }

            if (GUILayout.Button("Clear Grid")) { clearGrid(grid); }

            if (GUILayout.Button("Generate Mountains"))
            {
                HexTile[] tiles = grid.GetTiles();

                for(int i = 0; i < mountainRangeCount; i++)
                {
                    float angle = mountainAngle * (i + 1); // makes following mountain ranges rotate relative to the initial mountain range
                    Dictionary<(int, int, int), float> mountainMask = MapGeneration.GenerateNoiseMap(grid: grid, angle: angle, xScale: mountainXScale, yScale: mountainYScale);

                    foreach (HexTile tile in tiles)
                    {
                        (int, int, int) key = tile.GetCoordinates().ToTuple();
                        float altitude = tile.GetAltitude();
                        altitude = Mathf.Max(altitude, mountainMask[key]);

                        tile.SetAltitude(altitude);
                    }
                }

                foreach (HexTile tile in tiles)
                {
                    float altitude = tile.GetAltitude();
                    if (altitude > 1f) // tiles that break the 0 - 1 normalization are coloured pure white
                    {
                        tile.GetComponentInChildren<MeshRenderer>().material.color = new Color(1f, 1f, 1f);
                    }
                    else
                    {
                        if (altitude <= 0.175f) // water level, might need tweaking
                        {
                            Color colorBlend = Color.Lerp(new Color(0.5294118f, 0.8078432f, 0.9215687f), new Color(0f, 0f, 0.5450981f), 1f - altitude / 0.175f);
                            tile.GetComponentInChildren<MeshRenderer>().material.color = colorBlend;
                        }
                        else if (altitude >= 0.7f)
                        {
                            if (altitude >= 0.9f)
                            {
                                tile.GetComponentInChildren<MeshRenderer>().material.color = new Color(1f, 1f, 1f);
                            }
                            else if (altitude >= 0.8f)
                            {
                                tile.GetComponentInChildren<MeshRenderer>().material.color = new Color(0.8f, 0.8f, 0.8f);
                            }
                            else
                            {
                                tile.GetComponentInChildren<MeshRenderer>().material.color = new Color(0.6f, 0.6f, 0.6f);
                            }
                        }
                        else
                        {
                            tile.GetComponentInChildren<MeshRenderer>().material.color = new Color(0f, altitude, 0f);
                        }
                    }
                }
            }
        }
    }

    static void generatedGrid(HexGrid grid)
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

    static void clearGrid(HexGrid grid)
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
}
