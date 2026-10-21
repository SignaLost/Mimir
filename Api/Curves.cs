using UnityEngine;

namespace Mimir;

/// <summary>Level curves for <see cref="MimirSkill.LevelCurve" />; each maps a whole level to the experience needed for the next.</summary>
public static class Curves
{
    /// <summary>The game's own curve: (level + 1)^1.5 × 0.5 + 0.5.</summary>
    public static float Game(int level)
    {
        return Mathf.Pow(level + 1, 1.5f) * 0.5f + 0.5f;
    }

    /// <summary>The game's curve times a factor.</summary>
    public static System.Func<int, float> Scaled(float factor)
    {
        return level => Game(level) * factor;
    }

    /// <summary>A linear curve: the first level's cost plus a fixed amount per level after it.</summary>
    public static System.Func<int, float> Linear(float first, float perLevel)
    {
        return level => first + perLevel * level;
    }

    /// <summary>Every level costs the same.</summary>
    public static System.Func<int, float> Flat(float cost)
    {
        return _ => cost;
    }
}
