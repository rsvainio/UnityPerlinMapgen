using UnityEngine;

[System.Serializable]
public class Terrain
{
    public Terrain(string name, float baseMovementCost, Color tileColor)
    {
        this.name = name;
        this.baseMovementCost = baseMovementCost;
        this.tileColor = tileColor;
    }

    public string name;
    public float baseMovementCost;
    //public bool isWalkable, isFlyable;
    public Color tileColor;
    
    /*
    public enum TerrainType
    {
        Wall, 
        Floor
    }
    */

    public static Terrain CreateFromJSON(string jsonString){
        return JsonUtility.FromJson<Terrain>(jsonString);
    }
}

public static class DefaultTerrains
{
    public static Terrain tropicalRainforest = new Terrain("tropicalRainforest", 1, new Color32(7, 83, 48, 255));
    public static Terrain savanna = new Terrain("savanna", 1, new Color32(151, 165, 39, 255));
    public static Terrain desert = new Terrain("desert", 1, new Color32(200, 113, 55, 255));
    
    public static Terrain temperateRainforest = new Terrain("temperateRainforest", 1, new Color32(10, 84, 109, 255));
    public static Terrain temperateForest = new Terrain("temperateForest", 1, new Color32(44, 137, 160, 255));
    public static Terrain woodland = new Terrain("woodland", 1, new Color32(179, 124, 6, 255));
    public static Terrain grassland = new Terrain("grassland", 1, new Color32(146, 126, 48, 255));

    public static Terrain borealForest = new Terrain("borealForest", 1, new Color32(91, 143, 82, 255));

    public static Terrain tundra = new Terrain("tundra", 1, new Color32(147, 167, 172, 255));

    public static Terrain mountain = new Terrain("mountain", 1, new Color32(189, 185, 185, 255));
    public static Terrain arctic = new Terrain("arctic", 1, new Color32(255, 255, 255, 255));
    public static Terrain water = new Terrain("water", 1, new Color32(112, 198, 255, 255));
    public static Terrain error = new Terrain("error", 1, new Color32(0, 0, 0, 255));
}