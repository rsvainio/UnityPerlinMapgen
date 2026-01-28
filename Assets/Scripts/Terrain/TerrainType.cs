using UnityEngine;

namespace Terrain
{
    [CreateAssetMenu(menuName = "Terrain/Terrain Type")]
    public class TerrainType : ScriptableObject
    {
        [Header("Info")]
        public string id;
        public string displayName;
        public Color baseColor;
        [Header("Gameplay")]
        public float baseMovementCost;
        //public bool isWalkable;

        public TerrainType(string displayName, float baseMovementCost, Color baseColor)
        {
            this.displayName = displayName;
            this.baseMovementCost = baseMovementCost;
            this.baseColor = baseColor;
        }
    }
}
