using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Terrain
{
    public static class TerrainDatabase
    {
        private static readonly Dictionary<string, TerrainType> _terrainTypes = new();
        private static readonly Dictionary<string, TerrainFeature> _terrainFeatures = new();

        public static TerrainType GetTerrainType(string id) => _terrainTypes[id];
        public static TerrainFeature GetTerrainFeature(string id) => _terrainFeatures[id];

        public static TerrainType GetMatchingTerrain(HexTile tile)
        {
            TerrainType returnTerrain = null;
            List<TerrainType> erroneousTerrains = new List<TerrainType>();
            foreach (TerrainType terrain in _terrainTypes.Values)
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
                    if (returnTerrain == null || terrain.priority < returnTerrain.priority)
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
                _terrainTypes.Remove(terrain.id);
            }

            Debug.Assert(returnTerrain != null, "No valid terrain was found for tile", tile);
            return returnTerrain;
        }

        private static void LoadTerrainTypes()
        {
            TerrainType[] terrainArray = Resources.LoadAll<TerrainType>("TerrainResources/TerrainTypes");
            foreach (TerrainType terrainType in terrainArray)
            {
                _terrainTypes[terrainType.id] = terrainType;
            }
        }

        private static void LoadTerrainFeatures()
        {
            TerrainFeature[] terrainArray = Resources.LoadAll<TerrainFeature>("TerrainResources/TerrainFeatures");
            foreach (TerrainFeature terrainFeature in terrainArray)
            {
                _terrainFeatures[terrainFeature.id] = terrainFeature;
            }
        }

        static TerrainDatabase()
        {
            LoadTerrainTypes();
            // sorting by terraintype priority makes getting a tile's matching terrain more efficient
            // this is technically a hacky solution as dictionaries are by definition unordered and as such converting from a LINQ-query back to a dictionary isn't guaranteed to preserve that order
            _terrainTypes = _terrainTypes.OrderBy(d => d.Value.priority).ToDictionary(d => d.Key, d => d.Value);
            LoadTerrainFeatures();
            Debug.Log($"Loaded {_terrainTypes.Count} TerrainTypes and {_terrainFeatures.Count} TerrainFeatures");
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
