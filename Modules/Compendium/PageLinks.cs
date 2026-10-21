using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Mimir.Common;
using Mimir.Compatibility;

namespace Mimir.Modules.Compendium;

/// <summary>Toggles a skill's ability list when its link on the Skill Abilities page is clicked.</summary>
internal sealed class PageLinks : MonoBehaviour
{
    private const int RestoreFrames = 10;

    private TMP_Text _text;
    private string _redrawn;
    private float _offset;
    private int _restoreFrames;

    /// <summary>Adds the handler to the compendium's text area once.</summary>
    internal static void Attach(TMP_Text text)
    {
        if (text && !text.GetComponent<PageLinks>())
        {
            text.gameObject.AddComponent<PageLinks>()._text = text;
        }
    }

    // Reads the mouse directly, since the game's text area takes no pointer events.
    private void Update()
    {
        if (_restoreFrames > 0)
        {
            RestoreScroll();
        }

        if (!ZInput.GetMouseButtonDown(0) || !AbilityPage.Showing)
        {
            return;
        }

        TMP_Text seneaL = SeneaLTexts.Text;
        string id = LinkUnder(seneaL ? seneaL : _text, ZInput.pointerPosition);

        if (id == null)
        {
            return;
        }

        float offset = seneaL ? SeneaLTexts.Offset() : 0f;
        string page = AbilityPage.Toggle(id);

        if (seneaL && page != null)
        {
            _redrawn = page;
            _offset = offset;
            _restoreFrames = RestoreFrames;
        }
    }

    /// <summary>Scrolls SeneaL UI's page back once it has redrawn the toggled page.</summary>
    private void RestoreScroll()
    {
        _restoreFrames--;
        TMP_Text seneaL = SeneaLTexts.Text;

        if (seneaL && seneaL.text == _redrawn)
        {
            SeneaLTexts.Restore(_offset);
            _restoreFrames = 0;
        }
    }

    /// <summary>The skill id of the link under the pointer, within the text's visible scroll area; null when none is.</summary>
    private static string LinkUnder(TMP_Text text, Vector2 pointer)
    {
        Canvas canvas = text.canvas ? text.canvas.rootCanvas : null;
        Camera camera = canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        ScrollRect scroll = text.GetComponentInParent<ScrollRect>();
        RectTransform view = scroll ? scroll.viewport ? scroll.viewport : (RectTransform)scroll.transform : null;

        if (view && !RectTransformUtility.RectangleContainsScreenPoint(view, pointer, camera))
        {
            return null;
        }

        int index = TMP_TextUtilities.FindIntersectingLink(text, pointer, camera);
        Log.LogInfo($"Skill Abilities click at {pointer}: link {index}", Log.InfoLevel.High);

        if (index < 0)
        {
            return null;
        }

        string id = text.textInfo.linkInfo[index].GetLinkID();
        return id.StartsWith(AbilityPage.LinkPrefix, StringComparison.Ordinal) ? id.Substring(AbilityPage.LinkPrefix.Length) : null;
    }
}
