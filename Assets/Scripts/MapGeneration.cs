using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

/*
this class is going to contain different types of map generation, just to see what works and what doesn't
things to look into: cellular automata, perlin noise, simplex noise, and diamond square
*/

/*
    TODO (maybe): add support for generating maps with multiple landmasses instead of only one central landmass.
        Could do this by placing seeds around the grid randomly which indicate where landmasses will be created.
        These seeds will then be made into landmasses by modifying the water-boundary generation to take into account distance from the nearest landmass seed.

    TODO: implement function for detecting different "regions" of the map, such as oceans, plains, mountain ranges, etc.
        This will need pathfinding to be implemented, likely in the HexTile class.
*/

public class MapGeneration
{
    readonly HexGrid grid;
    public Dictionary<(int, int, int), float> precipitationMap, altitudeMap, temperatureMap;
    private Dictionary<(int, int, int), int> oceanDistanceMap = new();
    private HexCoordinates windDirection;

    public MapGeneration(HexGrid grid)
    {
        this.grid = grid;
    }

    /*
    public static void GenerateCellularAutomataMap(HexGrid grid)
    {
        Dictionary<(int, int, int), float> precipitationMap, altitudeMap, temperatureMap;
        precipitationMap = GenerateNoiseMap(grid, scale: 2f, exponent: 2f);

        altitudeMap = GenerateNoiseMap(grid, scale: 2f, exponent: 2f);

        temperatureMap = GenerateNoiseMap(grid, scale: 2f, exponent: 2f);
        temperatureMap = DoTemperatureRefinementPass(grid, temperatureMap, altitudeMap);

        //probably will need to be rewritten
        foreach (HexTile tile in grid.GetTilesArray())
        {
            (int, int, int) coordinates = tile.GetCoordinates().ToTuple();
            float precipitation = precipitationMap[coordinates];
            float altitude = altitudeMap[coordinates];
            float temperature = temperatureMap[coordinates];
            tile.SetBiomeAttributes(precipitation, altitude, temperature);
        }

        GenerateTerrainFeatures(grid, altitudeMap);
    }
    */

    // should reorganize these to have altitude be the first function as precipitation and temperature generation are dependent on it
    public Dictionary<(int, int, int), float> GeneratePrecipitationMap(float scale = 2f, float exponent = 2f, int amplitudeCount = 4, float fudgeFactor = 1.2f)
    {   
        //precipitationMap = GenerateNoiseMap(scale, exponent);
        precipitationMap = SimulateRainShadow();

        // assign precipitation values to tiles here before returning the altitude map
        foreach (KeyValuePair<(int, int, int), float> entry in precipitationMap)
        {
            grid.FetchTile(entry.Key).SetPrecipitation(entry.Value);
        }

        return precipitationMap;
    }

    // https://www.reddit.com/r/proceduralgeneration/comments/4knask/how_can_i_make_this_terrain_more_interesting_ie/d3gfg4d/
    public Dictionary<(int, int, int), float> GenerateAltitudeMap(float scale = 2f, float exponent = 2f, int amplitudeCount = 4, float fudgeFactor = 1.2f, bool generateElevationFeatures = true)
    {
        altitudeMap = GenerateNoiseMap(scale, exponent / 1.5f, amplitudeCount);
        Dictionary<(int, int, int), float> altitudeUpperBound = GenerateNoiseMap(scale * 3, exponent * 1.5f, amplitudeCount);
        Dictionary<(int, int, int), float> altitudeLerpValue = GenerateNoiseMap(scale: 2f, exponent: 1f, amplitudeCount: 1);

        foreach (KeyValuePair<(int, int, int), float> entry in altitudeLerpValue)
        {
            (int, int, int) key = entry.Key;
            float lowerBound = altitudeMap[key], upperBound = altitudeUpperBound[key];
            altitudeMap[key] = Mathf.Lerp(lowerBound, upperBound, entry.Value);
        }

        if (generateElevationFeatures)
        {
            GenerateElevationFeatures();
        }

        // water boundary generation
        GenerateWaterBoundary();

        altitudeMap = DoCellularAutomataPass(altitudeMap, grid.waterLevel); // water level cellular automata pass
        altitudeMap = DoCellularAutomataPass(altitudeMap, 0.7f, passes: 2); // mountain level cellular automata pass

        // assign altitude values to tiles here before returning the altitude map
        foreach (KeyValuePair<(int, int, int), float> entry in altitudeMap)
        {
            grid.FetchTile(entry.Key).SetAltitude(entry.Value);
        }

        CategorizeWaterTiles(); // elevation shouldn't change after this so we can map water tiles here
        return altitudeMap;
    }

