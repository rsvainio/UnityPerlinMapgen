using System.Collections.Generic;
using UnityEngine;

namespace Terrain.TerrainRules
{
    [CreateAssetMenu(menuName = "Terrain/Terrain Rule/Terrain in Range")]
    public class TerrainInRange : TerrainRule
    {
        [Header("Info")]
            public List<TerrainType> terrainList;
            [Tooltip("The range at which to look for terrains")]
            [Range(1, 10)] public int range = 1;
            [Tooltip("The number of tiles from the list of terrains that are required for this rule to return true")]
            [Range(1, 10)] public int tilesRequired = 1;
            [Tooltip("Invert the rule so that it returns false if the required amount of terrains are within range")] // fix
            public bool inverted = false;

        public override bool MatchesRule(HexTile tile)
        {
            List<HexTile> tilesInRange = tile.GetTilesAtRange(range);
            int x = 0;

            tilesInRange.ForEach(t => {
                if (terrainList.Contains(t.terrain)) { x++; } 
            });

            return x >= tilesRequired ? !inverted : inverted;
        }
    }
}
