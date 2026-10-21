using System;
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
///     skill is listed by localized name with its level, perks and bonuses, and a collapsible list of its enabled
///     abilities with their unlock levels. Colors come from the SeneaL API theme when present; <see cref="PageLinks" />
///     handles the expand and collapse links.
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

    private static readonly HashSet<string> Expanded = new(StringComparer.OrdinalIgnoreCase);
    private static readonly List<MimirSkill> Sorted = new();
    private static readonly List<Ability> Shown = new();
    private static readonly List<string> Perks = new();
    private static readonly StringBuilder Text = new(2048);

    private static TextsDialog _dialog;
    private static TextsDialog.TextInfo _page;

    internal static bool Showing => _page != null && _page.m_selected && _page.m_selected.activeSelf;

    /// <summary>Adds the page to the dialog's list when any skill has something to show.</summary>
    internal static void Add(TextsDialog dialog)
    {
        _page = null;

        if (!Show.Value || !Player.m_localPlayer)
        {
            return;
        }

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
    internal static string Toggle(string skillId)
    {
        if (!Showing)
        {
            return null;
        }

        if (!Expanded.Remove(skillId))
        {
            Expanded.Add(skillId);
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
        Sorted.Clear();
        Sorted.AddRange(MimirAPI.All);
        Sorted.Sort((a, b) => string.Compare(Names.Localize(a.Name), Names.Localize(b.Name), StringComparison.CurrentCultureIgnoreCase));

        // Links cannot be clicked with a gamepad, so every list shows.
        bool expandAll = ZInput.IsGamepadActive();

        foreach (MimirSkill skill in Sorted)
        {
            AppendSkill(skill, expandAll || Expanded.Contains(skill.Id));
        }

        return Text.ToString();
    }

    private static void AppendSkill(MimirSkill skill, bool expanded)
    {
        Shown.Clear();
        Perks.Clear();

        foreach (Ability ability in skill.Abilities)
        {
            if (!skill.IsDisabled(ability.Id))
            {
                Shown.Add(ability);
            }
        }

        foreach (Bonus bonus in skill.Bonuses)
        {
            AddPerk(BonusName(bonus), bonus.Current, bonus.Type == BonusType.Percent ? "%" : string.Empty);
        }

        foreach (Perk perk in skill.Perks)
        {
            AddPerk(Names.Localize(perk.Name), perk.Current, perk.Unit);
        }

        if (Shown.Count == 0 && Perks.Count == 0)
        {
            return;
        }

        Text.Append("<b><color=").Append(_skillColor).Append('>').Append(Names.Localize(skill.Name)).Append("</color></b>: ")
            .Append(Names.Localize(Tokens.PageLevel, Mathf.FloorToInt(skill.Level).ToString())).Append('\n');

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
            AppendAbilities(skill, expanded);
        }

        Text.Append('\n');
    }

    private static void AppendAbilities(MimirSkill skill, bool expanded)
    {
        int unlocked = 0;

        foreach (Ability ability in Shown)
        {
            if (skill.IsUnlocked(ability.Id))
            {
                unlocked++;
            }
        }

        Text.Append("<link=\"").Append(LinkPrefix).Append(skill.Id).Append("\">");
        Header(Tokens.PageAbilities).Append(' ').Append(unlocked).Append('/').Append(Shown.Count).Append(expanded ? " [-]" : " [+]").Append("</link>\n");

        if (!expanded)
        {
            return;
        }

        foreach (Ability ability in Shown)
        {
            string name = Names.Localize(ability.Name);
            string description = ability.Description != null ? Names.Localize(ability.Description) : null;
            Text.Append("<indent=1em>");

            if (skill.IsUnlocked(ability.Id))
            {
                Text.Append("<color=").Append(_unlockedColor).Append('>').Append(name).Append("</color>");

                if (description != null)
                {
                    Text.Append(": ").Append(description);
                }
            }
            else
            {
                Text.Append("<color=").Append(_lockedColor).Append('>').Append(name).Append("</color><color=").Append(_mutedColor).Append("> (")
                    .Append(Names.Localize(Tokens.PageLevel, Mathf.CeilToInt(ability.Level).ToString())).Append(')');

                if (description != null && !HideLocked.Value)
                {
                    Text.Append(": ").Append(description);
                }

                Text.Append("</color>");
            }

            Text.Append("</indent>\n");
        }
    }

    private static StringBuilder Header(string token)
    {
        return Text.Append("<color=").Append(_headerColor).Append('>').Append(Names.Localize(token)).Append("</color>");
    }

    /// <summary>Adds a perk line of name, signed amount and unit, unless the amount shows as 0.</summary>
    private static void AddPerk(string name, float amount, string unit)
    {
        string value = amount.ToString("+0.##;-0.##;0", CultureInfo.CurrentCulture);

        if (value != "0")
        {
            Perks.Add(name + " " + value + unit);
        }
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
