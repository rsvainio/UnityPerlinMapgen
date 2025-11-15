using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HexGrid))]
public class HexGridInspector : Editor
{
    bool mapGenerationFoldout = true;
    bool mountainGenerationFoldout = false;

    int customSeed = 0;

    // map generation parameters
    float precipitationScale = 2f;
    float precipitationExponent = 2f;
    float altitudeScale = 7f; // these values are tuned with elevation feature generation in mind
    float altitudeExponent = 2f; // these values are tuned with elevation feature generation in mind
    int altitudeAmplitudes = 4;
    float temperatureScale = 2f;
    float temperatureExponent = 1.25f;

    bool generatePrecipitationMap = false;
    bool generateAltitudeMap = true;
    bool generateTemperatureMap = false;
    bool generateRivers = false;
    bool generateElevationFeatures = false;

    float cellularAutomataBoundary = 0f;

    // mountain generation parameters
    float mountainScale = 7f;
    float mountainXScale = 2f;
    float mountainYScale = 0.3f;
    int mountainAngle = 90;
    float mountainExponent = 0.8f;
    int mountainRangeCount = 3;

    bool singleColor = true;
    bool averageElevationFeatures = false;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();        
        HexGrid grid = (HexGrid)target;

        customSeed = EditorGUILayout.IntField("Custom seed", customSeed);

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
            generateRivers = EditorGUILayout.Toggle("Generate rivers", generateRivers);
            generateElevationFeatures = EditorGUILayout.Toggle("Generate elevation features", generateElevationFeatures);
            //warmPoles = EditorGUILayout.Toggle("Warm poles", warmPoles);

            cellularAutomataBoundary = EditorGUILayout.Slider("Cellular automata boundary", cellularAutomataBoundary, 0f, 1f);

            if (GUILayout.Button("Generate Grid")) { GenerateGrid(grid); }

            if (GUILayout.Button("Clear Grid")) { ClearGrid(grid); }

