using System;
using UnityEngine;

namespace Terrain.TerrainRules
{
    [CreateAssetMenu(menuName = "Terrain/Terrain Rule/Altitude")]
    public class AltitudeRule : TerrainRule
    {
        [Range(0f, 1f)] public float min;
        [Range(0f, 1f)] public float max;
        public override bool MatchesRule(HexTile tile)
        {
            return tile.altitude >= min && tile.altitude <= max;
        }
    }
}
