using System.Collections.Generic;
using UnityEngine;
using Mimir.Common;
using Mimir.Modules;

namespace Mimir.Core;

/// <summary>
///     Watches each Mimir skill's level for the local player. When a level rises it raises the level-up events and
///     unlocks every ability the new level covers, then hands the unlocks to <see cref="UnlockNotices" /> and raises
///     the unlock events. Skills are checked the frame after a known change and every half second otherwise, which
///     also catches levels other mods set directly.
/// </summary>
internal static class Unlocks
{
    private const float SweepInterval = 0.5f;

    private static readonly Dictionary<MimirSkill, float> _known = new();
    private static readonly List<(MimirSkill Skill, Ability Ability)> _earned = new();
    private static readonly List<(MimirSkill Skill, int Level)> _leveled = new();
    private static Player _watched;
    private static float _nextSweep;
    private static bool _sweeping;

    /// <summary>Set when a level or an ability may have changed; the next frame checks every skill.</summary>
    internal static bool Dirty = true;

    /// <summary>Called every frame.</summary>
    internal static void Update()
    {
        Player player = Player.m_localPlayer;

        if (!ReferenceEquals(player, _watched))
        {
            _watched = player;
            _known.Clear();
            UnlockNotices.Clear();
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

        if (Dirty || Time.time >= _nextSweep)
        {
            Sweep();
        }

        UnlockNotices.Update();
    }

    /// <summary>Unlocks and raises level-ups now rather than next frame; does nothing during a running sweep.</summary>
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
            MimirAPI.RaiseLeveledUp(skill, level);
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

        UnlockNotices.Announce(abilities);

        foreach ((MimirSkill Skill, Ability Ability) entry in granted)
        {
            MimirAPI.RaiseUnlocked(entry.Skill, entry.Ability);
        }
    }
}
