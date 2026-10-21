using System.Collections.Generic;
using UnityEngine;

namespace Mimir.Core;

/// <summary>Checks used by the patches: whether a character or hit is the local player's, and whether an object was already counted recently.</summary>
internal static class Local
{
    private static readonly Dictionary<int, float> _until = new();
    private static readonly List<int> _stale = new();

    internal static bool Is(object character)
    {
        return character != null && ReferenceEquals(character, Player.m_localPlayer);
    }

    /// <summary>Whether the local player made the hit, compared by id.</summary>
    internal static bool Attacked(HitData hit, out Player player)
    {
        player = Player.m_localPlayer;
        return hit != null && player && !hit.m_attacker.IsNone() && hit.m_attacker == player.GetZDOID();
    }

    /// <summary>Counts the object once per window; false if it was counted within the window in seconds.</summary>
    internal static bool First(Object target, float window = 2f)
    {
        float now = Time.time;
        int id = target.GetInstanceID();

        if (_until.TryGetValue(id, out float until) && now < until)
        {
            return false;
        }

        if (_until.Count > 64)
        {
            Prune(now);
        }

        _until[id] = now + window;
        return true;
    }

    private static void Prune(float now)
    {
        foreach (KeyValuePair<int, float> entry in _until)
        {
            if (now >= entry.Value)
            {
                _stale.Add(entry.Key);
            }
        }

        foreach (int id in _stale)
        {
            _until.Remove(id);
        }

        _stale.Clear();
    }
}