    public Dictionary<(int, int, int), float> GenerateTemperatureMap(float scale = 6f, float exponent = 1.5f, int amplitudeCount = 4, bool temperatureRefinementPass = true)
    {
        temperatureMap = GenerateNoiseMap(scale, exponent);
        if (temperatureRefinementPass)
        {
            if (altitudeMap != null)
            {
                DoTemperatureRefinementPass();
            }
            else
            {
                Debug.Log("No altitude map generated, skipping temperature refinement pass");
            }
        }

        // assign temperature values to tiles here before returning the altitude map
        foreach (KeyValuePair<(int, int, int), float> entry in temperatureMap)
        {
            grid.FetchTile(entry.Key).SetTemperature(entry.Value);
        }

        return temperatureMap;
    }

    //map generation presets could potentially be expressed as these parameter values
    //i.e. a flatlands map would have a high exponent with low scale
    public Dictionary<(int, int, int), float> GenerateNoiseMap(float scale = 2f, float exponent = 2f, int amplitudeCount = 4)
    {
        Dictionary<(int, int, int), float> noiseMap = new Dictionary<(int, int, int), float>();
        float[] amplitudes = new float[amplitudeCount];
        for (int i = 0; i < amplitudeCount; i++) { amplitudes[i] = 1f / Mathf.Pow(3, i); }

        int coordinateOffset = Mathf.CeilToInt(Mathf.Sqrt(Mathf.Pow(grid.height, 2f) + Mathf.Pow(grid.width, 2f)) / 2f); // this ensures that the coordinates stay positive without them mirroring through absolute
        float offsetQShared = Random.Range(0f, 100f);
        float offsetRShared = Random.Range(0f, 100f);

        foreach (HexTile tile in grid.GetTilesArray())
        {
            HexCoordinates coordinates = tile.GetCoordinates();
            float qn = (coordinates.q + coordinateOffset) / (float)grid.width * scale;
            float rn = (coordinates.r + coordinateOffset) / (float)grid.height * scale;
            float sample = 0f;

            //a separate pass for each of the amplitudes
            foreach (float amplitude in amplitudes)
            {
                float offsetQ = Random.Range(0f, 0.3f / amplitude);
                float offsetR = Random.Range(0f, 0.3f / amplitude);
                float coordX = (qn * (1f / amplitude)) + offsetQ + offsetQShared;
                float coordY = (rn * (1f / amplitude)) + offsetR + offsetRShared;

                sample += amplitude * Mathf.PerlinNoise(coordX, coordY);
            }

            float fudgeFactor = 1.2f;
            sample /= (amplitudes.Sum() * fudgeFactor);             //normalize the values to a 0.0 - 1.0 range
            sample = Mathf.Pow(sample * fudgeFactor, exponent);     //raise the sample value to the power of exponent to make the noise values less linear
            noiseMap.Add(coordinates.ToTuple(), sample);
        }

        return noiseMap;
    }

    // currently the noisemaps generated are rotated counter-clockwise by the angle instead of clockwise
    public Dictionary<(int, int, int), float> GenerateNoiseMap(float angle, float xScale, float yScale, float scale = 2f, float exponent = 2f, int amplitudeCount = 4)
    {
        Dictionary<(int, int, int), float> noiseMap = new Dictionary<(int, int, int), float>();
        float[] amplitudes = new float[amplitudeCount];
        for (int i = 0; i < amplitudeCount; i++) { amplitudes[i] = 1f / Mathf.Pow(3, i); }

        int coordinateOffset = Mathf.CeilToInt(Mathf.Sqrt(Mathf.Pow(grid.height, 2f) + Mathf.Pow(grid.width, 2f)) / 2f); // this ensures that the coordinates stay positive without them mirroring through absolute
        float offsetQShared = Random.Range(0f, 100f);
        float offsetRShared = Random.Range(0f, 100f);

        //angle = (Mathf.PI * 180f) / angle;
        angle *= Mathf.Deg2Rad;
        float sin = Mathf.Sin(angle);
        float cos = Mathf.Cos(angle);

        foreach (HexTile tile in grid.GetTilesArray())
        {
            HexCoordinates coordinates = tile.GetCoordinates();
            float qn = (coordinates.q + coordinateOffset) / (float)grid.width * scale;
            float rn = (coordinates.r + coordinateOffset) / (float)grid.height * scale;
            float sample = 0f;

            //a separate pass for each of the amplitudes
            foreach (float amplitude in amplitudes)
            {
                float offsetQ = Random.Range(0f, 0.3f / amplitude);
                float offsetR = Random.Range(0f, 0.3f / amplitude);
                //float coordX = ((qn * cos - rn * sin) * (1f / amplitude)) + offsetQ + offsetQShared;
                //float coordY = ((qn * sin + rn * cos) * (1f / amplitude)) + offsetR + offsetRShared;
                float coordX = ((qn * cos - rn * sin) * (1f / amplitude)) + offsetQ + offsetQShared;
                float coordY = ((qn * sin + rn * cos) * (1f / amplitude)) + offsetR + offsetRShared;

                sample += amplitude * Mathf.PerlinNoise(coordX * xScale, coordY * yScale);
            }

            float fudgeFactor = 1.2f;
            sample /= (amplitudes.Sum() * fudgeFactor);             //normalize the values to a 0.0 - 1.0 range
            sample = Mathf.Pow(sample * fudgeFactor, exponent);     //raise the sample value to the power of exponent to make the noise values less linear
            noiseMap.Add(coordinates.ToTuple(), sample);
        }

        return noiseMap;
    }

