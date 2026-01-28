using UnityEngine;

namespace Terrain
{
    [CreateAssetMenu(menuName = "Terrain/Terrain Feature")]
    public class TerrainFeature : ScriptableObject
    {
        [Header("Info")]
        public string id;
        public string displayName;
        [Header("Gameplay")]
        public float movementCostModifier;
        //public bool isWalkable;

        public TerrainFeature(string displayName, float movementCostModifier)
        {
            this.displayName = displayName;
            this.movementCostModifier = movementCostModifier;
        }
    }
}
