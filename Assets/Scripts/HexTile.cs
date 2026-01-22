using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class HexTile : MonoBehaviour
{
    private HexCoordinates coordinates;
    private HexGrid grid;
    public string stringcoords;
    public Terrain terrain;
    private HexTile[] neighbors;
    [SerializeField] private float precipitation, altitude, temperature;
    private bool hasRiver = false;

    public void Initialize(int q, int r, int s, Terrain terrain, HexGrid grid)
    {
        this.coordinates = new HexCoordinates(q, r, s);
        this.stringcoords = $"{q}, {r}, {s}";
        this.neighbors = null;
        this.terrain = terrain;
        this.grid = grid;
    }

    // resets the iteration-specific attributes of the tile
    public void ResetTile()
    {
        SetBiomeAttributes(0f, 0f, 0f);
        SetTerrain(null);
        hasRiver = false;
    }

    public HexCoordinates GetCoordinates()
    {
        return coordinates;
    }

    public float GetPrecipitation() { return precipitation; }
    public void SetPrecipitation(float precipitation) { SetBiomeAttributes(precipitation, this.altitude, this.temperature); }
    public float GetAltitude() { return altitude; }
    public void SetAltitude(float altitude) { SetBiomeAttributes(this.precipitation, altitude, this.temperature); }
    public float GetTemperature() { return temperature; }
    public void SetTemperature(float temperature) { SetBiomeAttributes(this.precipitation, this.altitude, temperature); } 
    public void SetBiomeAttributes(float precipitation, float altitude, float temperature)
    {
        Debug.Assert(!float.IsNaN(precipitation) && !float.IsNaN(altitude) && !float.IsNaN(temperature), 
            $"Attempted to set a NaN biome attribute, precipitation: {precipitation}, altitude: {altitude}, temperature: {temperature}", this);
        this.precipitation = precipitation;
        this.altitude = altitude;
        this.temperature = temperature;
    }
    public bool HasRiver() { return hasRiver;  }
    public void SetHasRiver(bool hasRiver) { this.hasRiver = hasRiver; }
    public Terrain GetTerrain() { return terrain; }
    public void SetTerrain(Terrain terrain)
    {
        this.terrain = terrain;
        //GetComponentInChildren<MeshRenderer>().material.color = terrain.baseColor;
    }

    // returns an array of references to this tile's neighbors and builds the said list of neighbors if it's null at call time
    public HexTile[] GetNeighbors()
    {
        if (neighbors == null)
        {
            HexCoordinates[] neighborCoordinates = GetNeighborCoordinates();
            Dictionary<(int, int, int), HexTile> gridTiles = grid.GetTiles();
            neighbors = new HexTile[neighborCoordinates.Length];
            int j = 0;

            for (int i = 0; i < neighbors.Length; i++)
            {
                if (gridTiles.TryGetValue(neighborCoordinates[i].ToTuple(), out HexTile tile))
                {
                    neighbors[i - j] = tile;
                }
                else
                {
                    j++;
                }
            }

            Array.Resize(ref neighbors, neighbors.Length - j);
        }

        return neighbors;
    }

    public HexCoordinates GetCoordinatesInDirection(Vector3 vector)
    {
        float bestDotProduct = 0f;
        HexCoordinates bestDirection = HexMetrics.neighborVectors[0];

        foreach(HexCoordinates coordinate in HexMetrics.neighborVectors)
        {
            float dot = Vector3.Dot(coordinate.ToVec3(), vector);
            if (dot > bestDotProduct)
            {
                dot = bestDotProduct;
                bestDirection = coordinate;
            }
        }

        return coordinates.HexAdd(bestDirection);
    }

    // returns a list of each HexTile that is at most a distance of range from this tile
    public List<HexTile> GetTilesAtRange(int range)
    {
        List<HexTile> results = new List<HexTile>();
        for (int q = -range; q <= range; q++)
        {
            for (int r = Mathf.Max(-range, -q - range); r <= Mathf.Min(range, -q + range); r++)
            {
                int s = -q - r;
                if (grid.GetTiles().TryGetValue((q, r, s), out HexTile tile))
                {
                    results.Add(tile);
                }
            }
        }

        return results;
    }

    // returns an array consisting of the coordinates of neighboring hexTiles
    // since the class has no knowledge of the boundaries of the map, there's a chance of returning coordinates that don't correspond to any actual hexTiles
    public HexCoordinates[] GetNeighborCoordinates()
    {
        HexCoordinates[] neighborCoordinates;

        if (neighbors == null)
        {
            HexCoordinates[] vectors = HexMetrics.neighborVectors;
            neighborCoordinates = new HexCoordinates[vectors.Length];

            for (int i = 0; i < vectors.Length; i++)
            {
                neighborCoordinates[i] = coordinates.HexAdd(vectors[i]);
            }
        }
        else
        {
            neighborCoordinates = new HexCoordinates[neighbors.Length];

            for (int i = 0; i < neighbors.Length; i++)
            {
                neighborCoordinates[i] = neighbors[i].GetCoordinates();
            }
        }

        return neighborCoordinates;
    }
}