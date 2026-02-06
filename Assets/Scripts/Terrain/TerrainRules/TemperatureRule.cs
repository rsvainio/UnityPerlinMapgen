using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Terrain/Terrain Rule/Temperature")]
public class TemperatureRule : TerrainRule
{
    public override bool MatchesRules(HexTile tile, TerrainRuleParameters parameters)
    {
        if (parameters == null)
        {
            throw new ArgumentNullException(nameof(parameters));
        }

        return tile.temperature >= parameters.min && tile.altitude <= parameters.max;
    }
}
