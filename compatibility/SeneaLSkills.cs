using System;
using System.Collections;
using System.Collections.Generic;
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

    /// <summary>What is read and made for one of its rows, kept so the reads happen once rather than every tick.</summary>
    private sealed class RowParts
    {
        internal RectTransform Root;
        internal RectTransform Track;
        internal TMP_Text Style;
        internal Skills.SkillType Type;
        internal RectTransform Bar;
        internal RectTransform Fill;
        internal TMP_Text Number;
        internal float LastWidth = -1f;
    }

    private static readonly Dictionary<object, RowParts> _parts = new();
    private static bool _failed;

    /// <summary>Puts the fortified bar and level under each row's progress bar; after a failure it does nothing more.</summary>
    internal static void Apply(object view)
    {
        if (_failed || _rows == null || !(bool)_applied.GetValue(view) || _rows.GetValue(view) is not IList rows)
        {
            return;
        }

        try
        {
            foreach (object row in rows)
            {
                RowParts parts = PartsOf(row);

                if (parts != null && parts.Root.gameObject.activeSelf)
                {
                    Draw(parts);
                }
            }
        }
        catch (Exception e)
        {
            _failed = true;
            Log.LogWarning($"Fortified levels in SeneaL UI's skills panel failed and are off until the game restarts: {e.Message}");
        }
    }

    /// <summary>The row's parts, read once; read again when SeneaL UI has rebuilt the row's objects.</summary>
    private static RowParts PartsOf(object row)
    {
        if (_parts.TryGetValue(row, out RowParts parts) && parts.Root && parts.Track)
        {
            return parts;
        }

        if (_root.GetValue(row) is not RectTransform root || _skill.GetValue(row) is not Skills.Skill skill
            || _fill.GetValue(row) is not RectTransform fill || fill.parent is not RectTransform track)
        {
            _parts.Remove(row);
            return null;
        }

        parts = new RowParts { Root = root, Track = track, Style = _next.GetValue(row) as TMP_Text, Type = skill.m_info.m_skill };
        _parts[row] = parts;
        return parts;
    }

    private static void Draw(RowParts parts)
    {
        bool shows = FortifiedDisplay.Shows(parts.Type);

        if (!parts.Bar)
        {
            if (!shows)
            {
                return;
            }

            Build(parts);
        }

        if (parts.Bar.gameObject.activeSelf != shows)
        {
            parts.Bar.gameObject.SetActive(shows);
            parts.Number.gameObject.SetActive(shows);
        }

        if (!shows)
        {
            return;
        }

        // SeneaL UI insets the fill one unit inside its track.
        float width = (parts.Bar.sizeDelta.x - 2f) * Fortifying.NextFraction(parts.Type);

        if (parts.Fill && !Mathf.Approximately(width, parts.LastWidth))
        {
            parts.LastWidth = width;
            parts.Fill.sizeDelta = new Vector2(width, BarHeight - 2f);
        }

        FortifiedDisplay.SetNumber(parts.Number, parts.Type);
    }

    /// <summary>
    ///     Copies the row's progress bar, thinner and with a blue fill, a little below it, and puts the fortified level at
    ///     its right end, right-aligned with the progress text. SeneaL UI anchors its row parts by their top left.
    /// </summary>
    private static void Build(RowParts parts)
    {
        RectTransform track = parts.Track;
        TMP_Text style = parts.Style;
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

        // A copy of the progress text keeps its font; a new text would look for TMP's default font, which the game lacks.
        GameObject made = style
            ? UnityEngine.Object.Instantiate(style.gameObject, track.parent)
            : new GameObject(FortifiedDisplay.NumberName, typeof(RectTransform), typeof(TextMeshProUGUI));
        made.name = FortifiedDisplay.NumberName;
        made.layer = track.gameObject.layer;
        RectTransform rect = (RectTransform)made.transform;
        rect.SetParent(track.parent, false);
        rect.anchorMin = track.anchorMin;
        rect.anchorMax = track.anchorMax;
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = new Vector2(track.anchoredPosition.x + track.sizeDelta.x, top - BarHeight * 0.5f);
        rect.sizeDelta = new Vector2(NumberWidth, 16f);

        TMP_Text number = made.GetComponent<TMP_Text>();
        number.text = string.Empty;
        number.color = FortifiedDisplay.Blue;
        number.alignment = TextAlignmentOptions.MidlineRight;
        number.textWrappingMode = TextWrappingModes.NoWrap;
        number.overflowMode = TextOverflowModes.Overflow;
        number.raycastTarget = false;

        parts.Bar = bar;
        parts.Fill = (RectTransform)fill;
        parts.Number = number;
        parts.LastWidth = -1f;
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
