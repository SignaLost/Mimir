using HarmonyLib;

namespace Mimir.Modules.Tracking.Patches;
/// <summary>Detects the local player placing a piece with a build tool. The game calls SetCreator on the placed copy, which is caught there.</summary>
[HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
internal static class Player_TryPlacePiece_Patch
{
    internal static bool Active;
    internal static Piece Placed;

    private static void Prefix()
    {
        Active = true;
        Placed = null;
    }

    private static void Postfix(Player __instance, bool __result)
    {
        Active = false;
        Piece placed = Placed;
        Placed = null;

        if (__result && placed && Local.Is(__instance))
        {
            Events.RaisePlaced(__instance, placed);
        }
    }
}

[HarmonyPatch(typeof(Piece), nameof(Piece.SetCreator))]
internal static class Piece_SetCreator_Patch
{
    private static void Postfix(Piece __instance)
    {
        if (Player_TryPlacePiece_Patch.Active)
        {
            Player_TryPlacePiece_Patch.Placed = __instance;
        }
    }
}

/// <summary>Detects the local player removing a piece. CanBeRemoved is the last check before removal, so the piece passing it is the one removed.</summary>
[HarmonyPatch(typeof(Player), "RemovePiece")]
internal static class Player_RemovePiece_Patch
{
    internal static bool Active;
    internal static Piece Removing;

    private static void Prefix()
    {
        Active = true;
        Removing = null;
    }

    private static void Postfix(Player __instance, bool __result)
    {
        Active = false;
        Piece removed = Removing;
        Removing = null;

        if (__result && removed && Local.Is(__instance))
        {
            Events.RaiseRemoved(__instance, removed);
        }
    }
}

[HarmonyPatch(typeof(Piece), nameof(Piece.CanBeRemoved))]
internal static class Piece_CanBeRemoved_Patch
{
    private static void Postfix(Piece __instance, bool __result)
    {
        if (Player_RemovePiece_Patch.Active && __result)
        {
            Player_RemovePiece_Patch.Removing = __instance;
        }
    }
}

/// <summary>Detects a damaged piece being repaired.</summary>
[HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Repair))]
internal static class WearNTear_Repair_Patch
{
    private static void Postfix(WearNTear __instance, bool __result)
    {
        Player player = Player.m_localPlayer;

        Piece piece = __result && player ? __instance.GetComponent<Piece>() : null;

        if (piece)
        {
            Events.RaiseRepaired(player, piece);
        }
    }
}
