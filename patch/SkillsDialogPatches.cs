using System;
using HarmonyLib;
using Mimir.Common;
using Mimir.Modules;

namespace Mimir.Patch;

/// <summary>Adds fortified levels and bars to the game's skills panel each time it opens.</summary>
[HarmonyPatch(typeof(SkillsDialog), nameof(SkillsDialog.Setup))]
internal static class SkillsDialog_Setup_Patch
{
    private static void Postfix(SkillsDialog __instance, Player player)
    {
        // A throw here would leave the skills panel half drawn.
        try
        {
            FortifiedDisplay.ApplyVanilla(__instance, player);
        }
        catch (Exception e)
        {
            Log.LogWarning($"Fortified levels in the skills panel failed: {e.Message}");
        }
    }
}
