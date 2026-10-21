using System;
using System.Collections.Generic;
using Mimir.Common;
using UnityEngine;

namespace Mimir.Core;
/// <summary>
///     Raises <see cref="Events.Killed" /> for the local player from the game's own kill message. The game that owns
///     the dying creature sends each player who damaged it a detail message just before, by the same route; the two
///     are matched by creature name, and details left unmatched expire after a few seconds.
/// </summary>
internal static class Kills
{
    private const string DetailRpc = "Mimir_KillDetail";

    /// <summary>Seconds after which unmatched details are dropped.</summary>
    private const float DetailLifetime = 10f;

    private static readonly List<(float Time, Kill Kill)> _details = new();

    internal static void Register()
    {
        _details.Clear();
        ZRoutedRpc.instance.Register<string, string, int, float, bool, int>(DetailRpc, RPC_Detail);
    }

    /// <summary>Sends the dying creature's details to each player who damaged it; does nothing unless this game owns it.</summary>
    internal static void SendDetails(Character victim)
    {
        ZNetView view = victim.m_nview;

        if (!view || !view.IsValid() || !view.IsOwner() || ZNet.instance == null || ZRoutedRpc.instance == null)
        {
            return;
        }

        ZDO zdo = view.GetZDO();
        string prefab = Utils.GetPrefabName(victim.gameObject);
        int level = victim.GetLevel();
        float maxHealth = victim.GetMaxHealth();
        Character killer = victim.m_lastHit?.GetAttacker();
        int skill = (int)(victim.m_lastHit?.m_skill ?? Skills.SkillType.None);

        foreach (ZNet.PlayerInfo info in ZNet.instance.GetPlayerList())
        {
            // The game records each attacker under s_attackers + player name.
            if (!zdo.GetBool(ZDOVars.s_attackers + info.m_name))
            {
                continue;
            }

            bool lastHit = killer is Player player && player.GetZDOID() == info.m_characterID;

            if (info.m_characterID == ZNet.instance.LocalPlayerCharacterID && Player.m_localPlayer)
            {
                RPC_Detail(0L, victim.m_name, prefab, level, maxHealth, lastHit, skill);
            }
            else
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(info.m_characterID.UserID, DetailRpc, victim.m_name, prefab, level, maxHealth, lastHit, skill);
            }
        }
    }

    /// <summary>Raises the kill from the game's kill message, with the details sent for it if any.</summary>
    internal static void Received(string name, int bossNumber, int weapon, int attackers, bool cheated)
    {
        Kill kill = Take(name) ?? new Kill { Name = name };
        kill.Boss = bossNumber > 0;
        kill.Weapon = (KillModifiers)weapon;
        kill.Attackers = attackers;
        kill.Cheated = cheated;

        if (Player.m_localPlayer)
        {
            Events.RaiseKilled(Player.m_localPlayer, kill);
        }
    }

    /// <summary>Removes and returns the details for this kill; null if none were received.</summary>
    private static Kill Take(string name)
    {
        Prune();

        for (int i = 0; i < _details.Count; i++)
        {
            if (_details[i].Kill.Name == name)
            {
                Kill kill = _details[i].Kill;
                _details.RemoveAt(i);
                return kill;
            }
        }

        return null;
    }

    private static void Prune()
    {
        float now = Time.time;

        for (int i = _details.Count - 1; i >= 0; i--)
        {
            if (now - _details[i].Time > DetailLifetime)
            {
                _details.RemoveAt(i);
            }
        }
    }

    private static void RPC_Detail(long sender, string name, string prefab, int level, float maxHealth, bool lastHit, int skill)
    {
        Prune();
        _details.Add((Time.time, new Kill
        {
            Name = name,
            Detailed = true,
            Prefab = prefab,
            Level = level,
            MaxHealth = maxHealth,
            LastHit = lastHit,
            Skill = (Skills.SkillType)skill
        }));
    }
}
