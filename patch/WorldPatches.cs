using HarmonyLib;
using Mimir.Core;
using UnityEngine;

namespace Mimir.Patch;
/// <summary>Counts the map pixels revealed by the game's regular explore.</summary>
[HarmonyPatch(typeof(Minimap), "Explore", typeof(Vector3), typeof(float))]
internal static class Minimap_Explore_Patch
{
    internal static bool Active;
    internal static int Revealed;

    private static void Prefix()
    {
        Active = true;
        Revealed = 0;
    }

    private static void Postfix()
    {
        Active = false;
        Player player = Player.m_localPlayer;

        if (Revealed > 0 && player)
        {
            Events.RaiseExplored(player, Revealed);
        }

        Revealed = 0;
    }
}

[HarmonyPatch(typeof(Minimap), "Explore", typeof(int), typeof(int))]
internal static class Minimap_ExplorePixel_Patch
{
    private static void Postfix(bool __result)
    {
        if (__result && Minimap_Explore_Patch.Active)
        {
            Minimap_Explore_Patch.Revealed++;
        }
    }
}

/// <summary>Detects the local player waking after sleeping.</summary>
[HarmonyPatch(typeof(Player), nameof(Player.SetSleeping))]
internal static class Player_SetSleeping_Patch
{
    private static void Prefix(Player __instance, bool sleep, out bool __state)
    {
        __state = !sleep && __instance.m_sleeping && Local.Is(__instance);
    }

    private static void Postfix(Player __instance, bool __state)
    {
        if (__state)
        {
            Events.RaiseSlept(__instance);
        }
    }
}

/// <summary>Detects the local player buying from a trader, by comparing coins before and after.</summary>
[HarmonyPatch(typeof(StoreGui), "BuySelectedItem")]
internal static class StoreGui_BuySelectedItem_Patch
{
    private static void Prefix(StoreGui __instance, out (Trader.TradeItem Item, int Coins) __state)
    {
        __state = Player.m_localPlayer && __instance.m_selectedItem != null ? (__instance.m_selectedItem, __instance.GetPlayerCoins()) : (null, 0);
    }

    private static void Postfix(StoreGui __instance, (Trader.TradeItem Item, int Coins) __state)
    {
        Player player = Player.m_localPlayer;

        if (__state.Item == null || !player)
        {
            return;
        }

        int paid = __state.Coins - __instance.GetPlayerCoins();

        if (paid > 0)
        {
            Events.RaiseBought(player, __state.Item, paid);
        }
    }
}

/// <summary>Detects the local player selling to a trader. The game sells the item GetSellableItem returns.</summary>
[HarmonyPatch(typeof(StoreGui), "SellItem")]
internal static class StoreGui_SellItem_Patch
{
    private static void Prefix(StoreGui __instance, out ItemDrop.ItemData __state)
    {
        __state = Player.m_localPlayer ? __instance.GetSellableItem() : null;
    }

    private static void Postfix(ItemDrop.ItemData __state)
    {
        Player player = Player.m_localPlayer;

        if (__state != null && player)
        {
            Events.RaiseSold(player, __state, __state.m_shared.m_value * __state.m_stack);
        }
    }
}

/// <summary>Raises the skill-raised event with the factor after Mimir's gain prefix scaled it, unless another mod blocked the gain.</summary>
[HarmonyPatch(typeof(Skills), nameof(Skills.RaiseSkill))]
internal static class Skills_RaiseSkill_Event_Patch
{
    private static void Postfix(Skills __instance, Skills.SkillType skillType, float factor, bool __runOriginal)
    {
        if (__runOriginal && skillType != Skills.SkillType.None && Local.Is(__instance.m_player))
        {
            Events.RaiseSkillRaised(__instance.m_player, skillType, factor);
        }
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.OnSkillLevelup))]
internal static class Player_OnSkillLevelup_Patch
{
    private static void Postfix(Player __instance, Skills.SkillType skill, float level)
    {
        if (Local.Is(__instance))
        {
            Events.RaiseLeveledUp(__instance, skill, Mathf.FloorToInt(level));
        }
    }
}
