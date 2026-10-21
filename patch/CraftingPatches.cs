using System.Collections.Generic;
using HarmonyLib;
using Mimir.Core;

namespace Mimir.Patch;
/// <summary>Detects the local player crafting or upgrading an item, caught as the game adds it under the recipe's name.</summary>
[HarmonyPatch(typeof(InventoryGui), "DoCrafting")]
internal static class InventoryGui_DoCrafting_Patch
{
    internal static Recipe Recipe;
    internal static ItemDrop.ItemData Made;
    internal static int Amount;

    // The upgraded item's quality; 0 when crafting.
    private static int _fromQuality;

    private static void Prefix(InventoryGui __instance)
    {
        Recipe = __instance.m_craftRecipe;
        Made = null;
        Amount = 0;
        _fromQuality = __instance.m_craftUpgradeItem?.m_quality ?? 0;
    }

    private static void Postfix(Player player)
    {
        Recipe recipe = Recipe;
        ItemDrop.ItemData made = Made;
        Recipe = null;
        Made = null;

        if (made == null || !recipe || !Local.Is(player))
        {
            return;
        }

        if (_fromQuality == 0)
        {
            Events.RaiseCrafted(player, recipe, made, Amount);
        }
        else if (made.m_quality > _fromQuality)
        {
            Events.RaiseUpgraded(player, recipe, made);
        }
    }
}

[HarmonyPatch(typeof(Inventory), nameof(Inventory.AddItem), typeof(string), typeof(int), typeof(int), typeof(int), typeof(long), typeof(string),
    typeof(Vector2i), typeof(bool), typeof(bool), typeof(bool))]
internal static class Inventory_AddItem_Crafted_Patch
{
    private static void Postfix(ItemDrop.ItemData __result, string name, int stack)
    {
        Recipe recipe = InventoryGui_DoCrafting_Patch.Recipe;

        if (__result == null || InventoryGui_DoCrafting_Patch.Made != null || !recipe || !recipe.m_item
            || name != recipe.m_item.gameObject.name)
        {
            return;
        }

        InventoryGui_DoCrafting_Patch.Made = __result;
        InventoryGui_DoCrafting_Patch.Amount = stack;
    }
}

/// <summary>Detects the local player repairing a worn item, by comparing durability before and after.</summary>
[HarmonyPatch(typeof(InventoryGui), "RepairOneItem")]
internal static class InventoryGui_RepairOneItem_Patch
{
    private static readonly List<ItemDrop.ItemData> _worn = new();
    private static readonly List<float> _durability = new();

    private static void Prefix()
    {
        _worn.Clear();
        _durability.Clear();

        if (Player.m_localPlayer)
        {
            Player.m_localPlayer.GetInventory().GetWornItems(_worn);

            foreach (ItemDrop.ItemData item in _worn)
            {
                _durability.Add(item.m_durability);
            }
        }
    }

    private static void Postfix()
    {
        Player player = Player.m_localPlayer;

        for (int i = 0; i < _worn.Count && player; i++)
        {
            if (_worn[i].m_durability > _durability[i])
            {
                Events.RaiseItemRepaired(player, _worn[i]);
                break;
            }
        }

        _worn.Clear();
        _durability.Clear();
    }
}

/// <summary>Detects the local player taking food off a cooking station. The game removes the first done slot.</summary>
[HarmonyPatch(typeof(CookingStation), "OnInteract")]
internal static class CookingStation_OnInteract_Patch
{
    private static void Prefix(CookingStation __instance, out string __state)
    {
        __state = null;

        if (!__instance.m_nview || !__instance.m_nview.IsValid())
        {
            return;
        }

        for (int i = 0; i < __instance.m_slots.Length; i++)
        {
            __instance.GetSlot(i, out string item, out _, out _, out _);

            if (item != "" && __instance.IsItemDone(item))
            {
                __state = item;
                return;
            }
        }
    }

    private static void Postfix(CookingStation __instance, Humanoid user, bool __result, string __state)
    {
        if (__result && __state != null && Local.Is(user))
        {
            bool burnt = __instance.m_overCookedItem && __state == __instance.m_overCookedItem.name;
            Events.RaiseCooked((Player)user, __instance, __state, burnt);
        }
    }
}

/// <summary>Detects the local player adding an item to a smelter. Given no item, the game picks one from the inventory with FindCookableItem.</summary>
[HarmonyPatch(typeof(Smelter), "OnAddOre")]
internal static class Smelter_OnAddOre_Patch
{
    private static void Prefix(Smelter __instance, Humanoid user, ItemDrop.ItemData item, out ItemDrop.ItemData __state)
    {
        __state = item ?? (Local.Is(user) ? __instance.FindCookableItem(user.GetInventory()) : null);
    }

    private static void Postfix(Smelter __instance, Humanoid user, bool __result, ItemDrop.ItemData __state)
    {
        if (__result && __state != null && Local.Is(user))
        {
            Events.RaiseRefined((Player)user, __instance, __state);
        }
    }
}

[HarmonyPatch(typeof(Smelter), "OnAddFuel")]
internal static class Smelter_OnAddFuel_Patch
{
    private static void Postfix(Smelter __instance, Humanoid user, bool __result)
    {
        if (__result && Local.Is(user))
        {
            Events.RaiseFueled((Player)user, __instance);
        }
    }
}

/// <summary>Detects the local player adding an item to a fermenter.</summary>
[HarmonyPatch(typeof(Fermenter), "AddItem")]
internal static class Fermenter_AddItem_Patch
{
    private static void Postfix(Fermenter __instance, Humanoid user, ItemDrop.ItemData item, bool __result)
    {
        if (__result && item != null && Local.Is(user))
        {
            Events.RaiseFermented((Player)user, __instance, item);
        }
    }
}

/// <summary>Detects the local player tapping a ready fermenter.</summary>
[HarmonyPatch(typeof(Fermenter), nameof(Fermenter.Interact))]
internal static class Fermenter_Interact_Patch
{
    private static void Prefix(Fermenter __instance, Humanoid user, bool hold, out bool __state)
    {
        __state = !hold && Local.Is(user) && __instance.m_nview && __instance.m_nview.IsValid()
                  && PrivateArea.CheckAccess(__instance.transform.position, 0f, flash: false)
                  && __instance.GetStatus() == Fermenter.Status.Ready;
    }

    private static void Postfix(Fermenter __instance, Humanoid user, bool __result, bool __state)
    {
        if (__result && __state && Local.First(__instance))
        {
            Events.RaiseTapped((Player)user, __instance);
        }
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.ConsumeItem))]
internal static class Player_ConsumeItem_Patch
{
    private static void Postfix(Player __instance, ItemDrop.ItemData item, bool __result)
    {
        if (__result && item != null && Local.Is(__instance))
        {
            Events.RaiseConsumed(__instance, item);
        }
    }
}
