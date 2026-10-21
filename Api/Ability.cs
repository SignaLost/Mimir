namespace Mimir;

/// <summary>Something a skill grants at a level. Once unlocked it stays unlocked until a mod or the console locks it again.</summary>
public sealed class Ability
{
    internal Ability(MimirSkill skill, string key, string name, string unlockText, string description, float level, int order)
    {
        Skill = skill;
        Id = skill.Id + "." + key;
        Key = key;
        Name = name;
        UnlockText = unlockText;
        Description = description;
        Level = level;
        Order = order;
    }

    /// <summary>The full id, the skill's id followed by the ability's key; unlocks are saved under it.</summary>
    public string Id { get; }

    /// <summary>The ability's own part of the id, typed in the console after the skill's name.</summary>
    public string Key { get; }

    /// <summary>The name shown for it, a localization token.</summary>
    public string Name { get; }

    /// <summary>The notice shown when it unlocks, a localization token in which <c>$1</c> is the ability's name; null uses Mimir's default notice.</summary>
    public string UnlockText { get; }

    /// <summary>What it does, a localization token shown on the Skill Abilities page; null shows only the name.</summary>
    public string Description { get; set; }

    /// <summary>The level it unlocks at; changed with <see cref="MimirSkill.SetAbilityLevel" />.</summary>
    public float Level { get; internal set; }

    /// <summary>The order it was added in, which orders abilities at the same level.</summary>
    internal int Order { get; }

    internal MimirSkill Skill { get; }

    /// <summary>Whether the ability is still part of its skill.</summary>
    internal bool Registered = true;

    /// <summary>Whether the loaded character has the unlock, as of the skill's last read.</summary>
    internal bool Flagged;
}
