using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;
using Mimir.Common;

namespace Mimir.Modules.Leveling;

/// <summary>How skill unlocks show.</summary>
internal enum UnlockNotice
{
    Announce,
    Popup,
    Notify,
    Off
}

/// <summary>Watches each skill's level, raising level-ups and unlocking and announcing the abilities it covers.</summary>
internal static class Unlocks
{
    /// <summary>Seconds between queued center announcements, the game's center message duration.</summary>
    private const float AnnounceSpacing = 4f;

    private const float SweepInterval = 0.5f;

    internal static ConfigEntry<UnlockNotice> Setting;

    private static readonly Dictionary<MimirSkill, float> _known = new();
    private static readonly Queue<(string Center, List<string> Corner)> _announcing = new();
    private static readonly List<(MimirSkill Skill, Ability Ability)> _earned = new();
    private static readonly List<(MimirSkill Skill, int Level)> _leveled = new();
    private static Player _watched;
    private static float _nextAnnounce;
    private static float _nextSweep;
    private static bool _sweeping;

    /// <summary>Set when a level or an ability may have changed, so the next frame checks every skill.</summary>
    internal static bool Dirty = true;

    /// <summary>Called every frame.</summary>
    internal static void Update()
    {
        Player player = Player.m_localPlayer;

        if (!ReferenceEquals(player, _watched))
        {
            _watched = player;
            _known.Clear();
            _announcing.Clear();
            Dirty = true;

            foreach (MimirSkill skill in Registry.Skills)
            {
                skill.Forget();
            }

            Fortifying.Forget();
        }

        if (!player)
        {
            return;
        }

        // Other mods may set a level directly, so every skill is still swept now and then.
        if (!Dirty && Time.time < _nextSweep)
        {
            Pump();
            return;
        }

        Sweep();
        Pump();
    }

    /// <summary>Unlocks and raises level-ups now rather than next frame, such as before the console prints a skill.</summary>
    internal static void Check()
    {
        // An unlock or level-up handler may set a level; the running sweep's lists are still in use then.
        if (!_sweeping && Player.m_localPlayer && ReferenceEquals(Player.m_localPlayer, _watched))
        {
            Sweep();
        }
    }

    /// <summary>Checks every skill's level, unlocking what it covers and raising level-ups.</summary>
    private static void Sweep()
    {
        _sweeping = true;

        try
        {
            SweepSkills();
        }
        finally
        {
            _sweeping = false;
        }
    }

    private static void SweepSkills()
    {
        Dirty = false;
        _nextSweep = Time.time + SweepInterval;
        _earned.Clear();
        _leveled.Clear();

        for (int i = 0; i < Registry.Skills.Count; i++)
        {
            MimirSkill skill = Registry.Skills[i];
            float now = skill.StoredLevel;
            bool seen = _known.TryGetValue(skill, out float before);

            if (!seen && !skill.HasRecord())
            {
                skill.EnsureRecord();
            }
            else
            {
                if (seen && now > before)
                {
                    skill.LiftLocks();

                    if (Mathf.FloorToInt(now) > Mathf.FloorToInt(before))
                    {
                        _leveled.Add((skill, Mathf.FloorToInt(now)));
                    }
                }

                if (!seen || now > before || skill.NeedsCheck)
                {
                    CatchUp(skill, now, _earned);
                }
            }

            skill.Checked();
            _known[skill] = now;
        }

        foreach ((MimirSkill skill, int level) in _leveled)
        {
            API.RaiseLeveledUp(skill, level);
        }

        Unlocked(_earned);
    }

    /// <summary>Unlocks abilities as one notice; returns how many were newly unlocked.</summary>
    internal static int Grant(MimirSkill skill, IEnumerable<Ability> abilities)
    {
        List<(MimirSkill Skill, Ability Ability)> granted = new();

        foreach (Ability ability in abilities)
        {
            if (skill.Grant(ability))
            {
                granted.Add((skill, ability));
            }
        }

        Unlocked(granted);
        return granted.Count;
    }

    /// <summary>Unlocks what the level covers that is neither unlocked nor locked.</summary>
    private static void CatchUp(MimirSkill skill, float level, List<(MimirSkill, Ability)> into)
    {
        List<Ability> due = new();
        skill.Due(level, due);

        foreach (Ability ability in due)
        {
            if (skill.Grant(ability))
            {
                into.Add((skill, ability));
            }
        }
    }

    /// <summary>Announces the unlocks, then raises their events.</summary>
    private static void Unlocked(List<(MimirSkill Skill, Ability Ability)> granted)
    {
        if (granted.Count == 0)
        {
            return;
        }

        List<Ability> abilities = new();

        foreach ((MimirSkill Skill, Ability Ability) entry in granted)
        {
            abilities.Add(entry.Ability);
        }

        Announce(abilities);

        foreach ((MimirSkill Skill, Ability Ability) entry in granted)
        {
            API.RaiseUnlocked(entry.Skill, entry.Ability);
        }
    }

    /// <summary>Queues or shows one notice for everything unlocked at once, by the notice setting.</summary>
    private static void Announce(List<Ability> earned)
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

    /// <summary>Shows the next queued center notice once the spacing has passed.</summary>
    private static void Pump()
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

    /// <summary>The ability's own notice if it has one, else Mimir's default.</summary>
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
