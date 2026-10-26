using System;
using Mimir.Common;

namespace Mimir;

/// <summary>The events of one weapon type, raised alongside the general events in <see cref="Events" />.</summary>
public sealed class WeaponEvents
{
    private readonly string _attackedName;
    private readonly string _hitName;
    private readonly string _killedName;

    internal WeaponEvents(Skills.SkillType skill)
    {
        Skill = skill;
        _attackedName = "Events." + skill + ".Attacked";
        _hitName = "Events." + skill + ".Hit";
        _killedName = "Events." + skill + ".Killed";
    }

    /// <summary>The weapon skill these are for.</summary>
    public Skills.SkillType Skill { get; }

    /// <summary>An attack started with this weapon type, with the weapon and whether it is the secondary attack.</summary>
    public event Action<Player, ItemDrop.ItemData, bool> Attacked;

    /// <summary>A hit with this weapon type landed on a creature or another player, before its resistances and armor.</summary>
    public event Action<Player, Character, HitData> Hit;

    /// <summary>The player landed the killing blow with this weapon type; raised only when <see cref="Kill.Detailed" />.</summary>
    public event Action<Player, Kill> Killed;

    internal void RaiseAttacked(Player player, ItemDrop.ItemData weapon, bool secondary)
    {
        if (Attacked != null)
        {
            Handlers.Invoke(Attacked, _attackedName, player, weapon, secondary);
        }
    }

    internal void RaiseHit(Player player, Character target, HitData hit)
    {
        if (Hit != null)
        {
            Handlers.Invoke(Hit, _hitName, player, target, hit);
        }
    }

    internal void RaiseKilled(Player player, Kill kill)
    {
        if (Killed != null)
        {
            Handlers.Invoke(Killed, _killedName, player, kill);
        }
    }
}
