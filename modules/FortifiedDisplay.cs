using System.Collections.Generic;
using BepInEx.Configuration;
using TMPro;
using UnityEngine;

namespace Mimir.Modules;

/// <summary>
///     Shows each fortifying skill's fortified level in the skills panel. The game's panel shows it after the level as
///     "22 (18)" in blue, with a thinner blue copy of the progress bar under it filling toward the next fortified level.
///     SeneaL UI's panel is handled by <c>SeneaLSkills</c>. Turned off by <c>Show Fortified Levels</c>.
/// </summary>
internal static class FortifiedDisplay
{
    internal const string BarName = "mimir_fortified";
    internal const string NumberName = "mimir_fortified_level";

    private const string BlueTag = "#8FC5F0";

    internal static readonly Color Blue = new(0.561f, 0.773f, 0.941f, 1f);
    internal static readonly Color DeepBlue = new(0.33f, 0.55f, 0.78f, 1f);

    private static readonly string[] Numbers = new string[101];
    private static readonly Dictionary<(int Level, int Fortified), string> _levelTexts = new();
    private static readonly Vector3[] Corners = new Vector3[4];

    internal static ConfigEntry<bool> Show;

    /// <summary>Whether the skill's fortified level is shown.</summary>
    internal static bool Shows(Skills.SkillType type)
    {
        return Show != null && Show.Value && Fortifying.Enabled(type);
    }

    /// <summary>Shows the skill's fortified level, built once per level.</summary>
    internal static void SetNumber(TMP_Text number, Skills.SkillType type)
    {
        int fortified = Fortified(type);
        string text = Numbers[fortified] ??= fortified.ToString();

        if (!ReferenceEquals(number.text, text))
        {
            number.text = text;
        }
    }

    /// <summary>Adds the fortified level and bar to each row of the game's skills panel.</summary>
    internal static void ApplyVanilla(SkillsDialog dialog, Player player)
    {
        List<Skills.Skill> skills = player.GetSkills().GetSkillList();

        for (int i = 0; i < skills.Count && i < dialog.m_elements.Count; i++)
        {
            DrawVanilla(dialog.m_elements[i].transform, skills[i]);
        }
    }

    private static int Fortified(Skills.SkillType type)
    {
        return Mathf.Clamp(Mathf.FloorToInt(Fortifying.LevelOf(type)), 0, 100);
    }

    private static void DrawVanilla(Transform row, Skills.Skill skill)
    {
        Skills.SkillType type = skill.m_info.m_skill;
        bool shows = Shows(type);
        Transform levelText = Utils.FindChild(row, "leveltext");

        if (shows && levelText && levelText.GetComponent<TMP_Text>() is TMP_Text level)
        {
            (int, int) key = ((int)skill.m_level, Fortified(type));

            if (!_levelTexts.TryGetValue(key, out string text))
            {
                text = $"{key.Item1} <color={BlueTag}>({key.Item2})</color>";
                _levelTexts[key] = text;
            }

            level.text = text;
        }

        Transform bar = Utils.FindChild(row, BarName);

        if (!bar && shows)
        {
            bar = BuildVanillaBar(row);
        }

        if (!bar)
        {
            return;
        }

        bar.gameObject.SetActive(shows);

        if (shows)
        {
            bar.GetComponent<GuiBar>()?.SetValue(Fortifying.NextFraction(type));
        }
    }

    /// <summary>Copies the row's progress bar, thinner and blue, right against its bottom edge.</summary>
    private static Transform BuildVanillaBar(Transform row)
    {
        if (Utils.FindChild(row, "currentlevel") is not RectTransform source)
        {
            return null;
        }

        GameObject copy = Object.Instantiate(source.gameObject, source.parent);
        copy.name = BarName;
        RectTransform rect = (RectTransform)copy.transform;
        source.GetWorldCorners(Corners);
        float sourceBottom = Corners[0].y;
        float sourceHeight = Mathf.Max(1f, Corners[1].y - Corners[0].y);

        // 60% of the progress bar plus one screen pixel.
        float scale = (sourceHeight * 0.6f + 1f) / sourceHeight;
        rect.localScale = new Vector3(source.localScale.x, source.localScale.y * scale, source.localScale.z);
        rect.GetWorldCorners(Corners);
        rect.position += new Vector3(0f, sourceBottom - Corners[1].y, 0f);

        copy.GetComponent<GuiBar>()?.SetColor(Blue);
        return rect;
    }
}
