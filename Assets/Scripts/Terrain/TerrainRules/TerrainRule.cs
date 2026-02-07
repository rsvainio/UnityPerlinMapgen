using UnityEngine;

// generation rules should be expanded to support non-parameterized rules as well - for instance
// a rule like "above sea level" would never need its value changed and so wouldn't require a TerrainRuleParameters object
namespace Terrain.TerrainRules
{
    public abstract class TerrainRule : ScriptableObject
    {
        public abstract bool MatchesRule(HexTile tile);
    }
}
