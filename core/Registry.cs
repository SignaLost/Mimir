using System;
using System.Collections.Generic;

namespace Mimir.Core;

/// <summary>Every skill Mimir keeps, in the order registered, with lookups by id, console name and skill type.</summary>
internal static class Registry
{
    internal static readonly List<MimirSkill> Skills = new();
    private static readonly Dictionary<global::Skills.SkillType, MimirSkill> _byType = new();

    internal static void Add(MimirSkill skill)
    {
        Skills.Add(skill);
        _byType[skill.Type] = skill;
        Unlocks.Dirty = true;
    }

    internal static MimirSkill ById(string id)
    {
        foreach (MimirSkill skill in Skills)
        {
            if (string.Equals(skill.Id, id, StringComparison.Ordinal))
            {
                return skill;
            }
        }

        return null;
    }

    /// <summary>A skill by id or console name, ignoring case; null if none matches.</summary>
    internal static MimirSkill Find(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        foreach (MimirSkill skill in Skills)
        {
            if (string.Equals(skill.Id, name, StringComparison.OrdinalIgnoreCase)
                || string.Equals(skill.Key, name, StringComparison.OrdinalIgnoreCase))
            {
                return skill;
            }
        }

        return null;
    }

    internal static MimirSkill Find(global::Skills.SkillType type)
    {
        return _byType.TryGetValue(type, out MimirSkill skill) ? skill : null;
    }

    internal static bool KeyTaken(string key)
    {
        foreach (MimirSkill skill in Skills)
        {
            if (string.Equals(skill.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
