using System.Collections.Generic;
using System.Globalization;
using System.Text;
using BepInEx.Configuration;
using Mimir.Core;
using UnityEngine;

namespace Mimir.Modules;

/// <summary>Which skills fortify.</summary>
internal enum FortifyMode
{
    ModsChoose,
    AllSkills,
    Off
}

/// <summary>
///     Keeps a fortified level per skill type for the loaded character: a slower level that trails the skill's and takes
///     a share of each gain, and that the skill falls back toward on death instead of the game's penalty. Which skills
///     fortify and the shares come from the <c>Fortify</c> settings or each skill's <see cref="FortifySettings" />.
///     Records are saved in the character's custom data under <c>Mimir.Fortify</c> as "type=level:progress;...",
///     written only when the character is saved; changing that format breaks existing saves.
/// </summary>
internal static class Fortifying
{
    private const string DataKey = "Mimir.Fortify";
    private const float MaxLevel = 100f;

    internal static ConfigEntry<FortifyMode> Mode;
    internal static ConfigEntry<float> RatePerLevel;
    internal static ConfigEntry<float> MaxRate;
    internal static ConfigEntry<float> ActiveRate;

    private static readonly Dictionary<Skills.SkillType, (float Level, float Progress)> _records = new();
    private static Player _readFor;
    private static string _readText;
    private static bool _unsaved;

    internal static bool Enabled(Skills.SkillType type)
    {
        FortifyMode mode = Mode?.Value ?? FortifyMode.ModsChoose;

        if (mode == FortifyMode.Off || type == Skills.SkillType.None)
        {
            return false;
        }

        return Registry.Find(type)?.Fortify.Enabled ?? mode == FortifyMode.AllSkills;
    }

    /// <summary>The multiplier on the skill's own gains; 1 when it does not fortify.</summary>
    internal static float ActiveMultiplier(Skills.SkillType type)
    {
        if (!Enabled(type))
        {
            return 1f;
        }

        float rate = Registry.Find(type)?.Fortify.ActiveRate ?? ActiveRate?.Value ?? 1f;
        return rate > 0f && !float.IsInfinity(rate) ? rate : 1f;
    }

    private static float Share(Skills.SkillType type, int levelsAbove)
    {
        FortifySettings settings = Registry.Find(type)?.Fortify;
        float perLevel = settings?.RatePerLevel ?? RatePerLevel?.Value ?? 0.1f;
        float max = settings?.MaxRate ?? MaxRate?.Value ?? 0.8f;
        float share = Mathf.Min(perLevel * levelsAbove, max);
        return share > 0f ? share : 0f;
    }

    internal static float LevelOf(Skills.SkillType type) => Peek(type).Level;

    internal static float ProgressOf(Skills.SkillType type) => Peek(type).Progress;

    /// <summary>The record, or what it would start at, without making one.</summary>
    private static (float Level, float Progress) Peek(Skills.SkillType type)
    {
        Player player = Player.m_localPlayer;

        if (!Enabled(type) || !Read() || !player.m_skills.m_skillData.TryGetValue(type, out Skills.Skill skill))
        {
            return (0f, 0f);
        }

        return _records.TryGetValue(type, out (float Level, float Progress) record) ? record : (Start(player.m_skills, skill.m_level), 0f);
    }

    /// <summary>Gives the fortified level its share of the points, never past the skill's own level; a skill that does not fortify drops its record.</summary>
    internal static void Gain(Skills skills, Skills.SkillType type, float points)
    {
        if (!Read())
        {
            return;
        }

        if (!Enabled(type))
        {
            Reset(type);
            return;
        }

        if (!(points > 0f) || float.IsInfinity(points) || !skills.m_skillData.TryGetValue(type, out Skills.Skill skill))
        {
            return;
        }

        float cap = Mathf.Min(MaxLevel, Mathf.Floor(skill.m_level));
        (float level, float progress) = Record(skills, type, skill.m_level);
        float share = Share(type, (int)cap - (int)Mathf.Min(level, cap));

        if (share <= 0f && level <= cap)
        {
            return;
        }

        progress += points * share;

        while (level < cap && progress >= CostAt(type, (int)level))
        {
            progress -= CostAt(type, (int)level);
            level++;
        }

        if (level >= cap)
        {
            level = cap;
            progress = 0f;
        }

        _records[type] = (level, progress);
        Write();
    }

