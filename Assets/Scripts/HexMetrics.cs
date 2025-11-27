using System.Xml;
using UnityEngine;

public static class HexMetrics
{
    public const float outerRadius = 10f;
    public const float innerRadius = outerRadius * 0.866025404f;

    //flat-top
    public static Vector3[] cornersFlat = {
        new Vector3(0.5f * outerRadius, 0f, innerRadius),
        new Vector3(outerRadius, 0f, 0f),
        new Vector3(0.5f * outerRadius, 0f, -innerRadius),
        new Vector3(-0.5f * outerRadius, 0f, -innerRadius),
        new Vector3(-outerRadius, 0f, 0f),
        new Vector3(-0.5f * outerRadius, 0f, innerRadius),
        new Vector3(0.5f * outerRadius, 0f, innerRadius),
    };

    //pointy-top
    public static Vector3[] cornersPointy = {
        new Vector3(0f, 0f, outerRadius),
        new Vector3(innerRadius, 0f, 0.5f * outerRadius),
        new Vector3(innerRadius, 0f, -0.5f * outerRadius),
        new Vector3(0f, 0f, -outerRadius),
        new Vector3(-innerRadius, 0f, -0.5f * outerRadius),
        new Vector3(-innerRadius, 0f, 0.5f * outerRadius)
    };

    //vectors that you can iterate through to find neighboring hexes
    public static HexCoordinates[] neighborVectors = {
        new(0, -1, 1), new(1, -1, 0), new(1, 0, -1),
        new(0, 1, -1), new(-1, 1, 0), new(-1, 0, 1)
    };

    // currently only used for wind direction
    public static Vector3 degreesToHexDirection(int angle)
    {
        float radians = angle * Mathf.Deg2Rad;
        float vx = Mathf.Cos(radians);
        float vy = Mathf.Sin(radians);

        float q = vx;
        float r = (Mathf.Sqrt(3f) / 2f) * vy - 0.5f * vx;
        float s = -q - r;

        return new Vector3(q, r, s).normalized;
    }
}