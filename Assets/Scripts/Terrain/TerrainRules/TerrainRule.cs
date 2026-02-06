using UnityEngine;

// generation rules should be expanded to support non-parameterized rules as well - for instance
// a rule like "above sea level" would never need its value changed and so wouldn't require a TerrainRuleParameters object
public abstract class TerrainRule : ScriptableObject
{
    public abstract bool MatchesRules(HexTile tile, TerrainRuleParameters parameters = null);
}

[System.Serializable]
public class TerrainRuleParameters
{
    public float min, max;
}

[System.Serializable]
public class TerrainRuleInstance
{
    public TerrainRule ruleLogic;
    public TerrainRuleParameters parameters;
}