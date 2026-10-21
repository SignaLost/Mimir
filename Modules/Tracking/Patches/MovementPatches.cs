using HarmonyLib;
using UnityEngine;

namespace Mimir.Modules.Tracking.Patches;
/// <summary>Detects a jump the local player made. Only Jump leads to OnJump through this path; launches call OnJump directly.</summary>
[HarmonyPatch(typeof(Character), nameof(Character.Jump))]
internal static class Character_Jump_Patch
{
    internal static bool Jumping;

    private static void Prefix(Character __instance)
    {
        Jumping = Local.Is(__instance);
    }

    private static void Postfix()
    {
        Jumping = false;
    }
}

[HarmonyPatch(typeof(Player), "OnJump")]
internal static class Player_OnJump_Patch
{
    private static void Postfix(Player __instance)
    {
        if (Character_Jump_Patch.Jumping && Local.Is(__instance))
        {
            Character_Jump_Patch.Jumping = false;
            Events.RaiseJumped(__instance);
        }
    }
}

/// <summary>Detects the local player landing from over 4 m, with the height read before landing resets it.</summary>
[HarmonyPatch(typeof(Character), "UpdateGroundContact")]
internal static class Character_UpdateGroundContact_Patch
{
    private const float HurtHeight = 4f;

    private static void Prefix(Character __instance, out float __state)
    {
        __state = Local.Is(__instance) && __instance.m_groundContact
            ? Mathf.Max(0f, __instance.m_maxAirAltitude - __instance.transform.position.y)
            : 0f;
    }

    private static void Postfix(Character __instance, float __state)
    {
        if (__state > HurtHeight)
        {
            Events.RaiseFell((Player)__instance, __state);
        }
    }
}
