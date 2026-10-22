using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using BepInEx.Configuration;
using UnityEngine;
using Mimir.Common;
using Mimir.Compatibility;

namespace Mimir.Modules;

/// <summary>
///     Builds the Skill Abilities page of the compendium, shown when the <c>Skill Abilities Page</c> setting is on. Each
///     skill is listed by localized name with its level, perks and a collapsible list of its abilities with their unlock
///     levels. A skill gathers Mimir's abilities, bonuses and perks, the game's own effects (<see cref="SkillEffects" />)
///     and Impactful Skills' perks (<see cref="ImpactfulSkills" />). Colors come from the SeneaL API theme when present;
///     <see cref="PageLinks" /> handles the expand and collapse links.
/// </summary>
internal static class AbilityPage
{
    internal const string LinkPrefix = "mimir:";

    // After the game's Active Effects and Logs pages.
    private const int PageIndex = 2;

    private static string _skillColor;
    private static string _headerColor;
    private static string _perkColor;
    private static string _unlockedColor;
    private static string _lockedColor;
    private static string _mutedColor;

    internal static ConfigEntry<bool> Show;
    internal static ConfigEntry<bool> HideLocked;

    private static readonly HashSet<int> Expanded = new();
    private static readonly HashSet<Skills.SkillType> Types = new();
    private static readonly List<(Skills.SkillType Type, string Name)> Sorted = new();
    private static readonly List<Entry> Shown = new();
    private static readonly PerkList Perks = new();
    private static readonly StringBuilder Text = new(2048);

    private static readonly Action<string, string, float, bool> AddImpactful =
        (name, description, level, unlocked) => Shown.Add(new Entry(Names.Localize(name), Names.Localize(description), level, unlocked, Shown.Count));

    private static TextsDialog _dialog;
    private static TextsDialog.TextInfo _page;

    internal static bool Showing => _page != null && _page.m_selected && _page.m_selected.activeSelf;

    /// <summary>An ability as listed: localized name and description, unlock level and whether it is unlocked.</summary>
    private readonly struct Entry
    {
        internal readonly string Name;
        internal readonly string Description;
        internal readonly float Level;
        internal readonly bool Unlocked;
        internal readonly int Order;

        internal Entry(string name, string description, float level, bool unlocked, int order)
        {
            Name = name;
            Description = description;
            Level = level;
            Unlocked = unlocked;
            Order = order;
        }
    }

    /// <summary>Adds the page to the dialog's list when any skill has something to show.</summary>
    internal static void Add(TextsDialog dialog)
    {
        _page = null;

        if (!Show.Value || !Player.m_localPlayer)
        {
            return;
        }

        ImpactfulSkills.RemovePage(dialog);
        string text = Build();

        if (text.Length == 0)
        {
            return;
        }

        _dialog = dialog;
        _page = new TextsDialog.TextInfo(Names.Localize(Tokens.PageTitle), text);
        dialog.m_texts.Insert(Mathf.Min(PageIndex, dialog.m_texts.Count), _page);
        PageLinks.Attach(dialog.m_textArea);
    }

    /// <summary>Expands or collapses a skill's abilities and redraws the page; returns the new page, or null when it is not showing.</summary>
    internal static string Toggle(string skill)
    {
        if (!Showing || !int.TryParse(skill, NumberStyles.Integer, CultureInfo.InvariantCulture, out int type))
        {
            return null;
        }

        if (!Expanded.Remove(type))
        {
            Expanded.Add(type);
        }

        _page.m_text = Build();
        _dialog.m_textArea.text = _page.m_text;
        return _page.m_text;
    }

