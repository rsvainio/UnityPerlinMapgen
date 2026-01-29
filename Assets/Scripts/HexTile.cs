using System;
using System.Collections.Generic;
using UnityEngine;
using Terrain;

// TODO: rewrite all the fields to be properties where reasonable
public class HexTile : MonoBehaviour
{
    public HexCoordinates coordinates { get; private set; }
    public string stringcoords { get; private set; }
    public float altitude { get; set; }
    public float temperature { get; set; }
    public float precipitation { get; set; }
    public TerrainType terrain { get; set; }
    public bool hasRiver { get; set; } = false;
    public HexTile[] neighbors
    {
        get { return _neighbors ?? GetNeighbors(); }
    }

    private HexGrid _grid;
    private HexTile[] _neighbors;

    public void Initialize(int q, int r, int s, TerrainType terrain, HexGrid grid)
    {
        this.coordinates = new HexCoordinates(q, r, s);
        this.stringcoords = $"{q}, {r}, {s}";
        this.terrain = terrain;
        this._neighbors = null;
        this._grid = grid;
    }

    // resets the iteration-specific attributes of the tile
    public void ResetTile()
    {
        altitude = temperature = precipitation = 0f;
        terrain = null;
        hasRiver = false;
    }

    // returns an array of references to this tile's neighbors and builds the said list of neighbors if it's null at call time
    private HexTile[] GetNeighbors()
    {
        HexCoordinates[] neighborCoordinates = GetNeighborCoordinates();
        Dictionary<(int, int, int), HexTile> _gridTiles = _grid.tiles;
        _neighbors = new HexTile[neighborCoordinates.Length];
        int j = 0;

        for (int i = 0; i < _neighbors.Length; i++)
        {
            if (_gridTiles.TryGetValue(neighborCoordinates[i].ToTuple(), out HexTile tile))
            {
                _neighbors[i - j] = tile;
            }
            else
            {
                j++;
            }
        }

        Array.Resize(ref _neighbors, _neighbors.Length - j);
        return _neighbors;
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
                if (_grid.tiles.TryGetValue((q, r, s), out HexTile tile))
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

        if (_neighbors == null)
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
            neighborCoordinates = new HexCoordinates[_neighbors.Length];

            for (int i = 0; i < _neighbors.Length; i++)
            {
                neighborCoordinates[i] = _neighbors[i].coordinates;
            }
        }

        return neighborCoordinates;
    }
}

public struct TileAttributes
{
    public TileAttributes(float altitude, float temperature, float precipitation)
    {
        this.altitude = altitude;
        this.temperature = temperature;
        this.precipitation = precipitation;
    }

    public float altitude { get; set; }
    public float temperature { get; set; }
    public float precipitation { get; set; }
}