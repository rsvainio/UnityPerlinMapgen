using Terrain.TerrainRules;
using UnityEngine;

namespace Terrain
{
    [CreateAssetMenu(menuName = "Terrain/Terrain Feature")]
    public class TerrainFeature : ScriptableObject
    {
        [Header("Info")]
            public string id;
            public string displayName;
        [Header("Gameplay")]
            public float movementCostModifier;
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
