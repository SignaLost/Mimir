using System;
using HarmonyLib;
using Mimir.Common;
using Mimir.Core;

namespace Mimir.Patch;
/// <summary>Sends kill details from the owner's game. A prefix, so they leave before the game's own kill messages.</summary>
[HarmonyPatch(typeof(Character), nameof(Character.OnDeath))]
internal static class Character_OnDeath_Patch
{
    private static void Prefix(Character __instance)
    {
        try
        {
            Kills.SendDetails(__instance);
        }
        catch (Exception e)
        {
            Log.LogError($"Kill details failed: {e.Message}");
        }
    }
}

[HarmonyPatch(typeof(Game), nameof(Game.RPC_RegisterKill))]
internal static class Game_RPC_RegisterKill_Patch
{
    private static void Postfix(string enemyName, int bossNumber, int modifiers, int attackers, bool cheatsUsed)
    {
        Kills.Received(enemyName, bossNumber, modifiers, attackers, cheatsUsed);
    }
}

/// <summary>Registers Mimir's routed messages with each new connection.</summary>
[HarmonyPatch(typeof(Game), "Start")]
internal static class Game_Start_Patch
{
    private static void Postfix()
    {
        Register(Kills.Register, "Kill details");
        Register(Tames.Register, "Taming credit");
    }

    private static void Register(Action register, string what)
    {
        try
        {
            register();
        }
        catch (Exception e)
        {
            Log.LogError($"{what} could not be registered: {e.Message}");
        }
    }
}
