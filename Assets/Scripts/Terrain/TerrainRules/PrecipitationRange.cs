using System;
using UnityEngine;

namespace Terrain.TerrainRules
{
    [CreateAssetMenu(menuName = "Terrain/Terrain Rule/Precipitation Range")]
    public class PrecipitationRange : TerrainRule
    {
        [Range(0f, 1f)] public float min;
        [Range(0f, 1f)] public float max;
        public override bool MatchesRule(HexTile tile)
        {
            return tile.precipitation >= min && tile.precipitation <= max;
        }
    }
}
