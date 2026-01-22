using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Drawing.Drawing2D;
using System.Linq;
using TreeEditor;
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
        MapGeneration mapGenerator = new MapGeneration(grid);
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

            //if (GUILayout.Button("Generate Grid")) { GenerateGrid(grid); mapGenerator = new MapGeneration(grid); }
            //if (GUILayout.Button("Clear Grid")) { ClearGrid(grid); }
            if (GUILayout.Button("Generate Noisemaps"))
            {
                if (grid.GetTiles().Count != grid.height * grid.width)
                {
                    Debug.Log("Tile count differs from grid size, regenerating...");
                    GenerateGrid(grid);
                    mapGenerator = new MapGeneration(grid);
                }
                else
                {
                    grid.ResetGrid(); // reset the grid to make sure previous tile values aren't used in this run
                }

                if (customSeed != 0) { UnityEngine.Random.InitState(customSeed); }
                if (generateAltitudeMap) { mapGenerator.GenerateAltitudeMap(scale: this.altitudeScale, exponent: this.altitudeExponent,
                                            amplitudeCount: this.altitudeAmplitudes, generateElevationFeatures: this.generateElevationFeatures); }
                if (generatePrecipitationMap) { mapGenerator.GeneratePrecipitationMap(scale: this.precipitationScale, exponent: this.precipitationExponent); }
                if (generateTemperatureMap) { mapGenerator.GenerateTemperatureMap(scale: this.temperatureScale, exponent: this.temperatureExponent); }

                if (generateRivers) { grid.rivers = mapGenerator.GenerateRivers(); }

                foreach (HexTile tile in grid.GetTilesArray())
                {
                    //(int, int, int) coordinates = tile.GetCoordinates().ToTuple();

                    float precipitation, temperature, altitude;
                    precipitation = temperature = altitude = 0f;
                    if (generatePrecipitationMap) { precipitation = tile.GetPrecipitation(); }
                    if (generateAltitudeMap) { altitude = tile.GetAltitude(); }
                    if (generateTemperatureMap) { temperature = tile.GetTemperature(); }

                    Material tileMaterial = tile.GetComponentInChildren<Renderer>().material;
                    if (tileMaterial.HasColor("_Color")) { tileMaterial.SetColor("_Color", new Color(0f, 0f, 0f)); }

                    // tile color assignment
                    Color colorBlend = new Color(0f, 0f, 0f);
                    Color altitudeColor = new Color(0f, 0f, 0f);
                    Color temperatureColor = new Color(0f, 0f, 0f);
                    Color precipitationColor = new Color(0f, 0f, 0f);
                    if (generateAltitudeMap)
                    {
                        Terrain terrain = tile.GetTerrain();
                        if (altitude <= grid.waterLevel || terrain == Terrain.FreshWater || terrain == Terrain.Ocean)
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

                        colorBlend += altitudeColor;
                    }
                    if (generateTemperatureMap)
                    {
                        temperatureColor = new Color(temperature * 0.5f, 0f, 0f);
                        colorBlend += temperatureColor;
                    }
                    if (generatePrecipitationMap)
                    {
                        precipitationColor = new Color(0f, 0f, precipitation * 0.5f);
                        colorBlend += precipitationColor;
                    }
                    tileMaterial.SetColor("_Color", colorBlend);
                    //if (altitude <= grid.waterLevel)
                    //{
                    //    tileMaterial.SetColor("_Color", altitudeColor);
                    //}
                    //else
                    //{
                    //    tileMaterial.SetColor("_Color", new Color(1f - precipitation, 0.5f * altitude, precipitation));
                    //}
                }

                if (generateRivers)
                {
                    foreach (List<HexTile> river in grid.rivers)
                    {
                        for (int i = 0; i < river.Count; i++)
                        {
                            float blueShade = Mathf.Lerp(0f, 1f, 1 - ((float)i / river.Count));
                            river[i].GetComponentInChildren<MeshRenderer>().material.color = new Color(0, 0, blueShade);
                        }
                    }
                }

                ////testing rain shadow calculation
                //Vector3 windDirection = HexMetrics.ConvertDegreesToVector(90);
                //Debug.Log($"Wind direction: {windDirection.ToString()}");
                //List<HexTile> sortedTiles = grid.GetTilesArray()
                //.OrderBy(t => Vector3.Dot(t.GetCoordinates().ToVec3(), windDirection))
                //.ToList();

                //int n = 0;
                //foreach (HexTile tile in sortedTiles)
                //{
                //    Material tileMaterial = tile.GetComponentInChildren<Renderer>().material;

                //    float gradient = (float)n / sortedTiles.Count;
                //    Color color = new Color(1f - gradient, 0f, 0.5f + gradient / 2);
                //    n++;

                //    tileMaterial.SetColor("_Color", color);
                //}
            }

            if (GUILayout.Button("Cellular automata pass"))
            {
                Dictionary<(int, int, int), float> altitudeMap = new();
                foreach (HexTile tile in grid.GetTilesArray())
                {
                    altitudeMap.Add(tile.GetCoordinates().ToTuple(), tile.GetAltitude());
                }
                altitudeMap = mapGenerator.DoCellularAutomataPass(altitudeMap, cellularAutomataBoundary, neighborTilesForTransition: 2);

                foreach (HexTile tile in grid.GetTilesArray())
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

            //if (GUILayout.Button("Generate Grid")) { mapGenerator = new MapGeneration(grid); GenerateGrid(grid); }
            //if (GUILayout.Button("Clear Grid")) { mapGenerator = new MapGeneration(grid); ClearGrid(grid); }
            if (GUILayout.Button("Generate Mountain Ranges"))
            {
                if (grid.GetTiles().Count != grid.height * grid.width)
                {
                    Debug.Log("Tile count differs from grid size, regenerating...");
                    GenerateGrid(grid);
                    mapGenerator = new MapGeneration(grid);
                }
                else
                {
                    grid.ResetGrid(); // reset the grid to make sure previous tile values aren't used in this run
                }

                if (customSeed != 0) { UnityEngine.Random.InitState(customSeed); }

                HexTile[] tiles = grid.GetTilesArray();
                Color[] mountainRangeColors = {Color.green, Color.red, Color.blue, Color.yellow, Color.purple};

                for(int i = 0; i < mountainRangeCount; i++)
                {
                    float angle = mountainAngle * (i + 1); // makes following mountain ranges rotate relative to the initial mountain range
                    Dictionary<(int, int, int), float> mountainMask = mapGenerator.GenerateNoiseMap(angle: angle, xScale: mountainXScale, yScale: mountainYScale, scale: mountainScale, exponent: mountainExponent);

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

                Dictionary<(int, int, int), float> mountainMask = mapGenerator.GenerateNoiseMap(scale: mountainScale, exponent: mountainExponent);
                Dictionary<(int, int, int), float> mixValueMask = mapGenerator.GenerateNoiseMap(scale: 4f, exponent: 2f);
                for (int i = 0; i < mountainRangeCount - 1; i++)
                {
                    Debug.Log($"Generating mountain range {i}");
                    Dictionary<(int, int, int), float> newMountainMask = mapGenerator.GenerateNoiseMap(scale: mountainScale, exponent: mountainExponent);
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
                    HexTile tile = grid.GetTiles()[key];
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
        grid.Initialize();
    }

    static void ClearGrid(HexGrid grid)
    {
        grid.DestroyGrid();
    }
}