    // varies tile temperatures based on pole and ocean proximity and altitude
    private Dictionary<(int, int, int), float> DoTemperatureRefinementPass(bool warmPoles = false)
    {
        float poleTemp = 0.65f;
        float equatorTemp = 1.35f;
        if (warmPoles) { poleTemp = 1.35f; equatorTemp = 0.65f; }
        Dictionary<(int, int, int), float> newTemperatureMap = new();
        float averageTemperature = 0;

        // calculate each tile's distance from the equator and modulate the temperature based on that
        // in addition calculate the average temperature of the entire map before altitudinal temperature modulation
        // so that it can be used to drift the temperature values of tiles closer to the ocean towards the average
        foreach (KeyValuePair<(int, int, int), float> entry in altitudeMap)
        {
            (int, int, int) key = entry.Key;
            float altitude = entry.Value;
            float temperature = temperatureMap[key];
            int rCoord = key.Item2;
            int sCoord = key.Item3;

            float distanceFromEquator = Mathf.Abs(rCoord - sCoord) / 2f;
            distanceFromEquator = 1f - Mathf.Sin(Mathf.PI * (distanceFromEquator / grid.height));
            float newTemperature = temperature * Mathf.Lerp(poleTemp, equatorTemp, distanceFromEquator);

            //newTemperature = Mathf.Pow(newTemperature, 0.75f + altitude);
            //newTemperature *= Mathf.Lerp(1.5f, 0.5f, altitude);

            averageTemperature += newTemperature;
            newTemperatureMap[key] = Mathf.Clamp01(newTemperature);
        }
        averageTemperature /= newTemperatureMap.Count;
        Debug.Log($"Average temperature of the map: {averageTemperature}");

        // modulate the temperature of tiles based on ocean proximity and altitude
        foreach (KeyValuePair<(int, int, int), float> entry in temperatureMap)
        {
            (int, int, int) key = entry.Key;
            HexTile tile = grid.FetchTile(key);
            if (tile.terrain != Terrain.Ocean)
            {
                float originalTemperature = newTemperatureMap[key];
                float maxDistance = ((grid.height + grid.width) / 2) * 0.05f; // the maximum distance from which ocean proximity has an effect on temperature
                float distanceFromNearestOcean = oceanDistanceMap[key] / maxDistance;

                float newTemperature = Mathf.Lerp(originalTemperature, averageTemperature, Mathf.Pow(1f - distanceFromNearestOcean, 2f)); // the exponent will probably need to be tweaked
                newTemperature = Mathf.Lerp(newTemperature / 1.5f, newTemperature * 1.5f, Mathf.Pow(1f - altitudeMap[key], 2f));
                newTemperatureMap[key] = Mathf.Clamp01(newTemperature);
            }
        }

        temperatureMap = newTemperatureMap;
        return newTemperatureMap;
    }

    public static void GenerateTerrainFeatures()
    {
        //GenerateElevationFeatures(grid, altitudeMap);
        //GenerateWaterBoundary(grid, altitudeMap);
        //GenerateRivers(grid);
        //GenerateForests(grid);
        return;
    }

