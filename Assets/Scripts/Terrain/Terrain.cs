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

    //public static Terrain CreateFromJSON(string jsonString)
    //{
    //    return JsonUtility.FromJson<Terrain>(jsonString);
    //}

    //public static readonly Terrain TropicalRainforest = new Terrain("tropicalRainforest", 1, new Color32(7, 83, 48, 255));
    //public static readonly Terrain Savanna = new Terrain("savanna", 1, new Color32(151, 165, 39, 255));
    //public static readonly Terrain Desert = new Terrain("desert", 1, new Color32(200, 113, 55, 255));

    //public static readonly Terrain TemperateRainforest = new Terrain("temperateRainforest", 1, new Color32(10, 84, 109, 255));
    //public static readonly Terrain TemperateForest = new Terrain("temperateForest", 1, new Color32(44, 137, 160, 255));
    //public static readonly Terrain Woodland = new Terrain("woodland", 1, new Color32(179, 124, 6, 255));
    //public static readonly Terrain Grassland = new Terrain("grassland", 1, new Color32(146, 126, 48, 255));

    //public static readonly Terrain BorealForest = new Terrain("borealForest", 1, new Color32(91, 143, 82, 255));

    //public static readonly Terrain Tundra = new Terrain("tundra", 1, new Color32(147, 167, 172, 255));

    //public static readonly Terrain Mountain = new Terrain("mountain", 1, new Color32(189, 185, 185, 255));
    //public static readonly Terrain Arctic = new Terrain("arctic", 1, new Color32(255, 255, 255, 255));

    //public static readonly Terrain Ocean = new Terrain("ocean", 1, new Color32(112, 198, 255, 255));
    //public static readonly Terrain FreshWater = new Terrain("freshWater", 1, new Color32(112, 198, 255, 255));

}