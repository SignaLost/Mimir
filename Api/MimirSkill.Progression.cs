using System;
using Mimir.Common;
using UnityEngine;

namespace Mimir;

// Level curve, death and fortifying.
public sealed partial class MimirSkill
{
    private float _deathLoss = 1f;
    private FortifySettings _fortify;

    /// <summary>
    ///     The experience each whole level needs for the next; null keeps the game's curve.
    ///     A result that is not a positive number falls back to the game's, and a curve that throws is logged and dropped.
    /// </summary>
    public Func<int, float> LevelCurve { get; set; }

    /// <summary>The share of the game's death penalty the skill takes, 1 by default; 0 also keeps progress. Unused while the skill fortifies.</summary>
    public float DeathLoss
    {
        get => _deathLoss;
        set => _deathLoss = float.IsNaN(value) || float.IsInfinity(value) ? 1f : Mathf.Max(0f, value);
    }

    /// <summary>How the skill fortifies; see <see cref="FortifySettings" />.</summary>
    public FortifySettings Fortify => _fortify ??= new FortifySettings(Type);

    /// <summary>Raised when the skill's level rises by any means, with the new level.</summary>
    public event Action<int> LeveledUp;

    /// <summary>The experience to reach the next level after the given one, by <see cref="LevelCurve" />.</summary>
    internal float CostAt(int level)
    {
        if (LevelCurve == null)
        {
            return Curves.Game(level);
        }

        try
        {
            float cost = LevelCurve(level);
            return cost > 0f && !float.IsInfinity(cost) ? cost : Curves.Game(level);
        }
        catch (Exception e)
        {
            Log.LogError($"The level curve of {Id} failed at level {level}; the game's is used: {e.Message}");
            LevelCurve = null;
            return Curves.Game(level);
        }
    }

    internal void RaiseLeveledUp(int level)
    {
        Handlers.Invoke(LeveledUp, $"{Id}.LeveledUp", level);
    }
}
