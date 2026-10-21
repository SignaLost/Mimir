using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Mimir.Modules.Leveling.Patches;
/// <summary>Replaces the death penalty of a fortifying skill with its fortified level, and scales other kept skills' penalty by <see cref="MimirSkill.DeathLoss" />.</summary>
[HarmonyPatch(typeof(Skills), nameof(Skills.OnDeath))]
internal static class Skills_OnDeath_Patch
{
    private static void Prefix(Skills __instance, out Dictionary<Skills.SkillType, (float Level, float Progress)> __state)
    {
        __state = null;

        if (!__instance.m_player || __instance.m_player != Player.m_localPlayer)
        {
            return;
        }

        __state = new Dictionary<Skills.SkillType, (float, float)>();

        foreach (KeyValuePair<Skills.SkillType, Skills.Skill> entry in __instance.m_skillData)
        {
            __state[entry.Key] = (entry.Value.m_level, entry.Value.m_accumulator);
        }
    }

    private static void Postfix(Skills __instance, Dictionary<Skills.SkillType, (float Level, float Progress)> __state)
    {
        if (__state == null)
        {
            return;
        }

        Unlocks.Dirty = true;

        foreach (MimirSkill kept in Registry.Skills)
        {
            kept.LevelChanged();
        }

        foreach (KeyValuePair<Skills.SkillType, (float Level, float Progress)> entry in __state)
        {
            if (!__instance.m_skillData.TryGetValue(entry.Key, out Skills.Skill skill))
            {
                continue;
            }

            float before = entry.Value.Level;

            if (Fortifying.Enabled(entry.Key))
            {
                skill.m_level = Fortifying.DeathLevel(__instance, entry.Key, before);
            }
            else
            {
                Fortifying.Reset(entry.Key);

                if (Registry.Find(entry.Key) is { } kept && kept.DeathLoss != 1f)
                {
                    skill.m_level = Mathf.Max(0f, before - (before - skill.m_level) * kept.DeathLoss);
                }
            }

            // OnDeath clears the accumulator even when the level did not drop.
            if (skill.m_level >= before)
            {
                skill.m_accumulator = entry.Value.Progress;
            }
        }
    }
}

/// <summary>Drops the fortified level of a skill the game resets.</summary>
[HarmonyPatch(typeof(Skills), nameof(Skills.ResetSkill))]
internal static class Skills_ResetSkill_Patch
{
    private static void Postfix(Skills __instance, Skills.SkillType skillType)
    {
        if (__instance.m_player && __instance.m_player == Player.m_localPlayer)
        {
            Fortifying.Reset(skillType);
            Unlocks.Dirty = true;
        }
    }
}

/// <summary>Drops every fortified level when the game clears every skill.</summary>
[HarmonyPatch(typeof(Skills), nameof(Skills.Clear))]
internal static class Skills_Clear_Patch
{
    private static void Postfix(Skills __instance)
    {
        if (__instance.m_player && __instance.m_player == Player.m_localPlayer)
        {
            Fortifying.ResetAll();
            Unlocks.Dirty = true;
        }
    }
}

/// <summary>Replaces the game's level cost with the skill's own level curve.</summary>
[HarmonyPatch(typeof(Skills.Skill), "GetNextLevelRequirement")]
internal static class Skill_GetNextLevelRequirement_Patch
{
    private static void Postfix(Skills.Skill __instance, ref float __result)
    {
        if (__instance.m_info != null && Registry.Find(__instance.m_info.m_skill) is { LevelCurve: not null } skill)
        {
            __result = skill.CostAt(Mathf.FloorToInt(__instance.m_level));
        }
    }
}

/// <summary>Writes unsaved fortified levels before the character is saved.</summary>
[HarmonyPatch(typeof(Player), nameof(Player.Save))]
internal static class Player_Save_Fortify_Patch
{
    private static void Prefix(Player __instance)
    {
        Fortifying.Flush(__instance);
    }
}