    private static string Build()
    {
        _skillColor = SeneaLTheme.Hex(SeneaLTheme.Accent, "orange");
        _headerColor = SeneaLTheme.Hex(SeneaLTheme.Accent, "#E8B04A");
        _perkColor = SeneaLTheme.Hex(SeneaLTheme.Good, "#9ACD5A");
        _unlockedColor = SeneaLTheme.Hex(SeneaLTheme.Accent, "yellow");
        _lockedColor = SeneaLTheme.Hex(SeneaLTheme.Bad, "#E06A5A");
        _mutedColor = SeneaLTheme.Hex(SeneaLTheme.Muted, "#8C8C8C");
        Text.Clear();

        Skills skills = Player.m_localPlayer.GetSkills();
        IDictionary impactful = ImpactfulSkills.Table;
        Types.Clear();
        Sorted.Clear();

        foreach (MimirSkill skill in MimirAPI.All)
        {
            Types.Add(skill.Type);
        }

        if (impactful != null)
        {
            foreach (object type in impactful.Keys)
            {
                Types.Add((Skills.SkillType)type);
            }
        }

        if (SkillEffects.Enabled)
        {
            foreach (Skills.SkillType type in SkillEffects.Types)
            {
                // The game's own effects show only for skills the character has.
                if (skills.m_skillData.ContainsKey(type))
                {
                    Types.Add(type);
                }
            }
        }

        foreach (Skills.SkillType type in Types)
        {
            if (type != Skills.SkillType.None)
            {
                Sorted.Add((type, Names.Localize(MimirAPI.Find(type)?.Name ?? "$skill_" + type.ToString().ToLower())));
            }
        }

        Sorted.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));

        // Links cannot be clicked with a gamepad, so every list shows.
        bool expandAll = ZInput.IsGamepadActive();

        foreach ((Skills.SkillType type, string name) in Sorted)
        {
            AppendSkill(skills, impactful, type, name, expandAll || Expanded.Contains((int)type));
        }

        return Text.ToString();
    }

    private static void AppendSkill(Skills skills, IDictionary impactful, Skills.SkillType type, string name, bool expanded)
    {
        Shown.Clear();
        Perks.Clear();

        MimirSkill skill = MimirAPI.Find(type);
        bool known = skills.m_skillData.ContainsKey(type);

        // Reading a level the character lacks would add the skill to it.
        float level = skill?.Level ?? (known ? skills.GetSkillLevel(type) : 0f);
        float factor = skill?.Factor ?? (known ? skills.GetSkillFactor(type) : 0f);

        if (skill != null)
        {
            foreach (Ability ability in skill.Abilities)
            {
                if (!skill.IsDisabled(ability.Id))
                {
                    string description = ability.Description != null ? Names.Localize(ability.Description) : null;
                    Shown.Add(new Entry(Names.Localize(ability.Name), description, ability.Level, skill.IsUnlocked(ability.Id), Shown.Count));
                }
            }

            foreach (Bonus bonus in skill.Bonuses)
            {
                Perks.Add(BonusName(bonus), bonus.Current, bonus.Type == BonusType.Percent ? "%" : string.Empty);
            }

            foreach (Perk perk in skill.Perks)
            {
                Perks.Add(Names.Localize(perk.Name), perk.Current, perk.Unit);
            }
        }

        if (SkillEffects.Enabled && known)
        {
            SkillEffects.Collect(type, skills, Player.m_localPlayer, Perks);
        }

        if (impactful != null && !ImpactfulSkills.Hidden(type) && impactful[type] is IList perks)
        {
            ImpactfulSkills.Collect(perks, level, factor, AddImpactful);
        }

        if (Shown.Count == 0 && Perks.Count == 0)
        {
            return;
        }

        // Stable, so abilities at the same level keep the order they were added in.
        Shown.Sort((a, b) => a.Level != b.Level ? a.Level.CompareTo(b.Level) : a.Order.CompareTo(b.Order));

        Text.Append("<color=").Append(_skillColor).Append('>').Append(name).Append("</color>: ")
            .Append(Names.Localize(Tokens.PageLevel, Mathf.FloorToInt(level).ToString())).Append('\n');

        if (Perks.Count > 0)
        {
            Header(Tokens.PagePerks).Append(' ');

            for (int i = 0; i < Perks.Count; i++)
            {
                Text.Append(i > 0 ? ", " : string.Empty).Append("<color=").Append(_perkColor).Append('>').Append(Perks[i]).Append("</color>");
            }

            Text.Append('\n');
        }

        if (Shown.Count > 0)
        {
            AppendAbilities(type, expanded);
        }

        Text.Append('\n');
    }

    private static void AppendAbilities(Skills.SkillType type, bool expanded)
    {
        int unlocked = 0;

        foreach (Entry ability in Shown)
        {
            if (ability.Unlocked)
            {
                unlocked++;
            }
        }

        Text.Append("<link=\"").Append(LinkPrefix).Append((int)type).Append("\">");
        Header(Tokens.PageAbilities).Append(' ').Append(unlocked).Append('/').Append(Shown.Count).Append(expanded ? " [-]" : " [+]").Append("</link>\n");

        if (!expanded)
        {
            return;
        }

        foreach (Entry ability in Shown)
        {
            Text.Append("<indent=1em>");

            if (ability.Unlocked)
            {
                Text.Append("<color=").Append(_unlockedColor).Append('>').Append(ability.Name).Append("</color>");

                if (ability.Description != null)
                {
                    Text.Append(": ").Append(ability.Description);
                }
            }
            else
            {
                Text.Append("<color=").Append(_lockedColor).Append('>').Append(ability.Name).Append("</color><color=").Append(_mutedColor).Append("> (")
                    .Append(Names.Localize(Tokens.PageLevel, Mathf.CeilToInt(ability.Level).ToString())).Append(')');

                if (ability.Description != null && !HideLocked.Value)
                {
                    Text.Append(": ").Append(ability.Description);
                }

                Text.Append("</color>");
            }

            Text.Append("</indent>\n");
        }
    }

    /// <summary>Appends a header in the header color, followed by an uncolored colon.</summary>
    private static StringBuilder Header(string token)
    {
        return Text.Append("<color=").Append(_headerColor).Append('>').Append(Names.Localize(token)).Append("</color>:");
    }

    /// <summary>A bonus's shown name: its own, or the stat's with the weapon type it is limited to.</summary>
    private static string BonusName(Bonus bonus)
    {
        if (bonus.Name != null)
        {
            return Names.Localize(bonus.Name);
        }

        string name = Names.Localize(Tokens.StatPrefix + bonus.Stat.ToString().ToLowerInvariant());
        return bonus.Weapon == Skills.SkillType.None ? name : name + " (" + Names.Localize("$skill_" + bonus.Weapon.ToString().ToLowerInvariant()) + ")";
    }
}

/// <summary>The perk lines of one skill on the Skill Abilities page.</summary>
internal sealed class PerkList : List<string>
{
    /// <summary>Adds a line of name, signed amount and unit, unless the amount shows as 0.</summary>
    internal void Add(string name, float amount, string unit)
    {
        string value = amount.ToString("+0.##;-0.##;0", CultureInfo.CurrentCulture);

        if (value != "0")
        {
            Add(name + " " + value + unit);
        }
    }

    /// <summary>Adds a line of name and an unsigned range, such as "Damage 85-100%".</summary>
    internal void AddSpan(string name, float min, float max, string unit)
    {
        Add(name + " " + min.ToString("0.##", CultureInfo.CurrentCulture) + "-" + max.ToString("0.##", CultureInfo.CurrentCulture) + unit);
    }
}
