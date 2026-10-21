namespace Mimir;

/// <summary>A creature's death, for <see cref="Events.Killed" />. The <see cref="Detailed" /> members are empty when the game that owned the creature does not run Mimir.</summary>
public sealed class Kill
{
    /// <summary>The creature's name token.</summary>
    public string Name { get; internal set; }

    /// <summary>Whether it was a boss.</summary>
    public bool Boss { get; internal set; }

    /// <summary>How the players hurt it: melee, ranged, magic, unarmed, or mixed when several.</summary>
    public KillModifiers Weapon { get; internal set; }

    /// <summary>How many players damaged it.</summary>
    public int Attackers { get; internal set; }

    /// <summary>Whether cheats were used on it.</summary>
    public bool Cheated { get; internal set; }

    /// <summary>Whether the detailed members are known.</summary>
    public bool Detailed { get; internal set; }

    /// <summary>The creature's prefab name; null when not <see cref="Detailed" />.</summary>
    public string Prefab { get; internal set; }

    /// <summary>Its level, 1 plus one per star; 0 when not <see cref="Detailed" />.</summary>
    public int Level { get; internal set; }

    /// <summary>Its maximum health; 0 when not <see cref="Detailed" />.</summary>
    public float MaxHealth { get; internal set; }

    /// <summary>Whether this player landed the killing blow; false when not <see cref="Detailed" />.</summary>
    public bool LastHit { get; internal set; }

    /// <summary>The weapon skill of the killing blow; None when not <see cref="Detailed" /> or when a damaging effect finished it.</summary>
    public Skills.SkillType Skill { get; internal set; }
}