    private Dictionary<(int, int, int), float> GenerateElevationFeatures(int mountainRangeCount = 3, float mountainScale = 7f, float mountainExponent = 1f)
    {
        // mountain range generation
        Dictionary<(int, int, int), float> mountainMask = GenerateNoiseMap(scale: mountainScale, exponent: mountainExponent);
        Dictionary<(int, int, int), float> mixValueMask = GenerateNoiseMap(scale: 4f, exponent: 2f);
        for (int i = 0; i < mountainRangeCount - 1; i++)
        {
            Dictionary<(int, int, int), float> newMountainMask = GenerateNoiseMap(scale: mountainScale, exponent: mountainExponent);
            foreach (KeyValuePair<(int, int, int), float> entry in newMountainMask)
            {
                (int, int, int) key = entry.Key;
                float tileElevationMask = Mathf.Lerp(mountainMask[key], 1f - Mathf.Abs(entry.Value * 2 - 1.0f), mixValueMask[key]);
                mountainMask[key] = Mathf.Clamp01(tileElevationMask);
            }
        }

        foreach (KeyValuePair<(int, int, int), float> entry in altitudeMap)
        {
            (int, int, int) key = entry.Key;
            float tileMountainMask = Mathf.Pow(mountainMask[key], 1.5f);

            float mixValue = Mathf.InverseLerp(grid.waterLevel, grid.waterLevel * 2f, altitudeMap[key]);
            mixValue *= tileMountainMask;
            tileMountainMask = Mathf.Lerp(altitudeMap[key], tileMountainMask + altitudeMap[key], mixValue);
            mountainMask[key] = Mathf.Clamp01(tileMountainMask);
        }

        altitudeMap = mountainMask;
        return altitudeMap;
    }

    private Dictionary<(int, int, int), float> GenerateWaterBoundary()
    {
        Dictionary<(int, int, int), float> newAltitudeMap = GenerateNoiseMap(scale: 8f);
        foreach (KeyValuePair<(int, int, int), float> entry in altitudeMap)
        {
            int qCoord = entry.Key.Item1;
            int rCoord = entry.Key.Item2;
            int sCoord = entry.Key.Item3;
            float xDistance = Mathf.Abs(qCoord - (rCoord + sCoord)) / 2f;
            xDistance = 2 * xDistance / grid.width;
            float yDistance = Mathf.Abs(rCoord - sCoord) / 2f;
            yDistance = 2 * yDistance / grid.height;

            float shaping = Mathf.Pow(Mathf.Cos(xDistance * (Mathf.PI / 2)) * Mathf.Cos(yDistance * (Mathf.PI / 2)), 4f); // https://www.wolframalpha.com/input?i=plot+%28sin%28x*pi%29sin%28y*pi%29%29%5E4+from+0+to+1
            float mixValue = Mathf.Pow(Mathf.Max(xDistance, yDistance), 1.75f);
            mixValue *= 1f - Mathf.Pow(altitudeMap[entry.Key], 8f); // helps to preserve mountains near coasts, the exponent could be tweaked
            mixValue *= Mathf.Lerp(0.75f, 1.25f, newAltitudeMap[entry.Key]); // adds a competing noise layer to the interpolation value
            shaping = Mathf.Lerp(altitudeMap[entry.Key], shaping, mixValue);
            newAltitudeMap[entry.Key] = shaping;
        }

        altitudeMap = newAltitudeMap;
        return newAltitudeMap;
    }

