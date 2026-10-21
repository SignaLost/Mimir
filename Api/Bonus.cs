using System;
using Mimir.Common;

namespace Mimir;

/// <summary>A passive change to one of the player's stats whose amount is a function of a skill's level.</summary>
public sealed class Bonus
{
    private readonly Func<float, float> _amount;
    private bool _failed;
    private Ability _required;
    private int _requiredVersion = -1;

    internal Bonus(MimirSkill skill, Stat stat, BonusType type, Func<float, float> amount, string requires)
    {
        Skill = skill;
        Stat = stat;
        Type = type;
        Requires = requires;
        _amount = amount;
    }

    /// <summary>The skill whose level sets the amount.</summary>
    public MimirSkill Skill { get; }

    public Stat Stat { get; }

    /// <summary>Whether the amount is in the stat's units or a percentage of its value.</summary>
    public BonusType Type { get; }

    /// <summary>The full id of the ability it needs, or null when it applies from level 0.</summary>
    public string Requires { get; }

    /// <summary>For a weapon stat, the one weapon skill type it applies to; None applies to every weapon type.</summary>
    public Skills.SkillType Weapon { get; set; } = Skills.SkillType.None;

    /// <summary>For a weapon stat, a test the weapon in use must also pass; null applies to every weapon. A filter that throws is logged and the bonus switched off.</summary>
    public Func<ItemDrop.ItemData, bool> WeaponFilter { get; set; }

    /// <summary>The name shown among the skill's perks, a localization token; null uses the stat's name.</summary>
    public string Name { get; set; }

    /// <summary>Switches it off without removing it.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>The amount it gives right now; 0 while disabled, before its ability, or with no character loaded.</summary>
    public float Current => Evaluate();

    /// <summary>The amount in this frame's totals.</summary>
    internal float Applied;

    /// <summary>Whether a weapon stat's bonus applies to the weapon in use; with no weapon, only an unrestricted one does.</summary>
    internal bool Fits(ItemDrop.ItemData weapon)
    {
        if (Weapon == Skills.SkillType.None && WeaponFilter == null)
        {
            return true;
        }

        if (_failed || weapon == null || (Weapon != Skills.SkillType.None && weapon.m_shared.m_skillType != Weapon))
        {
            return false;
        }

        if (WeaponFilter == null)
        {
            return true;
        }

        try
        {
            return WeaponFilter(weapon);
        }
        catch (Exception e)
        {
            _failed = true;
            Log.LogError($"A {Stat} bonus's weapon filter of {Skill.Id} failed and the bonus is off until the game restarts: {e}");
            return false;
        }
    }

    /// <summary>Whether the required ability, if any, can be used now.</summary>
    private bool Active()
    {
        if (Requires == null)
        {
            return true;
        }

        if (_requiredVersion != Skill.AbilitiesVersion)
        {
            _requiredVersion = Skill.AbilitiesVersion;
            _required = Skill.GetAbility(Requires);
        }

        return Skill.Has(_required);
    }

    /// <summary>The amount at the skill's level, or 0 when it should not apply. Never NaN or infinite.</summary>
    internal float Evaluate()
    {
        if (!Enabled || _failed || !Player.m_localPlayer || !Active())
        {
            return 0f;
        }

        try
        {
            float amount = _amount(Skill.Level);
            return float.IsNaN(amount) || float.IsInfinity(amount) ? 0f : amount;
        }
        catch (Exception e)
        {
            _failed = true;
            Log.LogError($"A {Stat} bonus of {Skill.Id} failed and is off until the game restarts: {e}");
            return 0f;
        }
    }
}
