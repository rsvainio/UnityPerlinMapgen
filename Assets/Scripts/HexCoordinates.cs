using System;
using UnityEngine;

public struct HexCoordinates
{
    public readonly int q, r, s;

    public HexCoordinates(int q, int r, int s)
    {
        this.q = q;
        this.r = r;
        this.s = s;
    }

    public HexCoordinates(int q, int r)
    {
        this.q = q;
        this.r = r;
        s = -q - r;
    }
    public HexCoordinates HexAdd(HexCoordinates coordinates)
    {
        int q = this.q + coordinates.q;
        int r = this.r + coordinates.r;
        int s = this.s + coordinates.s;
        return new HexCoordinates(q, r, s);
    }

    public static HexCoordinates FromPosition(Vector3 position)
    {
        float q = (2f / 3f * position.x) / HexMetrics.outerRadius;
        float r = -(1f / 3f * position.x + (float)Math.Sqrt(3) / 3f * position.z) / HexMetrics.outerRadius;

        return CubeRound(q, r);
    }

    private static HexCoordinates CubeRound(float q, float r, float s)
    {
        int intQ = Mathf.RoundToInt(q);
        int intR = Mathf.RoundToInt(r);
        int intS = Mathf.RoundToInt(s);

        float qDiff = Mathf.Abs(intQ - q);
        float rDiff = Mathf.Abs(intR - r);
        float sDiff = Mathf.Abs(intS - s);

        if (qDiff > rDiff && qDiff > sDiff)
        {
            intQ = -intR - intS;
        } else if (rDiff > sDiff)
        {
            intR = -intQ - intS;
        } else
        {
            intS = -intQ - intR;
        }

        return new HexCoordinates(intQ, intR, intS);
    }

    private static HexCoordinates CubeRound(float q, float r)
    {
        return CubeRound(q, r, -q - r);
    }

    public override string ToString()
    {
        return "(" + q.ToString() + ", " + r.ToString() + ", " + s.ToString() + ")";
    }

    public string ToStringOnSeparateLines()
    {
        return q.ToString() + "\n" + r.ToString() + "\n" + s.ToString();
    }

    public (int, int, int) ToTuple()
    {
        return (this.q, this.r, this.s);
    }
}