    public Dictionary<(int, int, int), float> SimulateRainShadow()
    {
        Vector3 windDirection = HexMetrics.ConvertDegreesToVector(Random.Range(0, 360));
        Debug.Log($"Wind direction: {windDirection.ToString()}");
        List<HexTile> sortedTiles = grid.GetTilesArray()
            .OrderBy(t => Vector3.Dot(t.GetCoordinates().ToVec3(), windDirection)) // sort tiles so that "upwind" tiles are first
            .ToList();

        // set initial cloud cover values for each tile before simulating the effect of rain shadow
        Dictionary<(int, int, int), float> cloudCoverMap = new Dictionary<(int, int, int), float>();
        foreach (HexTile tile in sortedTiles)
        {
            (int, int, int) key = tile.GetCoordinates().ToTuple();
            float startingCloudCover = 0;
            if (tile.terrain == Terrain.Ocean)
            {
                startingCloudCover = 1f;
            }
            else if (tile.terrain == Terrain.FreshWater)
            {
                startingCloudCover = 0.5f; // this value will need to be tweaked
            }
            else
            {
                startingCloudCover = 0.1f;
                //float maxDistance = ((grid.height + grid.width) / 2) * 0.05f; // the maximum distance beyond which starting precipitation will be 0
                //float distanceFromNearestOcean = oceanDistanceMap[key] / maxDistance;
                //startingPrecipitation = 1f - Mathf.Min(distanceFromNearestOcean, 1f);
            }
            cloudCoverMap[key] = startingCloudCover;
        }

        // simulate rain shadow for each tile
        Dictionary<(int, int, int), float> rainShadowMap = new Dictionary<(int, int, int), float>();
        foreach (HexTile tile in sortedTiles)
        {
            (int, int, int) key = tile.GetCoordinates().ToTuple();
            float tileMaxCloudCover = 1f - tile.GetAltitude();
            float tileCloudCover = Mathf.Min(cloudCoverMap[key], tileMaxCloudCover);
            rainShadowMap[key] = Mathf.Clamp01(tileCloudCover);

            (int, int, int) neighborKey = tile.GetCoordinatesInDirection(windDirection).ToTuple();
            if (grid.GetTiles().TryGetValue(neighborKey, out HexTile downwindTile))
            {
                float altitudeFactor = 1f - Mathf.Pow(tile.GetAltitude(), 1.5f);
                cloudCoverMap[neighborKey] += tileCloudCover * altitudeFactor;
            }
        }

        // simulate diffusion for sideways wind
        Dictionary<(int, int, int), float> averagedRainShadowMap = new Dictionary<(int, int, int), float>();
        foreach (HexTile tile in sortedTiles)
        {
            (int, int, int) key = tile.GetCoordinates().ToTuple();
            float precipitationSum = rainShadowMap[key];
            foreach (HexTile neighbor in tile.GetNeighbors())
            {
                precipitationSum += rainShadowMap[neighbor.GetCoordinates().ToTuple()];
            }
            averagedRainShadowMap[key] = precipitationSum / (tile.GetNeighbors().Length + 1);
        }

        precipitationMap = averagedRainShadowMap;
        return averagedRainShadowMap;
    }

