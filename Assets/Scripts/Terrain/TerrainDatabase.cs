using UnityEngine;
using System.Collections.Generic;

public static class TerrainDatabase
{
    private static readonly Dictionary<string, Terrain> terrains = new();

    public static Terrain GetTerrain(string id) => terrains[id];

    public static void LoadTerrains()
    {
        Terrain[] terrainArray = Resources.LoadAll<Terrain>("TerrainTypes");

        foreach (Terrain terrain in terrainArray)
        {
            terrains[terrain.id] = terrain;
        }
    }

    // references to default terrains for convenience
    public static readonly Terrain ocean = GetTerrain("ocean");
    public static readonly Terrain freshWater = GetTerrain("freshWater");
}
