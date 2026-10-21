using System;
using System.Collections.Generic;
using UnityEngine;
using Mimir.Common;
using Mimir.Modules.Leveling;

namespace Mimir;

/// <summary>A skill Mimir keeps for the local player: its level, abilities and bonuses. Readings are 0 while no character is loaded.</summary>
public sealed partial class MimirSkill
{
    private readonly List<Func<Player, float>> _gainModifiers = new();
    private float _gainRate = 1f;
    private float _level;
    private int _levelFrame = -1;
    private readonly float _earlyLevelCost;

    internal MimirSkill(Skills.SkillType type, string identifier, string key, string name, float earlyLevelCost)
    {
        Type = type;
        Id = identifier;
        Key = key;
        Name = name;
        UnlockKey = "Mimir.Abilities." + identifier;
        _earlyLevelCost = earlyLevelCost;
    }

    /// <summary>The game's skill type.</summary>
    public Skills.SkillType Type { get; }

    /// <summary>The skill's id; for a tracked skill, its type's name lowercased.</summary>
    public string Id { get; }

    /// <summary>The short name typed in the <c>mimir</c> console command.</summary>
    public string Key { get; }

    /// <summary>The name shown in game, a localization token.</summary>
    public string Name { get; }

    /// <summary>The character data key the unlocked abilities are kept under.</summary>
    internal string UnlockKey { get; }

    /// <summary>Whether gains are scaled, by pace or modifiers.</summary>
    internal bool ScalesGain => _earlyLevelCost > 0f || _gainModifiers.Count > 0 || !Mathf.Approximately(_gainRate, 1f);

    /// <summary>Raised for each ability unlocked, whether or not notices show.</summary>
    public event Action<Ability> Unlocked;

    // ---- Levels ----

    /// <summary>The level with status effects.</summary>
    public float Level
    {
        get
        {
            if (_levelFrame == Time.frameCount)
            {
                return _level;
            }

            Skills skills = LocalSkills();
            _level = skills ? skills.GetSkillLevel(Type) : 0f;
            _levelFrame = Time.frameCount;
            return _level;
        }
    }

    /// <summary><see cref="Level" /> scaled to 0 to 1.</summary>
    public float Factor
    {
        get
        {
            Skills skills = LocalSkills();
            return skills ? skills.GetSkillFactor(Type) : 0f;
        }
    }

    /// <summary>The level without status effects.</summary>
    public float StoredLevel => Entry(out Skills.Skill skill) ? skill.m_level : 0f;

    /// <summary>Experience banked toward the next level.</summary>
    public float Progress => Entry(out Skills.Skill skill) ? skill.m_accumulator : 0f;

    /// <summary>Experience the game requires for the next level; 1 before the character has any.</summary>
    public float LevelCost => Entry(out Skills.Skill skill) ? skill.GetNextLevelRequirement() : 1f;

    // ---- Changing ----

    /// <summary>Gives the player experience through the game's own path, with status effects, world rate and increase step applied.</summary>
    public void Raise(Player player, float amount)
    {
        if (player && amount > 0f && Type != Skills.SkillType.None)
        {
            player.RaiseSkill(Type, amount);
        }
    }

    /// <summary>Sets the level and clears progress toward the next one; false when no character is loaded.</summary>
    public bool SetLevel(float level)
    {
        Skills skills = LocalSkills();

        if (Type == Skills.SkillType.None || !skills)
        {
            return false;
        }

        level = Mathf.Clamp(level, 0f, 100f);

        // GetSkill creates the entry if the character has none yet.
        Skills.Skill skill = skills.GetSkill(Type);
        skill.m_level = level;
        skill.m_accumulator = 0f;
        _levelFrame = -1;
        Unlocks.Dirty = true;
        return true;
    }

    // ---- Experience ----

    /// <summary>A multiplier on every gain of the skill, 1 by default and never below 0.</summary>
    public float GainRate
    {
        get => _gainRate;
        set => _gainRate = float.IsNaN(value) || float.IsInfinity(value) ? 1f : Mathf.Max(0f, value);
    }

    /// <summary>Adds a multiplier on every gain of this skill; several modifiers multiply together.</summary>
    public void AddGainModifier(Func<Player, float> multiplier)
    {
        if (multiplier != null)
        {
            _gainModifiers.Add(multiplier);
        }
    }

    public void RemoveGainModifier(Func<Player, float> multiplier)
    {
        _gainModifiers.Remove(multiplier);
    }

    // ---- Internal ----

    /// <summary>Drops the cached level after a change within the frame.</summary>
    internal void LevelChanged()
    {
        _levelFrame = -1;
    }

    internal void RaiseUnlocked(Ability ability)
    {
        Handlers.Invoke(Unlocked, $"{Id}.Unlocked of '{ability.Id}'", ability);
    }

    /// <summary>This skill's own gain multiplier: its pace times its modifiers.</summary>
    internal float GainMultiplier(Player player)
    {
        float multiplier = Pace() * _gainRate;

        for (int i = 0; i < _gainModifiers.Count; i++)
        {
            try
            {
                multiplier *= _gainModifiers[i](player);
            }
            catch (Exception e)
            {
                Log.LogError($"A gain modifier for {Id} failed: {e.Message}");
            }
        }

        return multiplier;
    }

    /// <summary>The early-level gain multiplier at the current level: cost over <c>hypot(cost, EarlyLevelCost)</c>.</summary>
    internal float Pace()
    {
        if (_earlyLevelCost <= 0f)
        {
            return 1f;
        }

        float asked = LevelCost;
        return asked / Mathf.Sqrt(asked * asked + _earlyLevelCost * _earlyLevelCost);
    }

    internal bool Matches(string name)
    {
        return string.Equals(name, Id, StringComparison.OrdinalIgnoreCase)
               || string.Equals(name, Key, StringComparison.OrdinalIgnoreCase)
               || string.Equals(name, Type.ToString(), StringComparison.OrdinalIgnoreCase)
               || string.Equals(name, Name, StringComparison.OrdinalIgnoreCase)
               || string.Equals(name, Names.Localize(Name), StringComparison.OrdinalIgnoreCase);
    }

    private static Skills LocalSkills()
    {
        Player player = Player.m_localPlayer;
        return player ? player.GetSkills() : null;
    }

    private bool Entry(out Skills.Skill skill)
    {
        skill = null;
        Skills skills = LocalSkills();

        return Type != Skills.SkillType.None
               && skills
               && skills.m_skillData != null
               && skills.m_skillData.TryGetValue(Type, out skill)
               && skill != null;
    }
}
