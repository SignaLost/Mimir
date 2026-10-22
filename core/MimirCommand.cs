using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Jotunn.Entities;
using Mimir.Common;
using Mimir.Modules;

namespace Mimir.Core;

/// <summary>
///     The <c>mimir</c> console command, registered through Jotunn. <c>list</c> and <c>stats</c> show every kept skill
///     or one skill's level, fortified level, abilities and bonuses, open to all. <c>level</c>, <c>unlock</c>,
///     <c>reset</c> and <c>fortify</c> change the local character's level, unlocks or fortified levels and require devcommands.
/// </summary>
internal sealed class MimirCommand : ConsoleCommand
{
    private const string Rule = "--------------------";

    private static readonly string[] Subcommands = { "help", "list", "stats", "level", "unlock", "reset", "fortify" };

    public override string Name => "mimir";

    public override string Help => Names.Localize(Tokens.CmdHelp);

    public override List<string> CommandOptionList()
    {
        return Subcommands.ToList();
    }

    public override void Run(string[] args, Terminal context)
    {
        if (context == null)
        {
            return;
        }

        string sub = args.Length > 0 ? args[0].ToLowerInvariant() : "help";
        string skillName = args.Length > 1 ? args[1] : null;
        string value = args.Length > 2 ? args[2].ToLowerInvariant() : null;

        switch (sub)
        {
            case "help":
                ShowHelp(context);
                break;
            case "list":
                ShowList(context);
                break;
            case "stats":
                WithSkill(context, skillName, skill => ShowStats(context, skill));
                break;
            case "level" when Allowed(context):
                WithSkill(context, skillName, skill => SetLevel(context, skill, value));
                break;
            case "unlock" when Allowed(context):
                WithSkill(context, skillName, skill => UnlockAbilities(context, skill, value));
                break;
            case "reset" when Allowed(context):
                WithSkill(context, skillName, skill => Reset(context, skill, value));
                break;
            case "fortify" when Allowed(context):
                Fortify(context, skillName, value);
                break;
            case "level" or "unlock" or "reset" or "fortify":
                break;
            default:
                context.AddString(Names.Localize(Tokens.CmdUnknown, sub));
                ShowHelp(context);
                break;
        }
    }

    private static bool Allowed(Terminal context)
    {
        if (context.IsCheatsEnabled())
        {
            return true;
        }

        context.AddString(Names.Localize(Tokens.CmdNeedsCheats));
        return false;
    }

    private static void WithSkill(Terminal context, string name, Action<MimirSkill> then)
    {
        MimirSkill skill = Registry.Find(name);

        if (skill != null)
        {
            then(skill);
            return;
        }

        context.AddString(Names.Localize(Tokens.CmdUnknownSkill, name ?? string.Empty));
        context.AddString("  " + Names.Localize(Tokens.CmdSkills, SkillList()));
    }

    private static void ShowHelp(Terminal context)
    {
        context.AddString(Names.Localize(Tokens.CmdHelpList));
        context.AddString(Names.Localize(Tokens.CmdHelpStats));
        context.AddString(Names.Localize(Tokens.CmdHelpLevel));
        context.AddString(Names.Localize(Tokens.CmdHelpUnlock));
        context.AddString(Names.Localize(Tokens.CmdHelpReset));
        context.AddString(Names.Localize(Tokens.CmdHelpFortify));
        context.AddString("  " + Names.Localize(Tokens.CmdSkills, SkillList()));
    }

