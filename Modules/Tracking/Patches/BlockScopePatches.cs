using System;
using HarmonyLib;

namespace Mimir.Modules.Tracking.Patches;
/// <summary>Tracks whether the local player's BlockAttack is running, restoring the outer state after a nested block.</summary>
[HarmonyPatch(typeof(Humanoid), "BlockAttack")]
internal static class BlockScope
{
    internal static bool Local { get; private set; }

    private static void Prefix(Humanoid __instance, out bool __state)
    {
        __state = Local;
        Local = ReferenceEquals(__instance, Player.m_localPlayer);
    }

    private static void Postfix(bool __state)
    {
        Local = __state;
    }

    /// <summary>Clears the state when BlockAttack throws.</summary>
    private static Exception Finalizer(Exception __exception)
    {
        if (__exception != null)
        {
            Local = false;
        }

        return __exception;
    }
}
