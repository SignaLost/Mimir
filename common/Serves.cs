using System;

namespace Mimir.Common;

/// <summary>What a patch class is there for, so it is applied only once some mod asks for that; a class with no such mark is always applied.</summary>
internal enum Need
{
    Attacks,
    Hits,
    Hurt,
    Blocking,
    Dodging,
    Jumping,
    Falling,
    Picking,
    PickingUp,
    ObjectHits,
    Fishing,
    Honey,
    Sap,
    Animals,
    Crafting,
    ItemRepairs,
    Cooking,
    Refining,
    Fermenting,
    Eating,
    Placing,
    Removing,
    PieceRepairs,
    Exploring,
    Sleeping,
    Trading,
    SkillRaises,
    LevelUps
}

/// <summary>Marks a patch class as serving events; it is applied when a mod subscribes to one of them.</summary>
[AttributeUsage(AttributeTargets.Class)]
internal sealed class ServesAttribute : Attribute
{
    internal ServesAttribute(params Need[] needs)
    {
        Needs = needs;
    }

    internal Need[] Needs { get; }
}

/// <summary>Marks a patch class as serving stats; it is applied when a mod adds a bonus to one of them.</summary>
[AttributeUsage(AttributeTargets.Class)]
internal sealed class ServesStatAttribute : Attribute
{
    internal ServesStatAttribute(params Stat[] stats)
    {
        Stats = stats;
    }

    internal Stat[] Stats { get; }
}
