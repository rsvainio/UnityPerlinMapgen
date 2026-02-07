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
            foreach (TerrainType terrain in terrainTypes.Values)
            {
                if (returnTerrain != null && (returnTerrain.priority > terrain.priority))
                {
                    break;
                }
                if (terrain.MatchesRules(tile))
                {
                    returnTerrain = returnTerrain != null ? returnTerrain : terrain;

                    if (terrain.priority < returnTerrain.priority)
                    {
                        returnTerrain = terrain;
                    }
                    else if (returnTerrain.priority == terrain.priority)
                    {
                        Debug.LogWarning($"Tile meets conditions for two terrains of equal priority: {returnTerrain.id}, {terrain.id}", tile);
                    }
                }
            }

            // check that returnTerrain isn't null here
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
