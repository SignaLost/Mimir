using System;
using System.Collections;
using System.Reflection;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Managers;
using Mimir.Common;

namespace Mimir.Compatibility;

/// <summary>
///     Lists Impactful Skills' perks as abilities on Mimir's Skill Abilities page and removes its own Skill Perks page,
///     when <c>Absorb Impactful Skills</c> is on. The perks are read from its <c>SkillPerks.BySkill</c> table, which
///     already holds each perk's toggle, unlock level and check, so they show and unlock exactly as on its own page.
///     If that table changes shape, Mimir warns once and leaves its page alone.
/// </summary>
internal static class ImpactfulSkills
{
    internal const string Guid = "MidnightsFX.ImpactfulSkills";

    private const string PageToken = "$compendium_skill_perks";

    internal static ConfigEntry<bool> Absorb;

    private static bool _resolved;
    private static PropertyInfo _bySkill;
    private static Func<Skills.SkillType, bool> _isHidden;
    private static FieldInfo _name;
    private static FieldInfo _enabled;
    private static FieldInfo _level;
    private static FieldInfo _reached;

    /// <summary>Whether its perks are listed by Mimir.</summary>
    internal static bool Active => Absorb != null && Absorb.Value && Resolve();

    /// <summary>Its perk table, keyed by skill type; null when it is not in use.</summary>
    internal static IDictionary Table => Active ? _bySkill.GetValue(null) as IDictionary : null;

    /// <summary>Whether it hides the skill because its feature is turned off.</summary>
    internal static bool Hidden(Skills.SkillType type)
    {
        return _isHidden != null && _isHidden(type);
    }

    /// <summary>Adds the skill's enabled perks, as abilities, to the list.</summary>
    internal static void Collect(IList perks, float level, float factor, Action<string, string, float, bool> add)
    {
        foreach (object perk in perks)
        {
            if (_enabled.GetValue(perk) is Func<bool> enabled && !enabled())
            {
                continue;
            }

            string name = (string)_name.GetValue(perk);
            Func<float> unlockLevel = _level.GetValue(perk) as Func<float>;
            Func<float, float, bool> reached = _reached.GetValue(perk) as Func<float, float, bool>;
            add(name, name + "_description", unlockLevel?.Invoke() ?? 0f, reached == null || reached(level, factor));
        }
    }

    /// <summary>Removes its Skill Perks page from the compendium.</summary>
    internal static void RemovePage(TextsDialog dialog)
    {
        if (!Active)
        {
            return;
        }

        // Translated the way Impactful Skills names the page.
        string title = LocalizationManager.Instance.TryTranslate(PageToken);
        dialog.m_texts.RemoveAll(page => page.m_topic == title);
    }

    private static bool Resolve()
    {
        if (_resolved)
        {
            return _bySkill != null;
        }

        _resolved = true;

        if (!Chainloader.PluginInfos.ContainsKey(Guid))
        {
            return false;
        }

        Type skillPerks = AccessTools.TypeByName("ImpactfulSkills.patches.SkillPerks");
        Type perk = skillPerks != null ? AccessTools.Inner(skillPerks, "Perk") : null;
        Type disabled = AccessTools.TypeByName("ImpactfulSkills.patches.DisabledSkills");
        PropertyInfo bySkill = skillPerks != null ? AccessTools.Property(skillPerks, "BySkill") : null;
        MethodInfo isHidden = disabled != null ? AccessTools.Method(disabled, "IsHidden", new[] { typeof(Skills.SkillType) }) : null;

        if (perk == null || bySkill == null || !typeof(IDictionary).IsAssignableFrom(bySkill.PropertyType))
        {
            Log.LogWarning("Impactful Skills' perk list has changed; its Skill Perks page is left as it is.");
            return false;
        }

        _name = AccessTools.Field(perk, "Name");
        _enabled = AccessTools.Field(perk, "Enabled");
        _level = AccessTools.Field(perk, "Level");
        _reached = AccessTools.Field(perk, "Reached");

        if (_name == null || _enabled == null || _level == null || _reached == null)
        {
            Log.LogWarning("Impactful Skills' perks have changed; its Skill Perks page is left as it is.");
            return false;
        }

        if (isHidden != null && isHidden.ReturnType == typeof(bool))
        {
            _isHidden = AccessTools.MethodDelegate<Func<Skills.SkillType, bool>>(isHidden);
        }

        _bySkill = bySkill;
        return true;
    }
}
