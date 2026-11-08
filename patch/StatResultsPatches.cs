using HarmonyLib;
using Mimir.Common;
using Mimir.Core;
using UnityEngine;

namespace Mimir.Patch;
// Stats with no base value read by the game: the result is adjusted where it is returned.

/// <summary>Adds flat eitr bonuses to the food total.</summary>
[HarmonyPatch(typeof(Player), "GetTotalFoodValue")]
[ServesStat(Stat.MaxEitr)]
internal static class Player_GetTotalFoodValue_Patch
{
    private static void Postfix(Player __instance, ref float eitr)
    {
        eitr = Mathf.Max(0f, eitr + StatTotals.Flat(__instance, Stat.MaxEitr));
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.GetBodyArmor))]
[ServesStat(Stat.Armor)]
internal static class Player_GetBodyArmor_Patch
{
    private static void Postfix(Player __instance, ref float __result)
    {
        __result = StatTotals.Apply(__instance, __result, Stat.Armor);
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.GetMaxAdrenaline))]
[ServesStat(Stat.MaxAdrenaline)]
internal static class Player_GetMaxAdrenaline_Patch
{
    private static void Postfix(Player __instance, ref float __result)
    {
        __result = StatTotals.Apply(__instance, __result, Stat.MaxAdrenaline);
    }
}

[HarmonyPatch(typeof(Character), "GetStaggerTreshold")]
[ServesStat(Stat.StaggerLimit)]
internal static class Character_GetStaggerTreshold_Patch
{
    private static void Postfix(Character __instance, ref float __result)
    {
        __result = StatTotals.Apply(__instance, __result, Stat.StaggerLimit);
    }
}

/// <summary>Adds flat comfort bonuses to the comfort level, never below the game's minimum of 1.</summary>
[HarmonyPatch(typeof(SE_Rested), nameof(SE_Rested.CalculateComfortLevel), typeof(Player))]
[ServesStat(Stat.Comfort)]
internal static class SE_Rested_CalculateComfortLevel_Patch
{
    private static void Postfix(Player player, ref int __result)
    {
        __result = Mathf.Max(1, __result + Mathf.RoundToInt(StatTotals.Flat(player, Stat.Comfort)));
    }
}

/// <summary>Scales build stamina; a free tool stays free.</summary>
[HarmonyPatch(typeof(Player), "GetBuildStamina")]
[ServesStat(Stat.BuildStamina)]
internal static class Player_GetBuildStamina_Patch
{
    private static void Postfix(Player __instance, ref float __result)
    {
        if (__result > 0f)
        {
            __result = StatTotals.Apply(__instance, __result, Stat.BuildStamina);
        }
    }
}

/// <summary>Scales the damage of every hit of an attack by the weapon's damage bonuses.</summary>
[HarmonyPatch(typeof(Attack), "ModifyDamage")]
[ServesStat(Stat.Damage)]
internal static class Attack_ModifyDamage_Patch
{
    private static void Postfix(Attack __instance, HitData hitData)
    {
        if (hitData == null || __instance.m_weapon == null)
        {
            return;
        }

        float multiplier = StatTotals.Multiplier(__instance.m_character, Stat.Damage, __instance.m_weapon);

        if (multiplier != 1f)
        {
            hitData.m_damage.Modify(multiplier);
        }
    }
}

[HarmonyPatch(typeof(Attack), "GetAttackStamina")]
[ServesStat(Stat.AttackStamina)]
internal static class Attack_GetAttackStamina_Patch
{
    private static void Postfix(Attack __instance, ref float __result)
    {
        if (__result > 0f && __instance.m_weapon != null)
        {
            __result = StatTotals.Apply(__instance.m_character, __result, Stat.AttackStamina, __instance.m_weapon);
        }
    }
}

[HarmonyPatch(typeof(Attack), "GetAttackHealth")]
[ServesStat(Stat.AttackHealth)]
internal static class Attack_GetAttackHealth_Patch
{
    private static void Postfix(Attack __instance, ref float __result)
    {
        if (__result > 0f && __instance.m_weapon != null)
        {
            __result = StatTotals.Apply(__instance.m_character, __result, Stat.AttackHealth, __instance.m_weapon);
        }
    }
}

/// <summary>Scales attack eitr cost in the public overload, which the attack and tooltips both use.</summary>
[HarmonyPatch(typeof(Attack), nameof(Attack.GetAttackEitr), typeof(Character), typeof(ItemDrop.ItemData))]
[ServesStat(Stat.AttackEitr)]
internal static class Attack_GetAttackEitr_Patch
{
    private static void Postfix(Character character, ItemDrop.ItemData weapon, ref float __result)
    {
        if (__result > 0f && weapon != null)
        {
            __result = StatTotals.Apply(character, __result, Stat.AttackEitr, weapon);
        }
    }
}

/// <summary>Scales crossbow reload time; the game only calls this for the local player.</summary>
[HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetWeaponLoadingTime))]
[ServesStat(Stat.ReloadTime)]
internal static class ItemData_GetWeaponLoadingTime_Patch
{
    private static void Postfix(ItemDrop.ItemData __instance, ref float __result)
    {
        if (__instance.m_shared.m_attack.m_requiresReload)
        {
            __result *= StatTotals.Multiplier(Player.m_localPlayer, Stat.ReloadTime, __instance);
        }
    }
}

/// <summary>Scales block power while the local player is blocking.</summary>
[HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetBlockPower), typeof(int), typeof(float))]
[ServesStat(Stat.BlockPower)]
internal static class ItemData_GetBlockPower_Patch
{
    private static void Postfix(ref float __result)
    {
        if (BlockScope.Local)
        {
            __result = StatTotals.Apply(Player.m_localPlayer, __result, Stat.BlockPower);
        }
    }
}

/// <summary>Scales the block power shown in the local player's tooltip.</summary>
[HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetBlockPowerTooltip), typeof(int))]
[ServesStat(Stat.BlockPower)]
internal static class ItemData_GetBlockPowerTooltip_Patch
{
    private static void Postfix(ref float __result)
    {
        __result = StatTotals.Apply(Player.m_localPlayer, __result, Stat.BlockPower);
    }
}

[HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyFallDamage))]
[ServesStat(Stat.FallDamage)]
internal static class SEMan_ModifyFallDamage_Patch
{
    private static void Postfix(SEMan __instance, ref float damage)
    {
        damage *= StatTotals.Multiplier(__instance.m_character, Stat.FallDamage);
    }
}

[HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyStealth))]
[ServesStat(Stat.Visibility)]
internal static class SEMan_ModifyStealth_Patch
{
    private static void Postfix(SEMan __instance, ref float stealth)
    {
        stealth *= StatTotals.Multiplier(__instance.m_character, Stat.Visibility);
    }
}

[HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyNoise))]
[ServesStat(Stat.Noise)]
internal static class SEMan_ModifyNoise_Patch
{
    private static void Postfix(SEMan __instance, ref float noise)
    {
        noise *= StatTotals.Multiplier(__instance.m_character, Stat.Noise);
    }
}
