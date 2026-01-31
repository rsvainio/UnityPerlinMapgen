using UnityEngine;

[CreateAssetMenu(menuName = "Terrain/Terrain Rule/Precipitation")]
public class PrecipitationRule : TerrainRule
{
    public override bool MatchesRules(HexTile tile, TerrainRuleParameters parameters)
    {
        return tile.precipitation >= parameters.min && tile.altitude <= parameters.max;
    }
}
