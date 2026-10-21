using System;
using HarmonyLib;
using Mimir.Common;
using Mimir.Modules;

namespace Mimir.Patch;

/// <summary>Adds the Skill Abilities page each time the compendium rebuilds its pages.</summary>
[HarmonyPatch(typeof(TextsDialog), nameof(TextsDialog.UpdateTextsList))]
internal static class TextsDialog_UpdateTextsList_Patch
{
    private static void Postfix(TextsDialog __instance)
    {
        // A throw here would keep the compendium from opening.
        try
        {
            AbilityPage.Add(__instance);
        }
        catch (Exception e)
        {
            Log.LogWarning($"Skill Abilities page failed: {e.Message}");
        }
    }
}
