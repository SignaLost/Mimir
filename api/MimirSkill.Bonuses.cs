using System;
using System.Collections.Generic;
using Mimir.Core;

namespace Mimir;

public sealed partial class MimirSkill
{
    private readonly List<Bonus> _bonuses = new();
    private IReadOnlyList<Bonus> _bonusesView;
    private readonly List<Perk> _perks = new();
    private IReadOnlyList<Perk> _perksView;

    /// <summary>This skill's display-only perks, in the order added.</summary>
    public IReadOnlyList<Perk> Perks => _perksView ??= _perks.AsReadOnly();

    /// <summary>This skill's bonuses, in the order added.</summary>
    public IReadOnlyList<Bonus> Bonuses => _bonusesView ??= _bonuses.AsReadOnly();

    /// <summary>
    ///     Adds a passive bonus to a stat whose amount is a function of the skill's level, optionally only while an ability is held.
    ///     Throws for a bonus type the stat does not take.
    /// </summary>
    public Bonus AddBonus(Stat stat, BonusType type, Func<float, float> amountAtLevel, string requires = null)
    {
        if (amountAtLevel == null)
        {
            throw new ArgumentNullException(nameof(amountAtLevel));
        }

        if (!Enum.IsDefined(typeof(Stat), stat) || !Enum.IsDefined(typeof(BonusType), type))
        {
            throw new ArgumentOutOfRangeException(nameof(stat), $"Unknown stat or bonus type: {stat}, {type}.");
        }

        if (!StatTotals.Takes(stat, type))
        {
            throw new ArgumentException($"{stat} takes no {type} bonus.", nameof(type));
        }

        Bonus bonus = new(this, stat, type, amountAtLevel, AbilityId(requires));
        _bonuses.Add(bonus);
        StatTotals.Add(bonus);
        return bonus;
    }

    /// <summary>Adds a passive bonus that applies only while the ability is held. Throws for a null ability.</summary>
    public Bonus AddBonus(Stat stat, BonusType type, Func<float, float> amountAtLevel, Ability requires)
    {
        if (requires == null)
        {
            throw new ArgumentNullException(nameof(requires), "Use the overload without an ability for a bonus from level 0.");
        }

        return AddBonus(stat, type, amountAtLevel, requires.Id);
    }

    /// <summary>
    ///     Adds a display-only perk for an effect the mod applies itself, its amount a function of the skill's level,
    ///     optionally shown only while an ability is held.
    /// </summary>
    public Perk AddPerk(string name, Func<float, float> amountAtLevel, string unit = "", string requires = null)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException("A perk needs a name.", nameof(name));
        }

        Perk perk = new(this, name, amountAtLevel ?? throw new ArgumentNullException(nameof(amountAtLevel)), unit, AbilityId(requires));
        _perks.Add(perk);
        return perk;
    }

    /// <summary>Removes a perk of this skill; false when it is not one.</summary>
    public bool RemovePerk(Perk perk)
    {
        return perk != null && _perks.Remove(perk);
    }

    /// <summary>An ability's full id from its key or id; null for none.</summary>
    private string AbilityId(string requires)
    {
        if (string.IsNullOrEmpty(requires))
        {
            return null;
        }

        string lower = requires.ToLowerInvariant();
        return lower.StartsWith(Id + ".", StringComparison.Ordinal) ? lower : Id + "." + lower;
    }

    /// <summary>Removes a bonus of this skill; false when it is not one.</summary>
    public bool RemoveBonus(Bonus bonus)
    {
        if (bonus == null || !_bonuses.Remove(bonus))
        {
            return false;
        }

        StatTotals.Remove(bonus);
        return true;
    }
}
