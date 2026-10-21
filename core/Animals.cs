using UnityEngine;

namespace Mimir.Core;
/// <summary>Credits a finished taming to the closest player within 30 m, directly or by a message to that player's game.</summary>
internal static class Tames
{
    private const string TamedRpc = "Mimir_Tamed";
    private const float CreditRange = 30f;

    internal static void Register()
    {
        ZRoutedRpc.instance.Register<ZDOID>(TamedRpc, RPC_Tamed);
    }

    internal static void Credit(Character creature)
    {
        Player closest = Player.GetClosestPlayer(creature.transform.position, CreditRange);

        if (!closest)
        {
            return;
        }

        if (closest == Player.m_localPlayer)
        {
            Events.RaiseTamed(closest, creature);
        }
        else if (ZRoutedRpc.instance != null)
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(closest.GetZDOID().UserID, TamedRpc, creature.GetZDOID());
        }
    }

    private static void RPC_Tamed(long sender, ZDOID id)
    {
        Player player = Player.m_localPlayer;
        GameObject instance = ZNetScene.instance ? ZNetScene.instance.FindInstance(id) : null;
        Character creature = instance ? instance.GetComponent<Character>() : null;

        if (player && creature)
        {
            Events.RaiseTamed(player, creature);
        }
    }
}
