using System;
using Mimir.Common;

namespace Mimir;

/// <summary>A display-only effect a mod's own code gives with a skill, listed among the skill's perks; it changes nothing itself.</summary>
public sealed class Perk
{
    private readonly Func<float, float> _amount;
    private bool _failed;

    internal Perk(MimirSkill skill, string name, Func<float, float> amount, string unit, string requires)
    {
        Skill = skill;
        Name = name;
        Unit = unit ?? string.Empty;
        Requires = requires;
        _amount = amount;
    }

    /// <summary>The skill whose level sets the amount.</summary>
    public MimirSkill Skill { get; }

    /// <summary>The name shown, a localization token.</summary>
    public string Name { get; set; }

    /// <summary>Shown after the amount, such as "%"; empty for none.</summary>
    public string Unit { get; set; }

    /// <summary>The full id of the ability it needs, or null when it shows from level 0.</summary>
    public string Requires { get; }

    /// <summary>Hides it without removing it.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>The amount shown right now; 0 while disabled, before its ability, or with no character loaded.</summary>
    public float Current
    {
        get
        {
            if (!Enabled || _failed || !Player.m_localPlayer || (Requires != null && !Skill.Has(Skill.GetAbility(Requires))))
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
                Log.LogError($"A perk of {Skill.Id} failed and is hidden until the game restarts: {e}");
                return 0f;
            }
        }
    }
}
