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

    private Action<Player, ItemDrop.ItemData, bool> _attacked;

    /// <summary>An attack started with this weapon type, with the weapon and whether it is the secondary attack.</summary>
    public event Action<Player, ItemDrop.ItemData, bool> Attacked
    {
        add => _attacked = Events.Wanting(Need.Attacks, _attacked, value);
        remove => _attacked -= value;
    }

    private Action<Player, Character, HitData> _hit;

    /// <summary>A hit with this weapon type landed on a creature or another player, before its resistances and armor.</summary>
    public event Action<Player, Character, HitData> Hit
    {
        add => _hit = Events.Wanting(Need.Hits, _hit, value);
        remove => _hit -= value;
    }

    private Action<Player, Kill> _killed;

    /// <summary>The player landed the killing blow with this weapon type; raised only when <see cref="Kill.Detailed" />.</summary>
    public event Action<Player, Kill> Killed
    {
        add => _killed += value;
        remove => _killed -= value;
    }

    internal void RaiseAttacked(Player player, ItemDrop.ItemData weapon, bool secondary)
    {
        if (_attacked != null)
        {
            Handlers.Invoke(_attacked, _attackedName, player, weapon, secondary);
        }
    }

    internal void RaiseHit(Player player, Character target, HitData hit)
    {
        if (_hit != null)
        {
            Handlers.Invoke(_hit, _hitName, player, target, hit);
        }
    }

    internal void RaiseKilled(Player player, Kill kill)
    {
        if (_killed != null)
        {
            Handlers.Invoke(_killed, _killedName, player, kill);
        }
    }
}
