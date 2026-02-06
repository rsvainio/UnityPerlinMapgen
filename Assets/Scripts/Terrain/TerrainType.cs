using UnityEngine;

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
        [Header("Rules")]
        public TerrainRuleInstance[] rules;

        public bool MatchesRules(HexTile tile)
        {
            foreach (TerrainRuleInstance rule in rules)
            {
                if (!rule.ruleLogic.MatchesRules(tile, rule.parameters)) { return false; }
            }

            return true;
        }
    }
}
