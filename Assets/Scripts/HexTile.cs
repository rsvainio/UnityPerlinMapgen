using System;
using UnityEngine;

public class HexTile : MonoBehaviour
{
    private HexCoordinates coordinates;
    private HexGrid grid;
    public string stringcoords;
    public Terrain terrain;
    private HexTile[] neighbors;
    [SerializeField] private float precipitation, altitude, temperature;

    public void Initialize(int q, int r, int s, Terrain terrain, HexGrid grid)
    {
        this.coordinates = new HexCoordinates(q, r, s);
        this.stringcoords = $"{q}, {r}, {s}";
        this.neighbors = null;
        this.terrain = terrain;
        this.grid = grid;
    }

    public HexCoordinates GetCoordinates()
    {
        return coordinates;
    }

    public float GetPrecipitation() { return precipitation; }
    public void SetPrecipitation(float precipitation) { this.precipitation = precipitation; }
    public float GetAltitude() { return altitude; }
    public void SetAltitude(float altitude) { this.altitude = altitude; }
    public float GetTemperature() { return temperature; }
    public void SetTemperature(float temperature) { this.temperature = temperature; } 

    public void SetBiomeAttributes(float precipitation, float altitude, float temperature)
    {
        this.precipitation = precipitation;
        this.altitude = altitude;
        this.temperature = temperature;
    }

    public void SetTerrain(Terrain terrain)
    {
        this.terrain = terrain;
        //GetComponentInChildren<MeshRenderer>().material.color = terrain.baseColor;
    }

    //returns an array of references to this tile's neighbors and builds the said list of neighbors if it's null when the function is called
    public HexTile[] GetNeighbors()
    {
        if (neighbors == null)
        {
            HexCoordinates[] neighborCoordinates = GetNeighborCoordinates();
            neighbors = new HexTile[neighborCoordinates.Length];
            int j = 0;

            for (int i = 0; i < neighbors.Length; i++)
            {
                try
                {
                    neighbors[i - j] = grid.FetchTile(neighborCoordinates[i].ToTuple());
                }
                catch(ArgumentException)
                {
                    j += 1;
                }
            }

            Array.Resize(ref neighbors, neighbors.Length - j);
            if (neighbors.Length < 6) { grid.borderTiles.Add(this); } // add this tile to the list of map border tiles if it has fewer than 6 neighbors
        }

        return neighbors;
    }

    //returns an array consisting of the coordinates of neighboring hexTiles
    //since the class has no knowledge of the boundaries of the map, there's a chance of returning coordinates that don't correspond to any actual hexTiles
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