    // altitude seems to be the biggest obstacle for river source candidate spots being found
    // might be fixed after elevation feature generation is implemented
    // the pathfinding for rivers is probably quite naive, should try and implement something else
    public List<List<HexTile>> GenerateRivers(float minAltitude = 0.65f, float minTemperature = 0.1f, float minPrecipitation = 0.1f)
    {
        List<HexTile> riverSourceCandidates = grid.GetTilesArray().Where(t => t.GetAltitude() >= minAltitude
                                                && t.GetTemperature() >= minPrecipitation
                                                && t.GetPrecipitation() >= minTemperature)
                                            .ToList();
        riverSourceCandidates = riverSourceCandidates.OrderByDescending(t => t.GetAltitude()).ToList(); // sort the list of candidates by altitude
        Debug.Log($"Found {riverSourceCandidates.Count} river source candidates");
        List<List<HexTile>> rivers = new List<List<HexTile>>();

        foreach (HexTile tile in riverSourceCandidates)
        {
            //add checks here to make sure the tile is still a valid candidate
            float weight = tile.GetPrecipitation() * tile.GetAltitude();
            if (Random.value < weight)
            {
                List<HexTile> newRiver = DoRiverRecursion(tile);
                if (newRiver.Count > 2) // only include rivers that are big enough
                {
                    rivers.Add(newRiver);
                    foreach (HexTile riverTile in newRiver)
                    {
                        riverTile.SetHasRiver(true);
                    }
                }
                else
                {
                    foreach (HexTile riverTile in newRiver)
                    {
                        riverTile.SetHasRiver(false);
                    }
                    newRiver.Clear();
                }
            }
        }

        // river searching will probably need to do searching for low points in a bigger range to avoid getting stuck in local minima
        // or alternatively when stuck in local minima make it into a lake and see if you can't derive further rivers from that
        List<HexTile> DoRiverRecursion(HexTile tile, HexTile biasTile = null, List<HexTile> riverTiles = null, int biasRange = 10)
        {
            HexTile nextTile = null;
            riverTiles ??= new List<HexTile>();
            riverTiles.Add(tile);

            // search for other rivers in a biasRange radius and bias the river generation towards those tiles
            // this helps to generate more natural-looking drainage basins
            if (biasTile == null)
            {
                int oldDistance = 0;
                foreach (HexTile searchTile in tile.GetTilesAtRange(biasRange))
                {
                    if (!riverTiles.Contains(searchTile))
                    {
                        if (searchTile.HasRiver() || searchTile.GetTerrain() == Terrain.Ocean || searchTile.GetTerrain() == Terrain.FreshWater)
                        {
                            int newDistance = HexCoordinates.HexDistance(tile.GetCoordinates(), searchTile.GetCoordinates());
                            if (newDistance < oldDistance || oldDistance == 0)
                            {
                                oldDistance = newDistance;
                                biasTile = searchTile;
                            }
                        }
                    }
                }
            }

            //float lowestEffectiveAltitude = 1f;
            float lowestEffectiveAltitude = tile.GetAltitude();
            foreach (HexTile neighbor in tile.GetNeighbors())
            {
                if (neighbor.GetTerrain() == Terrain.Ocean || neighbor.GetTerrain() == Terrain.FreshWater) // the neighbouring tile is a water tile so the river terminates
                {
                    return riverTiles;
                }
                else if (!riverTiles.Contains(neighbor)) // checks that the new tile isn't already a part of the same river
                {
                    if (neighbor.HasRiver()) // two rivers have met and should combine here
                    {
                        return riverTiles;
                    }
                    else
                    {
                        // check that the tile 'neighbor' has no surrounding tiles that are part of this river other than 'tile'
                        bool neighborIsValid = true;
                        foreach (HexTile neighborsNeighbor in neighbor.GetNeighbors())
                        {
                            if (!neighborIsValid)
                            {
                                break;
                            }
                            if (riverTiles.Contains(neighborsNeighbor))
                            {
                                neighborIsValid = neighborsNeighbor == tile;
                            }
                        }
                        if (!neighborIsValid) { continue; }

                        float alignment = 0f;
                        if (biasTile != null)
                        {
                            HexCoordinates toNeighbor = neighbor.GetCoordinates().HexSubtract(tile.GetCoordinates());
                            HexCoordinates toDestination = biasTile.GetCoordinates().HexSubtract(tile.GetCoordinates());
                            alignment = Vector3.Dot(toNeighbor.ToVec3().normalized, toDestination.ToVec3().normalized);
                            alignment = Mathf.Clamp01((alignment + 1f) * 0.5f);
                        }
                        
                        float effectiveAltitude = neighbor.GetAltitude() - alignment * 0.15f;
                        effectiveAltitude += Random.Range(-0.02f, 0.02f);
                        if (effectiveAltitude < lowestEffectiveAltitude)
                        {
                            nextTile = neighbor;
                            lowestEffectiveAltitude = effectiveAltitude;
                        }

                    }
                }
            }

            if (nextTile == null)
            {
                if (Random.value <= 0.5f) // random chance to build a lake at the end of the river instead of terminating
                {
                    return BuildLake(riverTiles);
                }
                else
                {
                    return riverTiles;
                }
            }
            else
            {
                return DoRiverRecursion(nextTile, biasTile, riverTiles);
            }
        }

        // TODO: handle the edge case where lakes can intersect a river and split it in two
        //       this isn't necessarily a bug but the split-off part of the river needs to be assigned into a new river list
        // TODO: the lakes that are being generated are too small currently, seems to average around 3 tiles
        List<HexTile> BuildLake(List<HexTile> river)
        {
            Debug.Log("Attempting to create a lake from a river...", river[0]);
            if (river.Count < 3)
            {
                Debug.Log("River size too small to build lakes, returning...", river[0]);
                return river;
            }

            // could also try just taking the final tile of the river and generating a lake from that
            HexTile lakeStartCandidate = null;
            float lowestAltitude = 1f; // this needs to be initialized to some other value, otherwise it's possible for lakes to go up mountains etc.
            int tilesToIterate = Mathf.RoundToInt((float)river.Count * 0.2f);
            for (int i = river.Count - 1; i > river.Count - tilesToIterate; i--)
            {
                HexTile curRiverTile = river[i];
                foreach (HexTile neighbor in curRiverTile.GetNeighbors())
                {
                    float neighborAltitude = neighbor.GetAltitude();
                    if (neighborAltitude < lowestAltitude && !river.Contains(neighbor))
                    {
                        lowestAltitude = neighborAltitude;
                        lakeStartCandidate = neighbor;
                    }
                }
            }
            if (lakeStartCandidate == null)
            {
                Debug.Log("No suitable lake start tile found, returning...", river[0]);
                return river;
            }

            List<HexTile> lake = new List<HexTile>();
            List<HexTile> lakeNeighbors = new List<HexTile>();
            Terrain newLakeTerrain = Terrain.FreshWater;
            lake.Add(lakeStartCandidate);
            lakeStartCandidate.SetTerrain(Terrain.FreshWater);
            foreach (HexTile neighbor in lakeStartCandidate.GetNeighbors()) { lakeNeighbors.Add(neighbor); }
            while (Random.value <= 1f - ((lake.Count - 1) * 0.025f)) // after the first tile every subsequent tile imposes an additional chance of stopping lake generation
            {
                HexTile nextLakeTile = null;
                lowestAltitude = 1f;
                foreach (HexTile neighbor in lakeNeighbors)
                {
                    Terrain terrain = neighbor.GetTerrain();
                    // encountering either another lake or an ocean will result in the current lake being terminated,
                    // but it'll likely create a noticeable artefact where the two bodies of water connect with only 1 tile
                    if (terrain == Terrain.Ocean)
                    {
                        lake.Add(neighbor);
                        newLakeTerrain = Terrain.Ocean;
                        nextLakeTile = null;

                        foreach (HexTile tile in neighbor.GetNeighbors().Where(x => x.terrain != Terrain.Ocean))
                        {
                            if (!lake.Contains(tile))
                            {
                                lake.Add(tile);
                            }
                        }

                        break;
                    }
                    else if (terrain == Terrain.FreshWater)
                    {
                        lake.Add(neighbor);
                        nextLakeTile = null;
                        break;
                    }
                    float neighborAltitude = neighbor.GetAltitude();
                    if (neighborAltitude < lowestAltitude)
                    {
                        lowestAltitude = neighborAltitude;
                        nextLakeTile = neighbor;
                    }
                }
                if (nextLakeTile == null) { break; }

                lake.Add(nextLakeTile);
                lakeNeighbors.Remove(nextLakeTile);
                foreach (HexTile neighbor in nextLakeTile.GetNeighbors())
                {
                    lakeNeighbors.Add(neighbor);
                }
            }

            // assign the appropriate terrain to the lake tiles and adjust them to be below the water level, and return the modified river
            // alternatively the water level could be ignored, as lakes shouldn't need to be below it
            foreach (HexTile lakeTile in lake)
            {
                lakeTile.SetTerrain(newLakeTerrain);
                river.Remove(lakeTile);

                HexTile[] neighbors = lakeTile.GetNeighbors();
                float altitude = lakeTile.GetAltitude();
                foreach (HexTile neighbor in neighbors)
                {
                    altitude += neighbor.GetAltitude();
                }
                altitude /= neighbors.Length + 1;
                lakeTile.SetAltitude(Mathf.Min(altitude, grid.waterLevel * Random.Range(0.85f, 0.95f)));
            }

            return river;
        }

        Debug.Log($"Generated {rivers.Count} rivers from source candidates");
        return rivers;
    }

