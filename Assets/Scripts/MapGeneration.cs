using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

/*
this class is going to contain different types of map generation, just to see what works and what doesn't
things to look into: cellular automata, perlin noise, simplex noise, and diamond square
*/

//TODO: add functions for adding terrain features, and actual objects along with a function for adding a water boundary around the map
//      will probably also need to add post- and preprocessing functions for the map generation to make it look nicer and less fractal

public static class MapGeneration
{
    //cellular automata variables
    const float noiseDensity = 0.5f;
    const int iterations = 3;

    public static void GenerateCellularAutomataMap(HexGrid grid)
    {
        Dictionary<(int, int, int), float> precipitationMap, altitudeMap, temperatureMap;
        precipitationMap = GenerateNoiseMap(grid, scale: 2f, exponent: 2f);

        altitudeMap = GenerateNoiseMap(grid, scale: 2f, exponent: 2f);

        temperatureMap = GenerateNoiseMap(grid, scale: 2f, exponent: 2f);
        temperatureMap = TemperatureRefinementPass(grid, temperatureMap, altitudeMap);


        //probably will need to be rewritten
        foreach (HexTile tile in grid.tiles.Values)
        {
            (int, int, int) coordinates = tile.GetCoordinates().ToTuple();
            float precipitation = precipitationMap[coordinates];
            float altitude = altitudeMap[coordinates];
            float temperature = temperatureMap[coordinates];
            tile.SetBiomeAttributes(precipitation, altitude, temperature);
        }

        GenerateTerrainFeatures(grid, altitudeMap);
    }

    //map generation presets could potentially be expressed as these parameter values
    //i.e. a flatlands map would have a high exponent with low scale
    public static Dictionary<(int, int, int), float> GenerateNoiseMap(HexGrid grid, float scale = 2f, float exponent = 2f, float fudgeFactor = 1.2f, bool useSimplex = false)
    {
        Dictionary<(int, int, int), float> noiseMap = new Dictionary<(int, int, int), float>();
        float[] amplitudes = {1f, 1f/2f, 1f/4f, 1/8f};
        int coordinateOffset = Mathf.CeilToInt(Mathf.Sqrt(Mathf.Pow(grid.height, 2f) + Mathf.Pow(grid.width, 2f)) / 2f); // this ensures that the coordinates stay positive without them mirroring through absolute
        float offsetQShared = Random.Range(0f, 100f);
        float offsetRShared = Random.Range(0f, 100f);
        //offsetQShared = offsetRShared = 0f;
        
        foreach (HexTile tile in grid.tiles.Values)
        {
            HexCoordinates coordinates = tile.GetCoordinates();
            float qn = (coordinates.q + coordinateOffset) / (float) grid.width * scale;
            float rn = (coordinates.r + coordinateOffset) / (float) grid.height * scale;
            float sample = 0f;

            //a separate pass for each of the amplitudes
            foreach (float amplitude in amplitudes)
            {
                float offsetQ = Random.Range(0f, 0.3f / amplitude);
                float offsetR = Random.Range(0f, 0.3f / amplitude);
                float coordX = (qn * (1f / amplitude)) + offsetQ + offsetQShared;
                float coordY = (rn * (1f / amplitude)) + offsetR + offsetRShared;

                if (useSimplex)
                {
                    //simplex
                    var noiseCoords = new Unity.Mathematics.float2(coordX, coordY);
                    sample += amplitude * Unity.Mathematics.noise.snoise(noiseCoords);
                } 
                else
                {
                    //perlin
                    sample += amplitude * Mathf.PerlinNoise(coordX, coordY);
                }               
            }

            sample /= (amplitudes.Sum() * fudgeFactor);             //normalize the values to a 0.0 - 1.0 range
            sample = Mathf.Pow(sample * fudgeFactor, exponent);     //raise the sample value to the power of exponent to make the noise values less linear
            noiseMap.Add(coordinates.ToTuple(), sample);
        }

        return noiseMap;
    }

    //varies tile temperatures based on pole proximity and elevation
    public static Dictionary<(int, int, int), float> TemperatureRefinementPass(HexGrid grid, Dictionary<(int, int, int), float> temperatureMap, Dictionary<(int, int, int), float> altitudeMap, bool warmPoles = false)
    {
        float poleTemp = 0.7f;
        float equatorTemp = 1f;
        if (warmPoles) { poleTemp = 1f; equatorTemp = 0.7f; }

        foreach (KeyValuePair<(int, int, int), float> entry in altitudeMap)
        {
            float altitude = entry.Value;
            float temperature = temperatureMap[entry.Key];
            int rCoord = entry.Key.Item2;
            int sCoord = entry.Key.Item3;

            float distanceFromEquator = Mathf.Abs(rCoord - sCoord) / 2f;
            float sine = 1f - Mathf.Sin(Mathf.PI * (distanceFromEquator / grid.height));

            float newTemperature = temperature * Mathf.Lerp(poleTemp, equatorTemp, sine);
            newTemperature = Mathf.Lerp(newTemperature, 1f - altitude, Mathf.Pow(altitude, 1.75f));

            temperatureMap[entry.Key] = newTemperature;
        }

        return temperatureMap;
    }