    /// <summary>The level a skill falls back to on death: toward its fortified level, scaled by the world's death penalty.</summary>
    internal static float DeathLevel(Skills skills, Skills.SkillType type, float before)
    {
        Read();
        float fortified = Mathf.Min(before, Record(skills, type, before).Level);
        return before - (before - fortified) * Mathf.Clamp01(Game.m_skillReductionRate);
    }

    internal static void Reset(Skills.SkillType type)
    {
        if (Read() && _records.Remove(type))
        {
            Write();
        }
    }

    internal static void ResetAll()
    {
        if (Read() && _records.Count > 0)
        {
            _records.Clear();
            Write();
        }
    }

    /// <summary>A skill's record, created on first use at the level the game's death penalty would leave.</summary>
    private static (float Level, float Progress) Record(Skills skills, Skills.SkillType type, float level)
    {
        if (_records.TryGetValue(type, out (float Level, float Progress) record))
        {
            return record;
        }

        record = (Start(skills, level), 0f);
        _records[type] = record;
        Write();
        return record;
    }

    private static float Start(Skills skills, float level)
    {
        float loss = Mathf.Clamp01(skills.m_DeathLowerFactor * Game.m_skillReductionRate);
        return Mathf.Floor(level * (1f - loss));
    }

    private static float CostAt(Skills.SkillType type, int level)
    {
        MimirSkill skill = Registry.Find(type);
        return skill != null ? skill.CostAt(level) : Curves.Game(level);
    }

    /// <summary>Loads the records from the character's data; false with no character loaded.</summary>
    private static bool Read()
    {
        Player player = Player.m_localPlayer;

        if (!player || player.m_customData == null)
        {
            return false;
        }

        player.m_customData.TryGetValue(DataKey, out string text);

        if (ReferenceEquals(player, _readFor) && (_unsaved || ReferenceEquals(text, _readText)))
        {
            return true;
        }

        _readFor = player;
        _readText = text;
        _unsaved = false;
        _records.Clear();

        if (string.IsNullOrEmpty(text))
        {
            return true;
        }

        foreach (string entry in text.Split(';'))
        {
            string[] parts = entry.Split('=', ':');

            if (parts.Length == 3
                && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int type)
                && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float level)
                && float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float progress)
                && !float.IsNaN(level) && !float.IsNaN(progress) && !float.IsInfinity(progress))
            {
                _records[(Skills.SkillType)type] = (Mathf.Clamp(level, 0f, MaxLevel), Mathf.Max(0f, progress));
            }
        }

        return true;
    }

    /// <summary>Marks the records as changed; they are written to the character on the next save.</summary>
    private static void Write()
    {
        _readFor = Player.m_localPlayer;
        _unsaved = true;
    }

    /// <summary>Writes unsaved records to the character's data; does nothing for a character other than the one they were read for.</summary>
    internal static void Flush(Player player)
    {
        if (!_unsaved || !player || player.m_customData == null || !ReferenceEquals(player, _readFor))
        {
            return;
        }

        StringBuilder text = new();

        foreach (KeyValuePair<Skills.SkillType, (float Level, float Progress)> entry in _records)
        {
            if (text.Length > 0)
            {
                text.Append(';');
            }

            text.Append(((int)entry.Key).ToString(CultureInfo.InvariantCulture)).Append('=')
                .Append(entry.Value.Level.ToString("R", CultureInfo.InvariantCulture)).Append(':')
                .Append(entry.Value.Progress.ToString("R", CultureInfo.InvariantCulture));
        }

        _readText = text.ToString();
        _unsaved = false;
        player.m_customData[DataKey] = _readText;
    }

    /// <summary>Flushes unsaved records to the character they were read for, then drops the cache.</summary>
    internal static void Forget()
    {
        Flush(_readFor);
        _readFor = null;
        _readText = null;
        _unsaved = false;
    }
}
