using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Terrain/Terrain Rule/Altitude")]
public class AltitudeRule : TerrainRule
{
    public override bool MatchesRules(HexTile tile, TerrainRuleParameters parameters)
    {
        if (parameters == null)
        {
            throw new ArgumentNullException(nameof(parameters)); 
        }

        return tile.altitude >= parameters.min && tile.altitude <= parameters.max;
    }
}