    public static void GenerateTerrainFeatures(HexGrid grid, Dictionary<(int, int, int), float> altitudeMap)
    {
        //GenerateMountains(grid)
        GenerateRivers(grid, altitudeMap);
        //GenerateForests(grid)
    }

    public static void GenerateRivers(HexGrid grid, Dictionary<(int, int, int), float> altitudeMap, int riverCount = 3, float minimumAltitude = 0.5f)
    {
        List<HexTile> riverStartingPointCandidates = new List<HexTile>();
        List<HexTile>[] rivers = new List<HexTile>[riverCount];
        System.Random rand = new System.Random();

        // this can still fail to find any starting points
        while (riverStartingPointCandidates.Count < riverCount * 2)
        {
            foreach (KeyValuePair<(int, int, int), float> entry in altitudeMap)
            {
                float altitude = entry.Value;

                if (altitude > minimumAltitude)
                {
                    HexTile tile = grid.FetchTile(entry.Key);
                    riverStartingPointCandidates.Add(tile);
                }
            }

            // Debug.Log(minimumAltitude);
            minimumAltitude =- 0.1f;
        }

        for (int i = 0; i < riverCount; i++)
        {
            // Debug.Log(riverStartingPointCandidates.Count);
            int n = rand.Next(riverStartingPointCandidates.Count);
            HexTile tile = riverStartingPointCandidates[n];
            List<HexTile> newRiver = RiverRecursion(tile);
            rivers[i] = newRiver;

            riverStartingPointCandidates.Remove(tile);
        }

        grid.rivers = rivers;
    }

    // river searching will probably need to do searching for low points in a bigger range to avoid getting stuck in local minima
    // or alternatively when stuck in local minima make it into a lake and see if you can't derive further rivers from that
    private static List<HexTile> RiverRecursion(HexTile tile, List<HexTile> riverTiles = null)
    {
        float lowestAltitude = tile.GetAltitude();
        HexTile newTile = null;
        if (riverTiles == null) { riverTiles = new List<HexTile>(); }
        riverTiles.Add(tile);

        foreach (HexTile neighbor in tile.GetNeighbors())
        {
            if (riverTiles.Contains(neighbor)) { continue; } // checks if the new tile is already a part of the same river
            float newAltitude = neighbor.GetAltitude();
            if (newAltitude * 0.9f < lowestAltitude)
            {
                newTile = neighbor;
                lowestAltitude = newAltitude;
            }
        }

        if (lowestAltitude == tile.GetAltitude())
        {
            return riverTiles;
        } 
        else
        {
            return RiverRecursion(newTile, riverTiles);
        }
    }

    public static void AssignTerrains(HexGrid grid)
    {
        if (grid.terrainList == null)
        {
            //grid.ReadTerrainTypes();
        }

        foreach (HexTile tile in grid.tiles.Values)
        {
            float precipitation = tile.GetPrecipitation();
            float altitude = tile.GetAltitude();
            float temperature = tile.GetTemperature();

            if (altitude < 0.2f)
            {
                tile.SetTerrain(DefaultTerrains.water);
            }
            else if (altitude < 0.8f) // revisit this number
            {
                if (temperature > 0.8f)
                {
                    if (precipitation > 0.63f)
                    {
                        tile.SetTerrain(DefaultTerrains.tropicalRainforest);
                    }
                    else if (precipitation > 0.188f)
                    {
                        tile.SetTerrain(DefaultTerrains.savanna);
                    }
                    else
                    {
                        tile.SetTerrain(DefaultTerrains.desert);
                    }
                }
                else if (temperature > 0.56f)
                {
                    if (precipitation > 0.8f)
                    {
                        tile.SetTerrain(DefaultTerrains.error);
                    }
                    else if (precipitation > 0.5f)
                    {
                        tile.SetTerrain(DefaultTerrains.temperateRainforest);
                    }
                    else if (precipitation > 0.25f)
                    {
                        tile.SetTerrain(DefaultTerrains.temperateForest);
                    }
                    else if (precipitation > 0.125f)
                    {
                        tile.SetTerrain(DefaultTerrains.woodland);
                    }
                    else
                    {
                        tile.SetTerrain(DefaultTerrains.grassland);
                    }
                }
                else if (temperature > 0.4f)
                {
                    if (precipitation > 0.5f)
                    {
                        tile.SetTerrain(DefaultTerrains.error);
                    }
                    else if (precipitation > 0.125f)
                    {
                        tile.SetTerrain(DefaultTerrains.borealForest);
                    }
                    else if (precipitation > 0.05f)
                    {
                        tile.SetTerrain(DefaultTerrains.woodland);
                    }
                    else
                    {
                        tile.SetTerrain(DefaultTerrains.grassland);
                    }
                } 
                else if (temperature > 0.24f)
                {
                    tile.SetTerrain(DefaultTerrains.tundra);
                }
                else
                {
                    tile.SetTerrain(DefaultTerrains.arctic);
                }
            }
            else
            {
                tile.SetTerrain(DefaultTerrains.mountain);
            }
        }
    }
}