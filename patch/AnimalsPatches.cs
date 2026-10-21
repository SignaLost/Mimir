using HarmonyLib;
using Mimir.Core;
using UnityEngine;

namespace Mimir.Patch;
/// <summary>Detects a creature becoming tamed on the game that owns it.</summary>
[HarmonyPatch(typeof(Tameable), "Tame")]
internal static class Tameable_Tame_Patch
{
    private static void Prefix(Tameable __instance, out bool __state)
    {
        __state = __instance.m_nview && __instance.m_nview.IsValid() && __instance.m_nview.IsOwner() && __instance.m_character
                  && !__instance.m_character.IsTamed();
    }

    private static void Postfix(Tameable __instance, bool __state)
    {
        if (__state && __instance.m_character.IsTamed())
        {
            Tames.Credit(__instance.m_character);
        }
    }
}

/// <summary>Detects the local player petting or commanding a tamed creature. Only a successful pet or command stamps m_lastPetTime.</summary>
[HarmonyPatch(typeof(Tameable), nameof(Tameable.Interact))]
internal static class Tameable_Interact_Patch
{
    private static void Prefix(Tameable __instance, out float __state)
    {
        __state = __instance.m_lastPetTime;
    }

    private static void Postfix(Tameable __instance, Humanoid user, bool alt, bool __result, float __state)
    {
        if (!__result || alt || __instance.m_lastPetTime == __state || !Local.Is(user) || !__instance.m_character)
        {
            return;
        }

        if (__instance.m_commandable)
        {
            Events.RaiseCommanded((Player)user, __instance.m_character);
        }
        else
        {
            Events.RaisePetted((Player)user, __instance.m_character);
        }
    }
}
