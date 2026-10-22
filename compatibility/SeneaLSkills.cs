using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using Mimir.Common;
using Mimir.Modules;

namespace Mimir.Compatibility;

/// <summary>
///     Adds fortified bars and levels to SeneaL UI's skills panel, which replaces the game's rows with its own. Each
///     bar is a thinner copy of the row's own progress bar with its fill recolored blue, and the level at its end uses
///     the row's progress text style. Stands down when its <c>SkillsView</c> is not found as expected.
/// </summary>
internal static class SeneaLSkills
{
    private const string ViewType = "SeneaLUI.Inv.SkillsView";
    private const string FillName = "f";
    private const float BarHeight = 6f;
    private const float FillRadius = 2f;
    private const float Gap = 4.5f;
    private const float NumberWidth = 20f;
    private const float NumberGap = 4f;

    private static bool _resolved;
    private static FieldInfo _rows;
    private static FieldInfo _applied;
    private static FieldInfo _root;
    private static FieldInfo _fill;
    private static FieldInfo _skill;
    private static FieldInfo _next;
    private static Type _roundedFill;
    private static MethodInfo _setFill;

    /// <summary>SeneaL UI's <c>SkillsView.Tick</c>, or null when it is not installed or has another shape.</summary>
    internal static MethodBase TickMethod()
    {
        Type view = AccessTools.TypeByName(ViewType);
        return view != null && Resolve(view) ? AccessTools.Method(view, "Tick", new[] { typeof(Player) }) : null;
    }

    /// <summary>Puts the fortified bar and level under each row's progress bar.</summary>
    internal static void Apply(object view)
    {
        if (_rows == null || !(bool)_applied.GetValue(view) || _rows.GetValue(view) is not IList rows)
        {
            return;
        }

        foreach (object row in rows)
        {
            if (_root.GetValue(row) is not RectTransform root || !root.gameObject.activeSelf || _skill.GetValue(row) is not Skills.Skill skill
                || _fill.GetValue(row) is not RectTransform fill || fill.parent is not RectTransform track)
            {
                continue;
            }

            Draw(root, track, _next.GetValue(row) as TMP_Text, skill.m_info.m_skill);
        }
    }

    private static void Draw(RectTransform root, RectTransform track, TMP_Text style, Skills.SkillType type)
    {
        bool shows = FortifiedDisplay.Shows(type);
        RectTransform bar = root.Find(FortifiedDisplay.BarName) as RectTransform;

        if (!bar)
        {
            if (!shows)
            {
                return;
            }

            bar = Build(track, style);
        }

        Transform number = root.Find(FortifiedDisplay.NumberName);
        bar.gameObject.SetActive(shows);
        number.gameObject.SetActive(shows);

        if (!shows)
        {
            return;
        }

        // SeneaL UI insets the fill one unit inside its track.
        RectTransform fill = (RectTransform)bar.Find(FillName);
        fill.sizeDelta = new Vector2((bar.sizeDelta.x - 2f) * Fortifying.NextFraction(type), BarHeight - 2f);
        FortifiedDisplay.SetNumber(number.GetComponent<TMP_Text>(), type);
    }

    /// <summary>
    ///     Copies the row's progress bar, thinner and with a blue fill, a little below it, and puts the fortified level at
    ///     its right end, right-aligned with the progress text. SeneaL UI anchors its row parts by their top left.
    /// </summary>
    private static RectTransform Build(RectTransform track, TMP_Text style)
    {
        float top = track.anchoredPosition.y - track.sizeDelta.y - Gap;

        RectTransform bar = (RectTransform)UnityEngine.Object.Instantiate(track.gameObject, track.parent).transform;
        bar.name = FortifiedDisplay.BarName;
        bar.anchoredPosition = new Vector2(track.anchoredPosition.x, top);
        bar.sizeDelta = new Vector2(track.sizeDelta.x - NumberWidth - NumberGap, BarHeight);

        Transform fill = bar.Find(FillName);
        Component rounded = fill ? fill.GetComponent(_roundedFill) : null;

        if (rounded)
        {
            _setFill.Invoke(rounded, new object[] { FillRadius, FortifiedDisplay.DeepBlue, FortifiedDisplay.Blue });
        }

        GameObject made = new(FortifiedDisplay.NumberName, typeof(RectTransform));
        made.layer = track.gameObject.layer;
        RectTransform rect = (RectTransform)made.transform;
        rect.SetParent(track.parent, false);
        rect.anchorMin = track.anchorMin;
        rect.anchorMax = track.anchorMax;
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = new Vector2(track.anchoredPosition.x + track.sizeDelta.x, top - BarHeight * 0.5f);
        rect.sizeDelta = new Vector2(NumberWidth, 16f);

        TextMeshProUGUI number = made.AddComponent<TextMeshProUGUI>();

        if (style)
        {
            number.font = style.font;
            number.fontSize = style.fontSize;
            number.fontStyle = style.fontStyle;
        }

        number.color = FortifiedDisplay.Blue;
        number.alignment = TextAlignmentOptions.MidlineRight;
        number.textWrappingMode = TextWrappingModes.NoWrap;
        number.overflowMode = TextOverflowModes.Overflow;
        number.raycastTarget = false;
        return bar;
    }

    private static bool Resolve(Type view)
    {
        if (_resolved)
        {
            return _rows != null;
        }

        _resolved = true;
        FieldInfo rows = AccessTools.Field(view, "_rows");
        FieldInfo applied = AccessTools.Field(view, "_applied");
        Type row = rows?.FieldType.IsGenericType == true ? rows.FieldType.GetGenericArguments()[0] : null;

        if (row == null || applied?.FieldType != typeof(bool))
        {
            Warn();
            return false;
        }

        _root = AccessTools.Field(row, "Root");
        _fill = AccessTools.Field(row, "Fill");
        _skill = AccessTools.Field(row, "Skill");
        _next = AccessTools.Field(row, "Next");
        _roundedFill = AccessTools.TypeByName("SeneaLUI.Theme.RoundedFill");
        _setFill = _roundedFill != null ? AccessTools.Method(_roundedFill, "Set", new[] { typeof(float), typeof(Color), typeof(Color) }) : null;

        if (_root == null || _next == null || _fill == null || _skill == null || _skill.FieldType != typeof(Skills.Skill) || _setFill == null)
        {
            Warn();
            return false;
        }

        _rows = rows;
        _applied = applied;
        return true;
    }

    private static void Warn()
    {
        Log.LogWarning("SeneaL UI's skills panel has another shape than expected; it shows no fortified levels.");
    }
}
