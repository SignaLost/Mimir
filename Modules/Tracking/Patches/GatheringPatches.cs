using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Mimir.Common;
using UnityEngine;

namespace Mimir.Modules.Tracking.Patches;
/// <summary>Detects the local player picking a pickable, once per pickable until its picked state arrives.</summary>
[HarmonyPatch(typeof(Pickable), nameof(Pickable.Interact))]
internal static class Pickable_Interact_Patch
{
    private static void Prefix(Pickable __instance, Humanoid character, out bool __state)
    {
        __state = Local.Is(character) && __instance.m_nview && __instance.m_nview.IsValid() && __instance.m_enabled != 0 && !__instance.m_picked
                  && !InTar(__instance) && Local.First(__instance, 5f);
    }

    private static void Postfix(Pickable __instance, Humanoid character, bool __state)
    {
        if (__state)
        {
            Events.RaisePicked((Player)character, __instance);
        }
    }

    private static bool InTar(Pickable pickable)
    {
        if (!pickable.m_tarPreventsPicking)
        {
            return false;
        }

        Floating floating = pickable.m_floating ? pickable.m_floating : pickable.GetComponent<Floating>();
        return floating && floating.IsInTar();
    }
}

/// <summary>Detects the local player picking up an item, with the stack read before the inventory merges it.</summary>
[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.Pickup))]
internal static class Humanoid_Pickup_Patch
{
    private static void Prefix(Humanoid __instance, GameObject go, out (ItemDrop.ItemData Item, int Stack) __state)
    {
        __state = default;
        ItemDrop drop = go && Local.Is(__instance) ? go.GetComponent<ItemDrop>() : null;

        if (drop)
        {
            drop.Load();
            __state = (drop.m_itemData, drop.m_itemData.m_stack);
        }
    }

    private static void Postfix(Humanoid __instance, bool __result, (ItemDrop.ItemData Item, int Stack) __state)
    {
        if (__result && __state.Item != null && Local.Is(__instance))
        {
            Events.RaisePickedUp((Player)__instance, __state.Item, __state.Stack);
        }
    }
}

/// <summary>Detects the local player hitting a tree, rock or other destructible, on the attacker's game before the hit is sent to the owner.</summary>
[HarmonyPatch]
internal static class ObjectHit_Patch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        Type[] types = { typeof(TreeBase), typeof(TreeLog), typeof(MineRock), typeof(MineRock5), typeof(Destructible), typeof(WearNTear) };

        foreach (Type type in types)
        {
            MethodInfo method = AccessTools.DeclaredMethod(type, "Damage", new[] { typeof(HitData) });

            if (method != null)
            {
                yield return method;
            }
            else
            {
                Log.LogWarning($"Events: {type.Name}.Damage not found; hits on it are not reported.");
            }
        }
    }

    private static void Postfix(object __instance, HitData hit)
    {
        if (!Local.Attacked(hit, out Player player) || __instance is not IDestructible target)
        {
            return;
        }

        if (__instance is TreeBase or TreeLog || __instance is Destructible { m_destructibleType: DestructibleType.Tree })
        {
            Events.RaiseChopped(player, target, hit);
        }
        else if (__instance is MineRock or MineRock5 || (__instance is Destructible && hit.m_damage.m_pickaxe > 0f))
        {
            Events.RaiseMined(player, target, hit);
        }

        Events.RaiseObjectHit(player, target, hit);
    }
}

/// <summary>Detects the local player catching a fish.</summary>
[HarmonyPatch(typeof(FishingFloat), nameof(FishingFloat.Catch))]
internal static class FishingFloat_Catch_Patch
{
    private static void Postfix(Fish fish, Character owner)
    {
        if (fish && Local.Is(owner))
        {
            Events.RaiseFished((Player)owner, fish);
        }
    }
}

/// <summary>Detects the local player taking honey from a beehive. Interact returns true either way, so the amount and access are read first.</summary>
[HarmonyPatch(typeof(Beehive), nameof(Beehive.Interact))]
internal static class Beehive_Interact_Patch
{
    private static void Prefix(Beehive __instance, Humanoid character, bool repeat, out int __state)
    {
        __state = !repeat && Local.Is(character) && PrivateArea.CheckAccess(__instance.transform.position, 0f, flash: false)
            ? __instance.GetHoneyLevel()
            : 0;
    }

    private static void Postfix(Beehive __instance, Humanoid character, int __state)
    {
        if (__state > 0 && Local.First(__instance))
        {
            Events.RaiseHoneyCollected((Player)character, __instance, __state);
        }
    }
}

[HarmonyPatch(typeof(SapCollector), nameof(SapCollector.Interact))]
internal static class SapCollector_Interact_Patch
{
    private static void Prefix(SapCollector __instance, Humanoid character, bool repeat, out int __state)
    {
        __state = !repeat && Local.Is(character) && PrivateArea.CheckAccess(__instance.transform.position, 0f, flash: false)
            ? __instance.GetLevel()
            : 0;
    }

    private static void Postfix(SapCollector __instance, Humanoid character, int __state)
    {
        if (__state > 0 && Local.First(__instance))
        {
            Events.RaiseSapCollected((Player)character, __instance, __state);
        }
    }
}
