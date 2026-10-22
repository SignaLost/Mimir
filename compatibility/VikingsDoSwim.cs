using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Mimir.Common;
using Mimir.Patch;

namespace Mimir.Compatibility;

/// <summary>
///     VikingsDoSwim replaces the game's <c>Character.UpdateSwimming</c> and <c>Player.OnSwimming</c> for players with its
///     own prefixes, which read swim speed, swim stamina and stamina regen themselves. This applies Mimir's bonuses to
///     those reads too. Each read stays in place; the player is copied on the stack before it and passed to the bonus.
/// </summary>
internal static class VikingsDoSwim
{
    internal const string Guid = "blacks7ar.VikingsDoSwim";

    private static Dictionary<FieldInfo, MethodInfo> _reads;

    internal static float SwimSpeed(Character character, float value) => StatReads.SwimSpeed(value, character);
    internal static float SwimStamina(Player player, float value) => StatReads.SwimStamina(value, player);
    internal static float StaminaRegen(Player player, float value) => StatReads.StaminaRegen(value, player);

    /// <summary>VikingsDoSwim's swimming prefixes that are found; none when it is not installed.</summary>
    internal static IEnumerable<MethodBase> Targets()
    {
        Type character = AccessTools.TypeByName("VikingsDoSwim.Patches.CharacterPatch");
        Type player = AccessTools.TypeByName("VikingsDoSwim.Patches.PlayerPatch");
        Type onSwimming = player != null ? AccessTools.Inner(player, "OnSwimmingPatch") : null;

        MethodInfo updateSwimming = character != null ? AccessTools.Method(character, "UpdateSwimming_Prefix") : null;
        MethodInfo swimmingPrefix = onSwimming != null ? AccessTools.Method(onSwimming, "Prefix") : null;

        if (updateSwimming != null)
        {
            yield return updateSwimming;
        }

        if (swimmingPrefix != null)
        {
            yield return swimmingPrefix;
        }
    }

    /// <summary>Adds Mimir's bonus after each stat read in one of VikingsDoSwim's prefixes.</summary>
    internal static IEnumerable<CodeInstruction> Rewrite(IEnumerable<CodeInstruction> instructions, MethodBase original)
    {
        _reads ??= new Dictionary<FieldInfo, MethodInfo>
        {
            [AccessTools.Field(typeof(Character), nameof(Character.m_swimSpeed))] = AccessTools.Method(typeof(VikingsDoSwim), nameof(SwimSpeed)),
            [AccessTools.Field(typeof(Player), nameof(Player.m_swimStaminaDrainMinSkill))] = AccessTools.Method(typeof(VikingsDoSwim), nameof(SwimStamina)),
            [AccessTools.Field(typeof(Player), nameof(Player.m_swimStaminaDrainMaxSkill))] = AccessTools.Method(typeof(VikingsDoSwim), nameof(SwimStamina)),
            [AccessTools.Field(typeof(Player), nameof(Player.m_staminaRegen))] = AccessTools.Method(typeof(VikingsDoSwim), nameof(StaminaRegen))
        };

        List<CodeInstruction> codes = new();
        int adjusted = 0;

        foreach (CodeInstruction code in instructions)
        {
            if (code.opcode != OpCodes.Ldfld || code.operand is not FieldInfo field || !_reads.TryGetValue(field, out MethodInfo hook))
            {
                codes.Add(code);
                continue;
            }

            // A jump to the read must now land on the dup, or the stack would be one short.
            CodeInstruction dup = new(OpCodes.Dup);
            code.MoveLabelsTo(dup);
            codes.Add(dup);
            codes.Add(code);
            codes.Add(new CodeInstruction(OpCodes.Call, hook));
            adjusted++;
        }

        if (adjusted == 0)
        {
            Log.LogWarning($"VikingsDoSwim's {original.DeclaringType?.Name}.{original.Name} has no stat reads Mimir knows; its swimming keeps no Mimir bonuses.");
        }

        return codes;
    }
}
