using System;
using System.Collections.Generic;
using HarmonyLib;
using Mimir.Common;
using Mimir.Core;
using Mimir.Modules;

namespace Mimir.Patch;
/// <summary>Scales every gain by the skill's pace, gain rate, modifiers and fortifying boost, then gives the fortified level its share.</summary>
[HarmonyPatch(typeof(Skills), nameof(Skills.RaiseSkill))]
internal static class Skills_RaiseSkill_Gain_Patch
{
    private static void Prefix(Skills __instance, Skills.SkillType skillType, ref float factor)
    {
        if (!__instance.m_player || __instance.m_player != Player.m_localPlayer)
        {
            return;
        }

        MimirSkill skill = Registry.Find(skillType);
        float multiplier = (skill != null && skill.ScalesGain ? skill.GainMultiplier(__instance.m_player) : 1f)
                           * Gain.Global(__instance.m_player, skillType)
                           * Fortifying.ActiveMultiplier(skillType);

        if (multiplier == 1f)
        {
            return;
        }

        if (Log.Shows(Log.InfoLevel.High))
        {
            Log.LogInfo($"{skill?.Key ?? skillType.ToString()}: +{factor:0.###} x {multiplier:0.###} (rate {Game.m_skillGainRate:0.##})",
                Log.InfoLevel.High);
        }

        factor *= multiplier > 0f && !float.IsInfinity(multiplier) ? multiplier : 0f;
    }

    /// <summary>Gives the fortified level its share of the points, computed as Skill.Raise does, before the fortifying boost.</summary>
    private static void Postfix(Skills __instance, Skills.SkillType skillType, float factor)
    {
        if (!__instance.m_player || __instance.m_player != Player.m_localPlayer
            || !__instance.m_skillData.TryGetValue(skillType, out Skills.Skill skill) || skill.m_info == null)
        {
            return;
        }

        Registry.Find(skillType)?.LevelChanged();
        Unlocks.Dirty = true;
        float points = skill.m_info.m_increseStep * factor * Game.m_skillGainRate;
        Fortifying.Gain(__instance, skillType, points / Fortifying.ActiveMultiplier(skillType));
    }
}
