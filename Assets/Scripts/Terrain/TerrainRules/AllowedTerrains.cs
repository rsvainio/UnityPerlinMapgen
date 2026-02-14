using System.Collections.Generic;
using UnityEngine;

namespace Terrain.TerrainRules
{
    [CreateAssetMenu(menuName = "Terrain/Terrain Rule/Allowed Terrain")]
    public class AllowedTerrain : TerrainRule
    {
        [Header("Info")]
            public List<TerrainType> terrainList;
            [Tooltip("Invert the rule so that it disallows this rule's terrains instead")]
            public bool inverted = false;

        public override bool MatchesRule(HexTile tile)
        {
            return terrainList.Contains(tile.terrain) ? !inverted : inverted;
        }
    }
}
