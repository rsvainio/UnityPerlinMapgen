using UnityEngine;

[CreateAssetMenu(menuName = "Terrain/Terrain Rule/Altitude")]
public class AltitudeRule : TerrainRule
{
    public override bool MatchesRules(HexTile tile, TerrainRuleParameters parameters)
    {
        return tile.altitude >= parameters.min && tile.altitude <= parameters.max;
    }
}
