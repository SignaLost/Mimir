using System;
using UnityEngine;

namespace Mimir;

/// <summary>A new skill to register with <see cref="API.Register" />.</summary>
public sealed class SkillDefinition
{
    /// <summary>The skill's id, required and lowercased; the game's skill type is generated from it, so changing it orphans saved progress.</summary>
    public string Id { get; set; }

    /// <summary>The name shown in game, a localization token.</summary>
    public string Name { get; set; }

    /// <summary>The description shown in the skills panel, a localization token.</summary>
    public string Description { get; set; }

    /// <summary>The skills panel icon; the game's default icon when null.</summary>
    public Sprite Icon { get; set; }

    /// <summary>Multiplier on all experience for the skill.</summary>
    public float IncreaseStep { get; set; } = 1f;

    /// <summary>The short name typed in the <c>mimir</c> console command; defaults to the last part of <see cref="Id" />.</summary>
    public string Key { get; set; }

    /// <summary>Scales gains so no level costs less than this many points; 0 keeps the game's own pace.</summary>
    public float EarlyLevelCost { get; set; }

    /// <summary>The experience each level needs; null keeps the game's curve. See <see cref="MimirSkill.LevelCurve" />.</summary>
    public Func<int, float> LevelCurve { get; set; }

    /// <summary>How much of the game's death penalty the skill takes; see <see cref="MimirSkill.DeathLoss" />.</summary>
    public float DeathLoss { get; set; } = 1f;

    /// <summary>Whether the skill fortifies; null leaves it to Mimir's setting. See <see cref="FortifySettings" />.</summary>
    public bool? Fortify { get; set; }
}