    // this might need rivers added to it as well
    private void CategorizeWaterTiles()
    {
        // assign terrain to oceanic water tiles
        Debug.Log("Starting ocean mapping...");
        foreach (HexTile tile in grid.borderTiles)
        {
            DoOceanMappingRecursion(tile);
        }

        void DoOceanMappingRecursion(HexTile tile)
        {
            if (tile.GetAltitude() <= grid.waterLevel && tile.terrain != Terrain.Ocean)
            {
                tile.SetTerrain(Terrain.Ocean);
                foreach (HexTile neighborTile in tile.GetNeighbors())
                {
                    DoOceanMappingRecursion(neighborTile);
                }
            }
        }

        if (oceanDistanceMap.Count == 0)
        {
            CalculateOceanDistances();
        }

        // assign terrain to non-oceanic water tiles
        foreach (HexTile tile in grid.GetTilesArray())
        {
            if (tile.GetAltitude() <= grid.waterLevel && tile.terrain != Terrain.Ocean)
            {
                tile.SetTerrain(Terrain.FreshWater);
            }
        }

        return;
    }

    // does a breadth-first search for each non-oceanic tile to find the distance between it and the nearest oceanic tile
    // this should obviously only be run after checking which tiles are oceanic
    private void CalculateOceanDistances()
    {
        foreach (HexTile tile in grid.GetTilesArray())
        {
            int distanceFromNearestOcean = 0;
            if (tile.terrain != Terrain.Ocean)
            {
                // do a breadth-first search to make sure that it is actually the nearest ocean tile that is found
                Queue<HexTile> tileQueue = new Queue<HexTile>();
                Dictionary<(int, int, int), bool> exploredTiles = new Dictionary<(int, int, int), bool>();

                foreach (HexTile neighborTile in tile.GetNeighbors())
                {
                    tileQueue.Enqueue(neighborTile);
                    exploredTiles[neighborTile.GetCoordinates().ToTuple()] = true;
                }

                while (tileQueue.Count > 0)
                {
                    HexTile neighborTile = tileQueue.Dequeue();
                    if (neighborTile.terrain == Terrain.Ocean)
                    {
                        distanceFromNearestOcean = HexCoordinates.HexDistance(tile.GetCoordinates(), neighborTile.GetCoordinates());
                        //float tileDistance = HexCoordinates.HexDistance(tile.GetCoordinates(), neighborTile.GetCoordinates());
                        //float maxDistance = ((grid.height + grid.width) / 2) * 0.05f; // the maximum distance from which ocean proximity has an effect on temperature
                        //distanceFromNearestOcean = tileDistance / maxDistance;

                        tileQueue.Clear();
                    }
                    else
                    {
                        foreach (HexTile tileToExplore in neighborTile.GetNeighbors())
                        {
                            (int, int, int) coordinateTuple = tileToExplore.GetCoordinates().ToTuple();
                            bool explored = exploredTiles.TryGetValue(coordinateTuple, out bool value);
                            if (explored) { continue; }

                            tileQueue.Enqueue(tileToExplore);
                            exploredTiles[coordinateTuple] = true;
                        }
                    }
                }
            }

            oceanDistanceMap[tile.GetCoordinates().ToTuple()] = distanceFromNearestOcean;
        }
    }