            if (GUILayout.Button("Generate Noisemaps"))
            {
                if (customSeed != 0) { UnityEngine.Random.InitState(customSeed); }

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
                        temperatureMap = MapGeneration.DoTemperatureRefinementPass(grid, temperatureMap, altitudeMap);
                    }
                    catch (NullReferenceException)
                    {
                        //generateTemperatureMap = false;
                        //temperatureMap = null;
                        UnityEngine.Debug.Log("No altitude map generated, skipping temperature refinement pass");
                    }
                }

                
                foreach (HexTile tile in grid.GetTiles())
                {
                    (int, int, int) coordinates = tile.GetCoordinates().ToTuple();

                    float precipitation, temperature, altitude;
                    precipitation = temperature = altitude = 0f;
                    if (generatePrecipitationMap) { precipitation = precipitationMap[coordinates]; }
                    if (generateAltitudeMap) { altitude = altitudeMap[coordinates]; }
                    if (generateTemperatureMap) { temperature = temperatureMap[coordinates]; }
                    tile.SetBiomeAttributes(precipitation, altitude, temperature);

                    Material tileMaterial = tile.GetComponentInChildren<Renderer>().material;
                    if (tileMaterial.HasColor("_Color")) { tileMaterial.SetColor("_Color", new Color(0f, 0f, 0f)); }

                    if (generateAltitudeMap)
                    {
                        Color altitudeColor;

                        if (altitude <= grid.waterLevel)
                        {
                            altitudeColor = Color.Lerp(new Color(0.5294118f, 0.8078432f, 0.9215687f), new Color(0f, 0f, 0.5450981f), 1f - altitude / 0.175f);
                        }
                        else if (altitude >= 0.7f)
                        {
                            if (altitude >= 0.9f)
                            {
                                altitudeColor = new Color(1f, 1f, 1f);
                            }
                            else if (altitude >= 0.8f)
                            {
                                altitudeColor = new Color(0.8f, 0.8f, 0.8f);
                            }
                            else
                            {
                                altitudeColor = new Color(0.6f, 0.6f, 0.6f);
                            }
                        }
                        else
                        {
                            altitudeColor = new Color(0f, altitude, 0f);
                        }

                        tileMaterial.SetColor("_Color", altitudeColor);
                    }

                    if (generateTemperatureMap)
                    {
                        Color colorBlend = tileMaterial.GetColor("_Color") + new Color(temperature, 0f, 0f);
                        tileMaterial.SetColor("_Color", colorBlend);
                    }

                    if (generatePrecipitationMap)
                    {
                        Color colorBlend = tileMaterial.GetColor("_Color") + new Color(0f, 0f, precipitation);
                        tileMaterial.SetColor("_Color", colorBlend);
                    }
                }

                //foreach (HexTile tile in grid.GetTiles())
                //{
                //    (int, int, int) coordinates = tile.GetCoordinates().ToTuple();

                //    float precipitation, temperature, altitude;
                //    precipitation = temperature = altitude = 0f;
                //    if (generatePrecipitationMap) { precipitation = precipitationMap[coordinates]; }
                //    if (generateAltitudeMap) { altitude = altitudeMap[coordinates]; }
                //    if (generateTemperatureMap) { temperature = temperatureMap[coordinates]; }
                //    tile.SetBiomeAttributes(precipitation, altitude, temperature);

                //    if (precipitation > 1f || altitude > 1f || temperature > 1f) // tiles that break the 0 - 1 normalization are coloured pure white
                //    {
                //        tile.GetComponentInChildren<MeshRenderer>().material.color = new Color(1f, 1f, 1f);
                //    }
                //    else
                //    {
                //        if (altitude <= grid.waterLevel && generateAltitudeMap) // water level, might need tweaking
                //        {
                //            Color colorBlend = Color.Lerp(new Color(0.5294118f, 0.8078432f, 0.9215687f), new Color(0f, 0f, 0.5450981f), 1f - altitude / 0.175f);
                //            tile.GetComponentInChildren<MeshRenderer>().material.color = colorBlend;
                //        }
                //        else if (altitude >= 0.7f && generateAltitudeMap)
                //        {
                //            if (altitude >= 0.9f && generateAltitudeMap)
                //            {
                //                tile.GetComponentInChildren<MeshRenderer>().material.color = new Color(1f, 1f, 1f);
                //            }
                //            else if (altitude >= 0.8f && generateAltitudeMap)
                //            {
                //                tile.GetComponentInChildren<MeshRenderer>().material.color = new Color(0.8f, 0.8f, 0.8f);
                //            }
                //            else
                //            {
                //                tile.GetComponentInChildren<MeshRenderer>().material.color = new Color(0.6f, 0.6f, 0.6f);
                //            }
                //        }
                //        else
                //        {
                //            tile.GetComponentInChildren<MeshRenderer>().material.color = new Color(temperature, altitude, 1f - temperature);
                //            //tile.GetComponentInChildren<MeshRenderer>().material.color = new Color(temperature, altitude, precipitation);
                //            //tile.GetComponentInChildren<MeshRenderer>().material.color = new Color(1f - altitude, altitude, 0f);
                //        }
                //    }
                //}

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

            if (GUILayout.Button("Cellular automata pass"))
            {
                Dictionary<(int, int, int), float> altitudeMap = new();
                foreach (HexTile tile in grid.GetTiles())
                {
                    altitudeMap.Add(tile.GetCoordinates().ToTuple(), tile.GetAltitude());
                }
                altitudeMap = MapGeneration.DoCellularAutomataPass(grid, altitudeMap, cellularAutomataBoundary, neighborTilesForTransition: 2);

                foreach (HexTile tile in grid.GetTiles())
                {
                    (int, int, int) coordinates = tile.GetCoordinates().ToTuple();
                    float altitude = altitudeMap[coordinates];
                    tile.SetAltitude(altitude);
                    if (altitude <= grid.waterLevel) // water level, might need tweaking
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
                }
            }
        }

        mountainGenerationFoldout = EditorGUILayout.Foldout(mountainGenerationFoldout, "Mountain Generation", true);
        if (mountainGenerationFoldout)
        {
            mountainScale = EditorGUILayout.Slider("Mountain scale", mountainScale, 1f, 10f);
            mountainXScale = EditorGUILayout.Slider("Mountain X scale", mountainXScale, 0.1f, 10f);
            mountainYScale = EditorGUILayout.Slider("Mountain Y scale", mountainYScale, 0.1f, 10f);
            mountainAngle = EditorGUILayout.IntSlider("Mountain angle", mountainAngle, 1, 360);
            mountainExponent = EditorGUILayout.Slider("Mountain exponent", mountainExponent, 0.1f, 4f);
            mountainRangeCount = EditorGUILayout.IntSlider("Mountain range count", mountainRangeCount, 1, 5);

            singleColor = EditorGUILayout.Toggle("Single color mountains", singleColor);
            averageElevationFeatures = EditorGUILayout.Toggle("Elevation features by average", averageElevationFeatures);

            if (GUILayout.Button("Generate Grid")) { GenerateGrid(grid); }

            if (GUILayout.Button("Clear Grid")) { ClearGrid(grid); }

            if (GUILayout.Button("Generate Mountain Ranges"))
            {
                if (customSeed != 0) { UnityEngine.Random.InitState(customSeed); }

                HexTile[] tiles = grid.GetTiles();
                Color[] mountainRangeColors = {Color.green, Color.red, Color.blue, Color.yellow, Color.purple};

                for(int i = 0; i < mountainRangeCount; i++)
                {
                    float angle = mountainAngle * (i + 1); // makes following mountain ranges rotate relative to the initial mountain range
                    Dictionary<(int, int, int), float> mountainMask = MapGeneration.GenerateNoiseMap(grid: grid, angle: angle, xScale: mountainXScale, yScale: mountainYScale, scale: mountainScale, exponent: mountainExponent);

                    foreach (HexTile tile in tiles)
                    {
                        (int, int, int) key = tile.GetCoordinates().ToTuple();
                        float altitude = tile.GetAltitude();
                        altitude = Mathf.Max(altitude, mountainMask[key]);
                        tile.SetAltitude(altitude);

                        if (singleColor)
                        {
                            if (altitude >= 0.7f)
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
                                tile.GetComponentInChildren<MeshRenderer>().material.color = new Color(1f - altitude, 0f, altitude);
                            }
                        } 
                        else
                        {
                            if (i == 0)
                            {
                                Color newColor = Color.Lerp(Color.gray, mountainRangeColors[i], mountainMask[key]);
                                tile.GetComponentInChildren<Renderer>().material.SetColor("_Color", newColor);
                            }
                            else
                            {
                                Color currentColor = tile.GetComponentInChildren<Renderer>().material.GetColor("_Color");
                                Color newColor = Color.Lerp(currentColor, mountainRangeColors[i], mountainMask[key]);
                                tile.GetComponentInChildren<Renderer>().material.SetColor("_Color", newColor);
                            }
                        }
                    }
                }

                foreach (HexTile tile in tiles)
                {
                    tile.SetAltitude(0f);
                }
            }

            if (GUILayout.Button("Generate Elevation Features"))
            {
                if (customSeed != 0) { UnityEngine.Random.InitState(customSeed); }

                Dictionary<(int, int, int), float> mountainMask = MapGeneration.GenerateNoiseMap(grid, scale: mountainScale, exponent: mountainExponent);
                Dictionary<(int, int, int), float> mixValueMask = MapGeneration.GenerateNoiseMap(grid, scale: 4f, exponent: 2f);
                for (int i = 0; i < mountainRangeCount - 1; i++)
                {
                    Debug.Log($"Generating mountain range {i}");
                    Dictionary<(int, int, int), float> newMountainMask = MapGeneration.GenerateNoiseMap(grid, scale: mountainScale, exponent: mountainExponent);
                    foreach (KeyValuePair<(int, int, int), float> entry in newMountainMask)
                    {
                        (int, int, int) key = entry.Key;
                        if (averageElevationFeatures)
                        { 
                            float tileElevationMask = 1f - Mathf.Abs(entry.Value * 2 - 1.0f);
                            mountainMask[key] += tileElevationMask;
                        }
                        else
                        {
                            //float tileElevationMask = Mathf.Min(mountainMask[key], 1f - Mathf.Abs(entry.Value * 2 - 1.0f));
                            float tileElevationMask = Mathf.Lerp(mountainMask[key], 1f - Mathf.Abs(entry.Value * 2 - 1.0f), mixValueMask[key]);
                            mountainMask[key] = tileElevationMask;
                        }
                    }
                }

                foreach ((int, int, int) key in mountainMask.Keys.ToList())
                {
                    mountainMask[key] = Mathf.Pow(mountainMask[key], 1.5f);
                    if (averageElevationFeatures)
                    {
                        float tileAverageMask = mountainMask[key] /= mountainRangeCount;
                        mountainMask[key] = tileAverageMask > 0.5f ? tileAverageMask : 0f;
                    }
                    else if (mountainMask[key] < 0.6f)
                    {
                        mountainMask[key] = Mathf.Pow(mountainMask[key], 1.5f);
                    }
                }

                //mountainMask = MapGeneration.cellularAutomataPass(grid, mountainMask, 0.6f, neighborTilesForTransition: 5, passes: 1);

                foreach ((int, int, int) key in mountainMask.Keys.ToList())
                {
                    HexTile tile = grid.FetchTile(key);
                    float altitude = mountainMask[key];
                    if (altitude >= 0.7f)
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
                        tile.GetComponentInChildren<MeshRenderer>().material.color = new Color(1f - altitude, 0f, altitude);
                    }
                }
            }
        }
    }

    static void GenerateGrid(HexGrid grid)
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

    static void ClearGrid(HexGrid grid)
    {
        grid.borderTiles.Clear();
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