    private static void ShowList(Terminal context)
    {
        if (Registry.Skills.Count == 0)
        {
            context.AddString(Names.Localize(Tokens.CmdNoSkills));
            return;
        }

        foreach (MimirSkill skill in Registry.Skills)
        {
            context.AddString($"{skill.Key}: {Names.Localize(skill.Name)} ({Number(skill.StoredLevel, "0.#")})");
        }
    }

    private static void ShowStats(Terminal context, MimirSkill skill)
    {
        context.AddString(Names.Localize(skill.Name) + ":");
        context.AddString(Rule);
        context.AddString($"{Names.Localize(Tokens.CmdCurrentLevel)}: {Number(skill.Level, "0.#")}");
        context.AddString($"{Names.Localize(Tokens.CmdProgress)}: {Number(skill.Progress, "0.#")}/{Number(skill.LevelCost, "0.#")}");

        if (skill.Fortify.Active)
        {
            float fortified = skill.Fortify.Level;
            string progress = $"{Number(skill.Fortify.Progress, "0.#")}/{Number(skill.CostAt((int)fortified), "0.#")}";
            context.AddString($"{Names.Localize(Tokens.CmdFortified)}: {Number(fortified, "0")} ({progress})");
        }

        if (skill.Abilities.Count > 0)
        {
            context.AddString(string.Empty);
        }

        foreach (Ability ability in skill.Abilities)
        {
            string mark = skill.IsUnlocked(ability.Id) ? "[x]" : "[ ]";
            string level = Names.Localize(Tokens.CmdLevelShort, Number(ability.Level, "0.#"));
            string disabled = skill.IsDisabled(ability.Id) ? ", " + Names.Localize(Tokens.CmdDisabled) : string.Empty;
            context.AddString($"{mark} {Names.Localize(ability.Name)} ({ability.Key}, {level}{disabled})");
        }

        if (skill.Bonuses.Count > 0)
        {
            context.AddString(string.Empty);
        }

        foreach (Bonus bonus in skill.Bonuses)
        {
            string unit = bonus.Type == BonusType.Percent ? "%" : string.Empty;
            string disabled = bonus.Enabled ? string.Empty : $" ({Names.Localize(Tokens.CmdDisabled)})";
            string type = bonus.Weapon != Skills.SkillType.None ? bonus.Weapon.ToString() : null;
            string filter = bonus.WeaponFilter != null ? "filtered" : null;
            string weapon = type != null || filter != null ? $" ({string.Join(", ", new[] { type, filter }.Where(part => part != null))})" : string.Empty;
            context.AddString($"{bonus.Stat}{weapon}: {Number(bonus.Current, "+0.##;-0.##;0")}{unit}{disabled}");
        }
    }

    /// <summary>Sets the level, or changes it by a signed amount.</summary>
    private static void SetLevel(Terminal context, MimirSkill skill, string value)
    {
        if (value == null || !float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float amount))
        {
            context.AddString(Names.Localize(Tokens.CmdNotALevel, value ?? string.Empty));
            return;
        }

        float level = value[0] is '+' or '-' ? skill.StoredLevel + amount : amount;

        if (!skill.SetLevel(level))
        {
            context.AddString(Names.Localize(Tokens.CmdNoCharacter));
            return;
        }

        Unlocks.Check();
        ShowStats(context, skill);
    }

    /// <summary>Unlocks an ability, or all, leaving the level as it is.</summary>
    private static void UnlockAbilities(Terminal context, MimirSkill skill, string value)
    {
        if (Pick(context, skill, value, out List<Ability> abilities))
        {
            Unlocks.Grant(skill, abilities);
            ShowStats(context, skill);
        }
    }

    /// <summary>Removes an ability's unlock, or every one, leaving the level as it is.</summary>
    private static void Reset(Terminal context, MimirSkill skill, string value)
    {
        if (!Pick(context, skill, value, out List<Ability> abilities))
        {
            return;
        }

        foreach (Ability ability in abilities)
        {
            skill.Lock(ability.Id);
        }

        ShowStats(context, skill);
    }

    /// <summary>Sets or resets the fortified level of a skill, or of every skill for "all", leaving the skills as they are.</summary>
    private static void Fortify(Terminal context, string name, string value)
    {
        Player player = Player.m_localPlayer;

        if (!player)
        {
            context.AddString(Names.Localize(Tokens.CmdNoCharacter));
            return;
        }

        bool reset = value == "reset";
        float level = 0f;

        if (!reset && (value == null || !float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out level)))
        {
            context.AddString(Names.Localize(Tokens.CmdNotALevel, value ?? string.Empty));
            return;
        }

        if (string.Equals(name, "all", StringComparison.OrdinalIgnoreCase))
        {
            int changed = 0;

            foreach (Skills.SkillType each in player.GetSkills().m_skillData.Keys.ToList())
            {
                if (FortifyOne(each, reset, level))
                {
                    changed++;
                }
            }

            context.AddString(Names.Localize(Tokens.CmdFortifiedCount, changed.ToString()));
            return;
        }

        if (!FindSkill(player, name, out Skills.SkillType type))
        {
            context.AddString(Names.Localize(Tokens.CmdUnknownSkill, name ?? string.Empty));
            return;
        }

        string shown = SkillName(type);

        if (!FortifyOne(type, reset, level))
        {
            context.AddString(Names.Localize(Tokens.CmdNotFortifying, shown));
            return;
        }

        context.AddString($"{shown}: {Names.Localize(Tokens.CmdFortified)} {Number(Fortifying.LevelOf(type), "0")}");
    }

    private static bool FortifyOne(Skills.SkillType type, bool reset, float level)
    {
        if (!reset)
        {
            return Fortifying.Set(type, level);
        }

        if (!Fortifying.Enabled(type))
        {
            return false;
        }

        Fortifying.Reset(type);
        return true;
    }

    /// <summary>A skill the character has, by Mimir name, the game's skill name or the name shown in game.</summary>
    private static bool FindSkill(Player player, string name, out Skills.SkillType type)
    {
        type = Registry.Find(name)?.Type ?? Skills.SkillType.None;

        if (type != Skills.SkillType.None || string.IsNullOrEmpty(name))
        {
            return type != Skills.SkillType.None;
        }

        foreach (Skills.SkillType each in player.GetSkills().m_skillData.Keys)
        {
            if (string.Equals(each.ToString(), name, StringComparison.OrdinalIgnoreCase)
                || string.Equals(SkillName(each), name, StringComparison.OrdinalIgnoreCase))
            {
                type = each;
                return true;
            }
        }

        return false;
    }

    private static string SkillName(Skills.SkillType type)
    {
        return Names.Localize("$skill_" + type.ToString().ToLowerInvariant());
    }

    /// <summary>The ability named, or every one for "all"; false, with a message, for an unknown ability or no character.</summary>
    private static bool Pick(Terminal context, MimirSkill skill, string value, out List<Ability> abilities)
    {
        abilities = new List<Ability>();

        if (!Player.m_localPlayer)
        {
            context.AddString(Names.Localize(Tokens.CmdNoCharacter));
            return false;
        }

        if (value == "all")
        {
            abilities.AddRange(skill.Abilities);
            return true;
        }

        Ability ability = value == null ? null : skill.GetAbility(value);

        if (ability != null)
        {
            abilities.Add(ability);
            return true;
        }

        IEnumerable<string> ids = skill.Abilities.Select(entry => entry.Key);
        context.AddString(Names.Localize(Tokens.CmdUnknownUnlock, value ?? string.Empty));
        context.AddString("  " + Names.Localize(Tokens.CmdUnlocks, string.Join(", ", ids)));
        return false;
    }

    private static string SkillList()
    {
        return string.Join(", ", Registry.Skills.Select(skill => skill.Key));
    }

    /// <summary>Formats a number in the player's culture.</summary>
    private static string Number(float value, string format)
    {
        return value.ToString(format, CultureInfo.CurrentCulture);
    }
}