    // could potentially generalize this to be type-agnostic instead of requiring a noisemap
    public Dictionary<(int, int, int), float> DoCellularAutomataPass(Dictionary<(int, int, int), float> noiseMap, float boundary, int neighborTilesForTransition = 4, int passes = 1)
    {
        Dictionary<(int, int, int), float> newNoiseMap = new Dictionary<(int, int, int), float>();
        int tilesWith0Value = 0; // for tracking the number of tiles in the noise map with a value of 0

        foreach (KeyValuePair<(int, int, int), float> entry in noiseMap)
        {
            (int, int, int) key = entry.Key;
            HexTile tile = grid.FetchTile(key);
            if (entry.Value == 0f) { tilesWith0Value++; }

            int neighborBoundaryTiles = 0;
            float tileNewValue = 0f; // the new value to which the tile will be set to if transitioned
            HexTile[] neighborTiles = tile.GetNeighbors();
            if (noiseMap[key] > boundary)
            {
                foreach (HexTile neighborTile in neighborTiles)
                {
                    float value = noiseMap[neighborTile.GetCoordinates().ToTuple()];
                    if (value <= boundary)
                    {
                        neighborBoundaryTiles++;
                        tileNewValue += value;
                    }
                }
            }
            else
            {
                foreach (HexTile neighborTile in neighborTiles)
                {
                    float value = noiseMap[neighborTile.GetCoordinates().ToTuple()];
                    if (value > boundary)
                    {
                        neighborBoundaryTiles++;
                        tileNewValue += value;
                    }
                }
            }

            int neighborTileReq = neighborTiles.Length == 6 ? neighborTilesForTransition : (int)Mathf.Round(neighborTilesForTransition * (neighborTiles.Length / 6f));
            if (neighborBoundaryTiles > neighborTileReq)
            {
                tileNewValue /= neighborBoundaryTiles;
                newNoiseMap[key] = tileNewValue;
            }
            else
            {
                newNoiseMap[key] = noiseMap[key];
            }
        }

        Debug.Assert(newNoiseMap.Count == noiseMap.Count, $"Generated only {newNoiseMap.Count} tiles out of the required {noiseMap.Count}", grid);
        if (tilesWith0Value > grid.height * grid.width * 0.1f)
        {
            Debug.LogWarning($"Found {tilesWith0Value} tiles with an initial value of 0, parameter noise map may not be initialized correctly", grid);
        }
        if (passes > 1)
        {
            return DoCellularAutomataPass(newNoiseMap, boundary, neighborTilesForTransition, passes - 1);
        }
        else
        {
            return newNoiseMap;
        }
    }

    // this will need to be rewritten
    public void AssignTerrains()
    {
        /*
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
    */
    }
}