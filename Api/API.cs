using System;
using System.Collections.Generic;
using Jotunn.Configs;
using Jotunn.Managers;
using Mimir.Modules.Leveling;
using Mimir.Common;

namespace Mimir;

/// <summary>
///     Entry point for other mods: registers or tracks skills and returns the <see cref="MimirSkill" /> that reads and changes them.
///     Everything is for the local player and must be called from the main thread.
/// </summary>
public static class API
{
    public const int Version = 1;

    public const string Guid = MimirPlugin.PluginGuid;

    /// <summary>Raised for each ability unlocked, of any skill, whether or not notices show.</summary>
    public static event Action<MimirSkill, Ability> Unlocked;

    /// <summary>Raised when a skill Mimir keeps rises a level, by any means, with the new level.</summary>
    public static event Action<MimirSkill, int> LeveledUp;

    /// <summary>Every skill Mimir keeps, in the order registered.</summary>
    public static IReadOnlyList<MimirSkill> All => _all ??= Registry.Skills.AsReadOnly();

    private static IReadOnlyList<MimirSkill> _all;

    // ---- Setting up ----

    /// <summary>
    ///     Registers a new skill, or returns the one already registered under the same id.
    ///     Null, logged, for an invalid id or when Jotunn refuses it.
    /// </summary>
    public static MimirSkill Register(SkillDefinition definition)
    {
        string id = definition?.Id?.Trim().ToLowerInvariant();

        if (string.IsNullOrEmpty(id) || !ValidSkillId(id))
        {
            Log.LogWarning($"A skill needs an id such as 'mymod.fishing', of letters, digits, '_', '-' and '.'; '{definition?.Id}' ignored.");
            return null;
        }

        MimirSkill known = Registry.ById(id);

        if (known != null)
        {
            return known;
        }

        Skills.SkillType type;

        // Jotunn throws when the id clashes with a game skill or one already registered.
        try
        {
            type = SkillManager.Instance.AddSkill(new SkillConfig
            {
                Identifier = id,
                Name = definition.Name ?? id,
                Description = definition.Description ?? string.Empty,
                Icon = definition.Icon,
                IncreaseStep = definition.IncreaseStep
            });
        }
        catch (Exception e)
        {
            Log.LogError($"The skill {id} could not be registered: {e.Message}");
            return null;
        }

        if (type == Skills.SkillType.None)
        {
            return null;
        }

        MimirSkill skill = new(type, id, KeyFor(definition.Key ?? LastPart(id), id), definition.Name ?? id, definition.EarlyLevelCost)
        {
            LevelCurve = definition.LevelCurve,
            DeathLoss = definition.DeathLoss
        };

        skill.Fortify.Enabled = definition.Fortify;
        Registry.Add(skill);
        return skill;
    }

    /// <summary>
    ///     Takes on a skill Mimir did not register, or returns the one already tracked for the type.
    ///     Its id and default console name are the type's name, lowercased.
    /// </summary>
    public static MimirSkill Track(Skills.SkillType type, string key = null)
    {
        if (type == Skills.SkillType.None || type == Skills.SkillType.All)
        {
            return null;
        }

        MimirSkill skill = Registry.Find(type);

        if (skill != null)
        {
            return skill;
        }

        // The game names its skills $skill_<type>, and Jotunn names custom ones the same way.
        string id = type.ToString().ToLowerInvariant();
        skill = new MimirSkill(type, id, KeyFor(key ?? id, id), "$skill_" + id, 0f);
        Registry.Add(skill);
        return skill;
    }

    /// <summary>A skill by id or console name, ignoring case; null if Mimir does not keep it.</summary>
    public static MimirSkill Find(string skill) => Registry.Find(skill);

    /// <summary>A skill by type; null if Mimir does not keep it.</summary>
    public static MimirSkill Find(Skills.SkillType type) => Registry.Find(type);

    // ---- By name ----

    /// <summary>The level with status effects; 0 for an unknown skill.</summary>
    public static float Level(string skill) => Registry.Find(skill)?.Level ?? 0f;

