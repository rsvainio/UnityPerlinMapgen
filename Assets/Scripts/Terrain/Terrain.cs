using UnityEngine;
using System.Collections.Generic;

namespace Terrain
{
    [CreateAssetMenu(menuName = "Terrain/Terrain Type")]
    public class TerrainType : ScriptableObject
    {
        [Header("Info")]
        public string id;
        public string displayName;
        public Color baseColor;
        [Header("Gameplay")]
        public float baseMovementCost;
        //public bool isWalkable;

        public TerrainType(string displayName, float baseMovementCost, Color baseColor)
        {
            this.displayName = displayName;
            this.baseMovementCost = baseMovementCost;
            this.baseColor = baseColor;
        }
    }

    public static class TerrainDatabase
    {
        private static readonly Dictionary<string, TerrainType> terrainTypes = new();

        public static TerrainType GetTerrainType(string id) => terrainTypes[id];

        private static void LoadTerrainTypes()
        {
            TerrainType[] terrainArray = Resources.LoadAll<TerrainType>("TerrainTypes");
            Debug.Log($"Loaded {terrainArray.Length} TerrainTypes");

            foreach (TerrainType terrainType in terrainArray)
            {
                terrainTypes[terrainType.id] = terrainType;
            }
        }

        static TerrainDatabase()
        {
            LoadTerrainTypes();
        }
    }

    public static class TerrainTypes
    {
        public static TerrainType ocean = TerrainDatabase.GetTerrainType("ocean");
        public static TerrainType freshWater = TerrainDatabase.GetTerrainType("freshWater");
    }
}
