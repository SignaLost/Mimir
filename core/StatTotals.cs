using System.Collections.Generic;
using UnityEngine;

namespace Mimir.Core;

/// <summary>
///     Totals every skill's <see cref="Bonus" /> amounts by stat, flat and percent, once a frame and for the local player
///     only. The stat patches call it to adjust the game's values; weapon stats are summed per call from the bonuses that
///     fit the weapon in use. It also decides which bonus types each stat takes.
/// </summary>
internal static class StatTotals
{
    private const int StatCount = (int)Stat.RestedDuration + 1;

    private static readonly List<Bonus> _all = new();
    private static float[] _flat = new float[StatCount];
    private static float[] _percent = new float[StatCount];
    private static float[] _nextFlat = new float[StatCount];
    private static float[] _nextPercent = new float[StatCount];
    private static int _frame = -1;
    private static int _version;
    private static int _refreshedVersion = -1;
    private static bool _refreshing;

    internal static void Add(Bonus bonus)
    {
        _all.Add(bonus);
        _version++;
    }

    internal static void Remove(Bonus bonus)
    {
        _all.Remove(bonus);
        _version++;
    }

    /// <summary>Whether a stat takes a bonus type; see <see cref="Stat" />.</summary>
    internal static bool Takes(Stat stat, BonusType type)
    {
        switch (stat)
        {
            case Stat.MaxEitr:
            case Stat.Comfort:
                return type == BonusType.Flat;
            case Stat.HealthRegen:
            case Stat.FoodDuration:
            case Stat.FallDamage:
            case Stat.Damage:
            case Stat.DrawTime:
            case Stat.ReloadTime:
            case Stat.Visibility:
            case Stat.Noise:
            case Stat.RestedDuration:
                return type == BonusType.Percent;
            default:
                return true;
        }
    }

    /// <summary>value × (1 + percent ÷ 100) + flat for the local player, with the weapon picking a weapon stat's bonuses; anyone else's value unchanged.</summary>
    internal static float Apply(Character character, float value, Stat stat, ItemDrop.ItemData weapon = null)
    {
        if (!Totals(character, stat, weapon, out float flat, out float percent))
        {
            return value;
        }

        float result = value * (1f + percent / 100f) + flat;

        return value < 0f ? result : Mathf.Max(Minimum(stat), result);
    }

    /// <summary>The percent total as a multiplier, never below 0.</summary>
    internal static float Multiplier(Character character, Stat stat, ItemDrop.ItemData weapon = null)
    {
        return Totals(character, stat, weapon, out _, out float percent) ? Mathf.Max(0f, 1f + percent / 100f) : 1f;
    }

    /// <summary>The flat total alone, for stats with no value to scale.</summary>
    internal static float Flat(Character character, Stat stat)
    {
        return Totals(character, stat, null, out float flat, out _) ? flat : 0f;
    }

    /// <summary>The floor of a stat's value: 1 for health, block power and the stagger limit, which the game divides by or dies at.</summary>
    private static float Minimum(Stat stat)
    {
        return stat is Stat.MaxHealth or Stat.BlockPower or Stat.StaggerLimit ? 1f : 0f;
    }

    private static bool IsWeaponStat(Stat stat) => stat >= Stat.Damage && stat <= Stat.ReloadTime;

    /// <summary>The flat and percent totals for the local player; false when nothing applies.</summary>
    private static bool Totals(Character character, Stat stat, ItemDrop.ItemData weapon, out float flat, out float percent)
    {
        flat = 0f;
        percent = 0f;

        if (!ReferenceEquals(character, Player.m_localPlayer) || character == null || _all.Count == 0)
        {
            return false;
        }

        Refresh();

        if (!IsWeaponStat(stat))
        {
            flat = _flat[(int)stat];
            percent = _percent[(int)stat];
            return flat != 0f || percent != 0f;
        }

        foreach (Bonus bonus in _all)
        {
            if (bonus.Stat != stat || !bonus.Fits(weapon))
            {
                continue;
            }

            if (bonus.Type == BonusType.Flat)
            {
                flat += bonus.Applied;
            }
            else
            {
                percent += bonus.Applied;
            }
        }

        return flat != 0f || percent != 0f;
    }

    /// <summary>Recomputes the totals once a frame, or after a bonus is added or removed; a bonus function reading a stat sees the previous totals.</summary>
    private static void Refresh()
    {
        if (_refreshing || (_frame == Time.frameCount && _refreshedVersion == _version))
        {
            return;
        }

        _refreshing = true;
        int version = _version;

        try
        {
            System.Array.Clear(_nextFlat, 0, StatCount);
            System.Array.Clear(_nextPercent, 0, StatCount);

            for (int i = 0; i < _all.Count; i++)
            {
                Bonus bonus = _all[i];
                bonus.Applied = bonus.Evaluate();
                float[] totals = bonus.Type == BonusType.Flat ? _nextFlat : _nextPercent;
                totals[(int)bonus.Stat] += bonus.Applied;
            }

            (_flat, _nextFlat) = (_nextFlat, _flat);
            (_percent, _nextPercent) = (_nextPercent, _percent);
        }
        finally
        {
            _refreshing = false;
        }

        _frame = Time.frameCount;
        _refreshedVersion = version;
    }
}
