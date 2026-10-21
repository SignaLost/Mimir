using UnityEngine;

namespace Mimir;

/// <summary>Values that grow or shrink with a level, for skill effects.</summary>
public static class Ramp
{
    /// <summary>A stepped value: <paramref name="below" /> under <paramref name="from" />, then <paramref name="start" /> plus <paramref name="perStep" /> every <paramref name="levelsPerStep" /> levels, never past <paramref name="limit" />.</summary>
    public static float Stepped(float level, float from, int levelsPerStep, float start, float perStep, float limit, float below = 0f)
    {
        if (level < from)
        {
            return below;
        }

        int steps = Mathf.FloorToInt((level - from) / Mathf.Max(1, levelsPerStep));
        float value = start + perStep * steps;
        return perStep >= 0f ? Mathf.Min(value, limit) : Mathf.Max(value, limit);
    }

    /// <summary>A linear value: <paramref name="below" /> under <paramref name="from" />, then <paramref name="atFrom" /> rising evenly to <paramref name="atTo" /> at <paramref name="to" /> and held there.</summary>
    public static float Linear(float level, float from, float to, float atFrom, float atTo, float below = float.NaN)
    {
        if (level < from)
        {
            return float.IsNaN(below) ? atFrom : below;
        }

        return to <= from ? atTo : Mathf.Lerp(atFrom, atTo, (level - from) / (to - from));
    }
}
