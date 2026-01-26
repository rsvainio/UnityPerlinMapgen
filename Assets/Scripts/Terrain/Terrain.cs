using UnityEngine;

[CreateAssetMenu(menuName = "Terrain/Terrain Type")]
public class Terrain : ScriptableObject
{
    [Header("Info")]
    public string id;
    public string displayName;
    public Color baseColor;
    [Header("Gameplay")]
    public float baseMovementCost;
    //public bool isWalkable;

    public Terrain(string displayName, float baseMovementCost, Color baseColor)
    {
        this.displayName = displayName;
        this.baseMovementCost = baseMovementCost;
        this.baseColor = baseColor;
    }
}