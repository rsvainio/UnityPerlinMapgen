using UnityEngine;
using System.Collections.Generic;

namespace Terrain
{
    public static class TerrainDatabase
    {
        private static readonly Dictionary<string, TerrainType> terrainTypes = new();
        private static readonly Dictionary<string, TerrainFeature> terrainFeatures = new();

        public static TerrainType GetTerrainType(string id) => terrainTypes[id];
        public static TerrainFeature GetTerrainFeature(string id) => terrainFeatures[id];

        public static TerrainType GetMatchingTerrain(HexTile tile)
        {
            TerrainType returnTerrain = null;
            List<TerrainType> erroneousTerrains = new List<TerrainType>();
            foreach (TerrainType terrain in terrainTypes.Values)
            {
                if (terrain.rules.Length == 0)
                {
                    Debug.LogWarning($"Terrain type {terrain.name} has no generation rules, skipping...", terrain);
                    erroneousTerrains.Add(terrain);
                    continue;
                }
                else if ((returnTerrain != null && (returnTerrain.priority < terrain.priority)) || !terrain.generateAtStartup)
                {
                    continue;
                }
                else if (terrain.MatchesRules(tile))
                {
                    if (returnTerrain == null || terrain.priority > returnTerrain.priority)
                    {
                        returnTerrain = terrain;
                    }
                    else if (returnTerrain.priority == terrain.priority)
                    {
                        Debug.LogWarning($"Tile meets conditions for two terrains of equal priority: {returnTerrain.id}, {terrain.id}", tile);
                    }
                }
            }

            foreach (TerrainType terrain in erroneousTerrains)
            {
                terrainTypes.Remove(terrain.id);
            }

            Debug.Assert(returnTerrain != null, "No valid terrain was found for tile", tile);
            return returnTerrain;
        }

        private static void LoadTerrainTypes()
        {
            TerrainType[] terrainArray = Resources.LoadAll<TerrainType>("TerrainResources/TerrainTypes");
            foreach (TerrainType terrainType in terrainArray)
            {
                terrainTypes[terrainType.id] = terrainType;
            }
        }

        private static void LoadTerrainFeatures()
        {
            TerrainFeature[] terrainArray = Resources.LoadAll<TerrainFeature>("TerrainResources/TerrainFeatures");
            foreach (TerrainFeature terrainFeature in terrainArray)
            {
                terrainFeatures[terrainFeature.id] = terrainFeature;
            }
        }

        static TerrainDatabase()
        {
            LoadTerrainTypes();
            LoadTerrainFeatures();
            Debug.Log($"Loaded {terrainTypes.Count} TerrainTypes and {terrainFeatures.Count} TerrainFeatures");
        }
    }

    public static class TerrainTypes
    {
        public static readonly TerrainType ocean = TerrainDatabase.GetTerrainType("ocean");
        public static readonly TerrainType freshWater = TerrainDatabase.GetTerrainType("freshWater");
    }

    public static class TerrainFeatures
    {
        public static readonly TerrainFeature forest = TerrainDatabase.GetTerrainFeature("forest");
    }
}
