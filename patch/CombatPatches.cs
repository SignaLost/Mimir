using HarmonyLib;
using Mimir.Common;
using Mimir.Core;

namespace Mimir.Patch;
/// <summary>Detects the local player starting an attack that passed the game's checks.</summary>
[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.StartAttack))]
internal static class Humanoid_StartAttack_Patch
{
    private static void Postfix(Humanoid __instance, bool secondaryAttack, bool __result)
    {
        if (__result && Local.Is(__instance) && __instance.GetCurrentWeapon() is { } weapon)
        {
            Events.RaiseAttacked((Player)__instance, weapon, secondaryAttack);
        }
    }
}

/// <summary>Detects the local player damaging a creature, on the attacker's game before the hit is sent to the target's owner.</summary>
[HarmonyPatch(typeof(Character), nameof(Character.Damage))]
internal static class Character_Damage_Patch
{
    private static void Postfix(Character __instance, HitData hit)
    {
        if (Local.Attacked(hit, out Player player) && !ReferenceEquals(__instance, player) && __instance.m_nview && __instance.m_nview.IsValid())
        {
            Events.RaiseDamageDealt(player, __instance, hit);
        }
    }
}

/// <summary>Detects the local player losing health to damage, by comparing health before and after.</summary>
[HarmonyPatch(typeof(Character), nameof(Character.ApplyDamage))]
internal static class Character_ApplyDamage_Patch
{
    private static void Prefix(Character __instance, out float __state)
    {
        __state = Local.Is(__instance) ? __instance.GetHealth() : 0f;
    }

    private static void Postfix(Character __instance, HitData hit, float __state)
    {
        if (!Local.Is(__instance))
        {
            return;
        }

        float lost = __state - __instance.GetHealth();

        if (lost > 0f)
        {
            Events.RaiseDamageTaken((Player)__instance, hit, lost);
        }
    }
}

/// <summary>
///     Detects the local player blocking or parrying a hit. BlockAttack returns true even when the block fails,
///     so a block counts only when the game reduced the hit with BlockDamage.
/// </summary>
[HarmonyPatch(typeof(Humanoid), "BlockAttack")]
internal static class Humanoid_BlockAttack_Patch
{
    internal static bool Reduced;

    private static void Prefix(Humanoid __instance, out (bool Parry, bool OuterReduced) __state)
    {
        ItemDrop.ItemData blocker = Local.Is(__instance) ? __instance.GetCurrentBlocker() : null;
        bool parry = blocker != null && blocker.m_shared.m_timedBlockBonus > 1f && __instance.m_blockTimer != -1f && __instance.m_blockTimer < 0.25f;
        __state = (parry, Reduced);
        Reduced = false;
    }

    private static void Postfix(Humanoid __instance, HitData hit, bool __result, (bool Parry, bool OuterReduced) __state)
    {
        bool reduced = Reduced;
        Reduced = __state.OuterReduced;

        if (__result && reduced && Local.Is(__instance))
        {
            Events.RaiseBlocked((Player)__instance, hit, __state.Parry);
        }
    }
}

[HarmonyPatch(typeof(HitData), nameof(HitData.BlockDamage))]
internal static class HitData_BlockDamage_Patch
{
    private static void Postfix()
    {
        if (BlockScope.Local)
        {
            Humanoid_BlockAttack_Patch.Reduced = true;
        }
    }
}

/// <summary>Detects the local player starting a dodge. A dodge that starts sets the queue timer to exactly 0.</summary>
[HarmonyPatch(typeof(Player), "UpdateDodge")]
internal static class Player_UpdateDodge_Patch
{
    private static void Prefix(Player __instance, float dt, out bool __state)
    {
        __state = Local.Is(__instance) && __instance.m_queuedDodgeTimer - dt > 0f;
    }

    private static void Postfix(Player __instance, bool __state)
    {
        if (__state && __instance.m_queuedDodgeTimer == 0f && __instance.m_dodgeInvincible)
        {
            Events.RaiseDodged(__instance);
        }
    }
}

/// <summary>Detects an attack passing through the local player while dodging, once per dodge.</summary>
[HarmonyPatch(typeof(Player), "RPC_HitWhileDodging")]
internal static class Player_RPC_HitWhileDodging_Patch
{
    private static void Prefix(Player __instance, out bool __state)
    {
        __state = Local.Is(__instance) && !__instance.m_beenHitWhileDodging;
    }

    private static void Postfix(Player __instance, bool __state)
    {
        if (__state && __instance.m_beenHitWhileDodging)
        {
            Events.RaiseDodgedHit(__instance);
        }
    }
}
