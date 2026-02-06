using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Terrain/Terrain Rule/Precipitation")]
public class PrecipitationRule : TerrainRule
{
    public override bool MatchesRules(HexTile tile, TerrainRuleParameters parameters)
    {
        if (parameters == null)
        {
            throw new ArgumentNullException(nameof(parameters));
        }

        return tile.precipitation >= parameters.min && tile.altitude <= parameters.max;
    }
}
