using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;
using Mimir.Common;

namespace Mimir.Modules;

/// <summary>How skill unlocks show.</summary>
internal enum UnlockNotice
{
    Announce,
    Popup,
    Notify,
    Off
}

/// <summary>
///     Shows ability unlocks by the <c>Unlock Notices</c> setting: the game's unlock popup, a mid-screen announcement, a
///     corner message, or nothing. Mid-screen announcements queue four seconds apart, the game's center message duration;
///     several unlocks earned at once share one announcement with each listed in the corner.
/// </summary>
internal static class UnlockNotices
{
    private const float AnnounceSpacing = 4f;

    internal static ConfigEntry<UnlockNotice> Setting;

    private static readonly Queue<(string Center, List<string> Corner)> _announcing = new();
    private static float _nextAnnounce;

    /// <summary>Shows or queues the notices for abilities unlocked together.</summary>
    internal static void Announce(List<Ability> earned)
    {
        UnlockNotice notice = Setting?.Value ?? UnlockNotice.Popup;

        if (notice == UnlockNotice.Off)
        {
            return;
        }

        if (notice == UnlockNotice.Popup)
        {
            foreach (Ability ability in earned)
            {
                Popup(ability);
            }

            return;
        }

        List<string> lines = new();

        foreach (Ability ability in earned)
        {
            lines.Add(UnlockLine(ability));
        }

        if (notice == UnlockNotice.Notify)
        {
            foreach (string line in lines)
            {
                Show(MessageHud.MessageType.TopLeft, line);
            }
        }
        else if (lines.Count == 1)
        {
            _announcing.Enqueue((lines[0], null));
        }
        else
        {
            _announcing.Enqueue((Names.Localize(Tokens.NewAbilities), lines));
        }
    }

    /// <summary>Shows the next queued announcement once the spacing has passed.</summary>
    internal static void Update()
    {
        if (_announcing.Count == 0 || Time.unscaledTime < _nextAnnounce)
        {
            return;
        }

        (string center, List<string> corner) = _announcing.Dequeue();
        _nextAnnounce = Time.unscaledTime + AnnounceSpacing;

        Show(MessageHud.MessageType.Center, center);

        if (corner == null)
        {
            return;
        }

        foreach (string line in corner)
        {
            Show(MessageHud.MessageType.TopLeft, line);
        }
    }

    internal static void Clear()
    {
        _announcing.Clear();
    }

    /// <summary>The ability's own unlock line if it has one, else Mimir's default.</summary>
    private static string UnlockLine(Ability ability)
    {
        return Names.Localize(ability.UnlockText ?? Tokens.Unlocked, Names.Localize(ability.Name));
    }

    /// <summary>Queues the game's unlock popup: the skill's icon, the ability's name, and its description or unlock line.</summary>
    private static void Popup(Ability ability)
    {
        Player player = Player.m_localPlayer;

        if (!player || !MessageHud.instance)
        {
            return;
        }

        Sprite icon = player.GetSkills().GetSkillDef(ability.Skill.Type)?.m_icon;
        string description = ability.Description != null ? Names.Localize(ability.Description) : UnlockLine(ability);
        MessageHud.instance.QueueUnlockMsg(icon, Names.Localize(ability.Name), description);
    }

    /// <summary>Shows a message through Player.Message, which keeps it out of the message log.</summary>
    private static void Show(MessageHud.MessageType type, string text)
    {
        if (Player.m_localPlayer)
        {
            Player.m_localPlayer.Message(type, text);
        }
    }
}
