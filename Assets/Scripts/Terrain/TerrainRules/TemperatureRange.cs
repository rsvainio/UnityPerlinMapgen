using System;
using UnityEngine;

namespace Terrain.TerrainRules
{
    [CreateAssetMenu(menuName = "Terrain/Terrain Rule/Temperature Range")]
    public class TemperatureRange : TerrainRule
    {
        [Range(0f, 1f)] public float min;
        [Range(0f, 1f)] public float max;
        public override bool MatchesRule(HexTile tile)
        {
            return tile.temperature >= min && tile.temperature <= max;
        }
    }
}
