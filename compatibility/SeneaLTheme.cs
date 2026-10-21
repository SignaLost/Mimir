using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Mimir.Common;

namespace Mimir.Compatibility;

/// <summary>Reads colors from SeneaL API's theme; stands down when <c>Themes.TryColor</c> is not found as expected.</summary>
internal static class SeneaLTheme
{
    internal const string Muted = "Muted";
    internal const string Accent = "Accent";
    internal const string Good = "Good";
    internal const string Bad = "Bad";

    private static bool _resolved;
    private static Type _colors;
    private static MethodInfo _tryColor;
    private static readonly object[] Args = new object[2];

    /// <summary>The theme's color as a "#RRGGBB" rich text value; the fallback without a theme.</summary>
    internal static string Hex(string name, string fallback)
    {
        Resolve();

        if (_tryColor == null)
        {
            return fallback;
        }

        try
        {
            Args[0] = Enum.Parse(_colors, name);
            Args[1] = null;
            return (bool)_tryColor.Invoke(null, Args) ? "#" + ColorUtility.ToHtmlStringRGB((Color)Args[1]) : fallback;
        }
        catch (Exception)
        {
            return fallback;
        }
    }

    private static void Resolve()
    {
        if (_resolved)
        {
            return;
        }

        _resolved = true;
        Type themes = AccessTools.TypeByName("SeneaL.API.Themes");
        Type colors = AccessTools.TypeByName("SeneaL.API.ThemeColor");

        if (themes == null || colors == null)
        {
            return;
        }

        MethodInfo tryColor = AccessTools.Method(themes, "TryColor", new[] { colors, typeof(Color).MakeByRefType() });

        if (tryColor == null || tryColor.ReturnType != typeof(bool) || !colors.IsEnum || !Enum.IsDefined(colors, Good))
        {
            Log.LogWarning("SeneaL API's theme has another shape than expected; the Skill Abilities page keeps its own colors.");
            return;
        }

        _colors = colors;
        _tryColor = tryColor;
    }
}