    /// <summary>Whether the character has unlocked an ability, disabled or not.</summary>
    public static bool IsUnlocked(string skill, string id) => Registry.Find(skill)?.IsUnlocked(id) ?? false;

    /// <summary>Whether an ability can be used now: unlocked and not disabled.</summary>
    public static bool Has(string skill, string id)
    {
        MimirSkill found = Registry.Find(skill);
        return found != null && found.Has(found.GetAbility(id));
    }

    /// <summary>See <see cref="MimirSkill.AddAbility" />. False for an unknown skill or a taken id.</summary>
    public static bool AddAbility(string skill, string id, string name, float level, string unlockText = null, string description = null) =>
        Registry.Find(skill)?.AddAbility(id, name, level, unlockText, description) != null;

    /// <summary>See <see cref="MimirSkill.RemoveAbility" />.</summary>
    public static bool RemoveAbility(string skill, string id) => Registry.Find(skill)?.RemoveAbility(id) ?? false;

    /// <summary>See <see cref="MimirSkill.Unlock" />.</summary>
    public static bool Unlock(string skill, string id) => Registry.Find(skill)?.Unlock(id) ?? false;

    /// <summary>See <see cref="MimirSkill.Lock" />.</summary>
    public static bool Lock(string skill, string id) => Registry.Find(skill)?.Lock(id) ?? false;

    /// <summary>See <see cref="MimirSkill.Disable" />.</summary>
    public static bool Disable(string skill, string id, string source) => Registry.Find(skill)?.Disable(id, source) ?? false;

    /// <summary>See <see cref="MimirSkill.Enable" />.</summary>
    public static bool Enable(string skill, string id, string source) => Registry.Find(skill)?.Enable(id, source) ?? false;

    /// <summary>The level an ability unlocks at; -1 for an unknown skill or id.</summary>
    public static float UnlockLevel(string skill, string id) => Registry.Find(skill)?.UnlockLevel(id) ?? -1f;

    /// <summary>A skill's full ability ids in level order; empty for an unknown skill.</summary>
    public static string[] AbilityIds(string skill)
    {
        IReadOnlyList<Ability> abilities = Registry.Find(skill)?.Abilities;

        if (abilities == null)
        {
            return Array.Empty<string>();
        }

        string[] ids = new string[abilities.Count];

        for (int i = 0; i < ids.Length; i++)
        {
            ids[i] = abilities[i].Id;
        }

        return ids;
    }

    // ---- Experience ----

    /// <summary>Adds a multiplier on every gain of every skill; several modifiers multiply together.</summary>
    public static void AddGainModifier(Func<Player, Skills.SkillType, float> multiplier)
    {
        if (multiplier != null)
        {
            Gain.Modifiers.Add(multiplier);
        }
    }

    public static void RemoveGainModifier(Func<Player, Skills.SkillType, float> multiplier)
    {
        Gain.Modifiers.Remove(multiplier);
    }

    // ---- Internal ----

    internal static void RaiseUnlocked(MimirSkill skill, Ability ability)
    {
        skill.RaiseUnlocked(ability);
        Handlers.Invoke(Unlocked, $"API.Unlocked of '{ability.Id}'", skill, ability);
    }

    internal static void RaiseLeveledUp(MimirSkill skill, int level)
    {
        skill.RaiseLeveledUp(level);
        Handlers.Invoke(LeveledUp, "API.LeveledUp", skill, level);
    }

    private static bool ValidSkillId(string id)
    {
        foreach (char c in id)
        {
            if (!char.IsLetterOrDigit(c) && c != '_' && c != '-' && c != '.')
            {
                return false;
            }
        }

        return id[0] != '.' && id[id.Length - 1] != '.';
    }

    private static string LastPart(string id)
    {
        int dot = id.LastIndexOf('.');
        return dot >= 0 && dot < id.Length - 1 ? id.Substring(dot + 1) : id;
    }

    /// <summary>The wanted console name, or the id when another skill already has it.</summary>
    private static string KeyFor(string wanted, string id)
    {
        if (!Registry.KeyTaken(wanted))
        {
            return wanted;
        }

        Log.LogWarning($"The console name '{wanted}' is taken; {id} uses its id instead.");
        return id;
    }
}
