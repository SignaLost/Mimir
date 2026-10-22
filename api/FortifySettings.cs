using Mimir.Core;
using Mimir.Modules;

namespace Mimir;

/// <summary>
///     How a skill fortifies: a slower fortified level trails the skill's, and on death the skill falls back to it instead of losing a share.
///     Each setting left null follows Mimir's own settings.
/// </summary>
public sealed class FortifySettings
{
    private readonly Skills.SkillType _type;

    internal FortifySettings(Skills.SkillType type)
    {
        _type = type;
    }

    /// <summary>True or false to decide for this skill; null leaves it to Mimir's Fortify setting.</summary>
    public bool? Enabled { get; set; }

    /// <summary>The share of a gain the fortified level gets per level the skill is above it.</summary>
    public float? RatePerLevel { get; set; }

    /// <summary>The largest share of a gain the fortified level gets.</summary>
    public float? MaxRate { get; set; }

    /// <summary>Multiplier on the skill's own gains while it fortifies.</summary>
    public float? ActiveRate { get; set; }

    /// <summary>The most levels the fortified level may trail the skill by.</summary>
    public int? MaxGap { get; set; }

    /// <summary>The highest the fortified level may reach, 1 to 100.</summary>
    public int? MaxLevel { get; set; }

    /// <summary>Whether the skill fortifies now, by this and Mimir's settings.</summary>
    public bool Active => Fortifying.Enabled(_type);

    /// <summary>The fortified level of the loaded character; 0 when the skill does not fortify or none is loaded.</summary>
    public float Level => Fortifying.LevelOf(_type);

    /// <summary>Experience toward the next fortified level.</summary>
    public float Progress => Fortifying.ProgressOf(_type);
}
