using UnityEngine;
using Terrain.TerrainRules;

namespace Terrain
{
    [CreateAssetMenu(menuName = "Terrain/Terrain Type")]
    public class TerrainType : ScriptableObject
    {
        [Header("Info")]
            public string id;
            [Tooltip("Terrains with a lower priority value will be prioritised when a tile meets the conditions for multiple terrains")]
            public int priority = 10;
            [Tooltip("Whether the terrain is included during map generation")]
            public bool generateAtStartup = false;
            public string displayName;
            public Color baseColor;
        [Header("Gameplay")]
            public float baseMovementCost;
            //public bool isWalkable;
        [Header("Rules")]
            public TerrainRule[] rules;

        public bool MatchesRules(HexTile tile)
        {
            foreach (TerrainRule rule in rules)
            {
                if (!rule.MatchesRule(tile)) { return false; }
            }

            return true;
        }
    }
}
