using System;
using System.Collections.Generic;
using UnityEngine;
using Terrain;

public class HexTile : MonoBehaviour
{
    public HexCoordinates coordinates { get; private set; }
    public string stringcoords { get; private set; }
    public float altitude { get; set; }
    public float temperature { get; set; }
    public float precipitation { get; set; }
    public TerrainType terrain { 
        get {  return _terrain; }
        set { _terrain = value; UpdateMovementCost(); }
    }
    public int movementCost { get; private set; }
    public bool hasRiver { get; set; } = false;
    public HexTile[] neighbors
    {
        get { return _neighbors ?? GetNeighbors(); }
    }

    private HexGrid _grid;
    private TerrainType _terrain;
    private HexTile[] _neighbors = null;

    public void Initialize(int q, int r, int s, TerrainType terrain, HexGrid grid)
    {
        this.coordinates = new HexCoordinates(q, r, s);
        this.stringcoords = $"{q}, {r}, {s}";
        this._terrain = terrain;
        this._grid = grid;
    }

    // resets the iteration-specific attributes of the tile
    public void ResetTile()
    {
        altitude = temperature = precipitation = 0f;
        _terrain = null;
        hasRiver = false;
        this.GetComponentInChildren<Renderer>().material.SetColor("_Color", _grid.defaultColor);
    }

    public HexCoordinates GetCoordinatesInDirection(Vector3 vector)
    {
        float bestDotProduct = 0f;
        HexCoordinates bestDirection = HexMetrics.neighborVectors[0];

        foreach (HexCoordinates coordinate in HexMetrics.neighborVectors)
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

    // returns a list containing each HexTile that is at most a distance of range from this tile
    public List<HexTile> GetTilesAtRange(int range)
    {
        List<HexTile> results = new List<HexTile>();
        for (int q = -range; q <= range; q++)
        {
            for (int r = Mathf.Max(-range, -q - range); r <= Mathf.Min(range, -q + range); r++)
            {
                int s = -q - r;
                (int, int, int) key = HexCoordinates.HexAdd(coordinates, new HexCoordinates(q, r, s)).ToTuple();
                if (_grid.tiles.TryGetValue(key, out HexTile tile))
                {
                    results.Add(tile);
                }
            }
        }

        //Debug.Log($"Found {results.Count} tiles in range", this);
        return results;
    }
    
    // returns a list containing each HexTile that is at a distance between (inclusive) min and max from this tile
    public List<HexTile> GetTilesAtRange(int minRange, int maxRange)
    {
        List<HexTile> results = new List<HexTile>();
        for (int q = -maxRange; q <= maxRange; q++)
        {
            int rMinOuter = Mathf.Max(-maxRange, -q - maxRange);
            int rMaxOuter = Mathf.Min(maxRange, -q + maxRange);

            int rMinInner = Mathf.Max(-minRange + 1, -q - minRange + 1);
            int rMaxInner = Mathf.Min(minRange - 1, -q + minRange - 1);

            for (int r = rMinOuter; r <= rMaxOuter; r++)
            {
                if (r >= rMinInner && r <= rMaxInner)
                {
                    continue;
                }

                int s = -q - r;
                (int, int, int) key = HexCoordinates.HexAdd(coordinates, new HexCoordinates(q, r, s)).ToTuple();
                if (_grid.tiles.TryGetValue(key, out HexTile tile))
                {
                    int distance = HexCoordinates.HexDistance(coordinates, new HexCoordinates(key));
                    Debug.Assert(distance >= minRange && distance <= maxRange, $"Tile distance not within the bounds of {minRange}, {maxRange}, actual distance: {distance}", tile);
                    results.Add(tile);
                }
            }
        }

        Debug.Log($"Found {results.Count} tiles in the range of {minRange} to {maxRange}", this);
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

    // returns an array of references to this tile's neighbors and builds the said list of neighbors if it's null at call time
    private HexTile[] GetNeighbors()
    {
        HexCoordinates[] neighborCoordinates = GetNeighborCoordinates(); // TODO: there is no reason to separately fetch the coordinates, remove this
        _neighbors = new HexTile[neighborCoordinates.Length];
        int j = 0;

        for (int i = 0; i < _neighbors.Length; i++)
        {
            if (_grid.tiles.TryGetValue(neighborCoordinates[i].ToTuple(), out HexTile tile))
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

    private void UpdateMovementCost()
    {
        movementCost = _terrain.baseMovementCost;
    }
}

//public class TileAttributes
//{
//    public TileAttributes(float altitude, float temperature, float precipitation)
//    {
//        this.altitude = altitude;
//        this.temperature = temperature;
//        this.precipitation = precipitation;
//    }

//    public float altitude { get; set; }
//    public float temperature { get; set; }
//    public float precipitation { get; set; }
//}
