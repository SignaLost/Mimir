using System;
using System.Reflection;
using HarmonyLib;
using TMPro;
using Mimir.Common;

namespace Mimir.Compatibility;

/// <summary>Reads and scrolls SeneaL UI's copy of the compendium text; stands down when its <c>TextsView</c> is not found as expected.</summary>
internal static class SeneaLTexts
{
    private const string ViewHolder = "SeneaLUI.Patches.InventoryGuiPatches";

    private static bool _resolved;
    private static FieldInfo _view;
    private static FieldInfo _text;
    private static FieldInfo _body;
    private static FieldInfo _count;
    private static MethodInfo _applied;
    private static MethodInfo _offset;
    private static MethodInfo _restore;

    /// <summary>The text SeneaL UI shows the compendium page in, while it has taken the compendium over; otherwise null.</summary>
    internal static TMP_Text Text
    {
        get
        {
            object view = View();
            return view != null ? _text.GetValue(view) as TMP_Text : null;
        }
    }

    /// <summary>
    ///     Makes its page list read the page names again. It only does so when the number of pages changes, so a page
    ///     swapped for another keeps the old name, such as Impactful Skills' Skill Perks page in place of Mimir's.
    /// </summary>
    internal static void Relabel()
    {
        Resolve();
        object view = _view != null && _count != null ? _view.GetValue(null) : null;

        if (view != null)
        {
            _count.SetValue(view, -1);
        }
    }

    /// <summary>How far its page is scrolled; 0 without SeneaL UI.</summary>
    internal static float Offset()
    {
        object body = Body();
        return body != null ? (float)_offset.Invoke(body, null) : 0f;
    }

    /// <summary>Scrolls its page to an offset.</summary>
    internal static void Restore(float offset)
    {
        object body = Body();

        if (body != null)
        {
            _restore.Invoke(body, new object[] { offset });
        }
    }

    private static object Body()
    {
        object view = View();
        return view != null ? _body.GetValue(view) : null;
    }

    private static object View()
    {
        Resolve();

        if (_view == null)
        {
            return null;
        }

        object view = _view.GetValue(null);
        return view != null && (bool)_applied.Invoke(view, null) ? view : null;
    }

    private static void Resolve()
    {
        if (_resolved)
        {
            return;
        }

        _resolved = true;
        Type holder = AccessTools.TypeByName(ViewHolder);
        FieldInfo view = holder != null ? AccessTools.Field(holder, "Texts") : null;

        if (view == null)
        {
            return;
        }

        Type viewType = view.FieldType;
        _text = AccessTools.Field(viewType, "_text");
        _body = AccessTools.Field(viewType, "_body");
        FieldInfo count = AccessTools.Field(viewType, "_count");
        _count = count != null && count.FieldType == typeof(int) ? count : null;
        _applied = AccessTools.PropertyGetter(viewType, "Applied");
        _offset = _body != null ? AccessTools.PropertyGetter(_body.FieldType, "Offset") : null;
        _restore = _body != null ? AccessTools.Method(_body.FieldType, "Restore", new[] { typeof(float) }) : null;

        if (_text == null || !typeof(TMP_Text).IsAssignableFrom(_text.FieldType) || _applied == null || _applied.ReturnType != typeof(bool)
            || _offset == null || _offset.ReturnType != typeof(float) || _restore == null)
        {
            Log.LogWarning("SeneaL UI's compendium has another shape than expected; Skill Abilities links use the game's text.");
            return;
        }

        _view = view;
    }
}
