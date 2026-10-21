using System;
using HarmonyLib;
using Mimir.Core;
using Mimir.Modules;

namespace Mimir.Patch;
/// <summary>
///     Makes resetskill also remove the skill's unlocks, and with "all" every fortified level.
///     A postfix, since Jotunn's prefix handles custom skills and skips the original.
/// </summary>
[HarmonyPatch(typeof(Skills), nameof(Skills.CheatResetSkill))]
internal static class Skills_CheatResetSkill_Patch
{
    private static void Postfix(Skills __instance, string name)
    {
        bool all = string.Equals(name, "all", StringComparison.OrdinalIgnoreCase);

        foreach (MimirSkill skill in Registry.Skills)
        {
            if (all || skill.Matches(name))
            {
                __instance.ResetSkill(skill.Type);
                skill.RevokeAll();
            }
        }

        if (all)
        {
            Fortifying.ResetAll();
        }

        Unlocks.Dirty = true;
    }
}

/// <summary>Checks unlocks after the game's raiseskill, which sets the level directly.</summary>
[HarmonyPatch(typeof(Skills), nameof(Skills.CheatRaiseSkill))]
internal static class Skills_CheatRaiseSkill_Patch
{
    private static void Postfix()
    {
        foreach (MimirSkill skill in Registry.Skills)
        {
            skill.LevelChanged();
        }

        Unlocks.Dirty = true;
    }
}
