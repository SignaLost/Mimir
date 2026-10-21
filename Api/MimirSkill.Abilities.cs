using System;
using System.Collections.Generic;
using UnityEngine;
using Mimir.Common;
using Mimir.Modules.Leveling;

namespace Mimir;

// Abilities and their saved unlocks.
public sealed partial class MimirSkill
{
    private readonly List<Ability> _abilities = new();
    private readonly Dictionary<string, Ability> _byId = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Ability> _byKey = new(StringComparer.OrdinalIgnoreCase);
    private int _added;
    private bool _dirty;

    // The loaded character's unlocks, reread when the saved text changes.
    private readonly HashSet<string> _unlocked = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _locked = new(StringComparer.OrdinalIgnoreCase);
    private Ability[] _snapshot = Array.Empty<Ability>();
    private Player _unlockedFor;
    private string _unlockedText;

    // Ability id or key, as given, to the sources disabling it; not saved.
    private readonly Dictionary<string, HashSet<string>> _disabled = new(StringComparer.OrdinalIgnoreCase);

    // ---- Add and remove ----

    /// <summary>
    ///     Adds an ability at a level, with its key, display name token, and optional unlock notice and description tokens.
    ///     Null, with a warning, if the skill already has an ability with this key.
    /// </summary>
    public Ability AddAbility(string id, string name, float level, string unlockText = null, string description = null)
    {
        if (!ValidId(id, name) || !ValidLevel(level, id))
        {
            return null;
        }

        Ability ability = new(this, id.ToLowerInvariant(), name, string.IsNullOrEmpty(unlockText) ? null : unlockText,
            string.IsNullOrEmpty(description) ? null : description, Mathf.Clamp(level, 0f, 100f), _added++);
        _abilities.Add(ability);
        _byId[ability.Id] = ability;
        _byKey[ability.Key] = ability;
        Read();
        ability.Flagged = _unlocked.Contains(ability.Id);
        _dirty = true;
        AbilitiesVersion++;
        NeedsCheck = true;
        Unlocks.Dirty = true;
        return ability;
    }

    /// <summary>Removes an ability, leaving any saved unlock of its id in place. False for an unknown id.</summary>
    public bool RemoveAbility(string id)
    {
        Ability ability = GetAbility(id);

        if (ability == null)
        {
            return false;
        }

        _abilities.Remove(ability);
        _byId.Remove(ability.Id);
        _byKey.Remove(ability.Key);
        ability.Registered = false;
        _dirty = true;
        AbilitiesVersion++;
        return true;
    }

    /// <summary>Moves an ability to another level, unlocking it at once if the character's level covers it. False for an unknown id.</summary>
    public bool SetAbilityLevel(string id, float level)
    {
        Ability ability = GetAbility(id);

        if (ability == null || !ValidLevel(level, id))
        {
            return false;
        }

        ability.Level = Mathf.Clamp(level, 0f, 100f);
        _dirty = true;
        NeedsCheck = true;
        Unlocks.Dirty = true;
        return true;
    }

    /// <summary>A copy of the abilities in level order, then the order added.</summary>
    public IReadOnlyList<Ability> Abilities
    {
        get
        {
            Sort();
            return _snapshot;
        }
    }

    /// <summary>The ability with this full id or key, ignoring case; null for an unknown id.</summary>
    public Ability GetAbility(string id)
    {
        if (id == null)
        {
            return null;
        }

        return _byId.TryGetValue(id, out Ability ability) || _byKey.TryGetValue(id, out ability) ? ability : null;
    }

    // ---- Reading ----

    /// <summary>Whether the ability can be used now: still part of the skill, unlocked and not disabled.</summary>
    public bool Has(Ability ability)
    {
        if (ability == null || ability.Skill != this || !ability.Registered)
        {
            return false;
        }

        Read();
        return ability.Flagged && !DisabledNow(ability);
    }

    /// <summary>Whether the character has unlocked the ability, disabled or not; false for an unknown id.</summary>
    public bool IsUnlocked(string id)
    {
        Ability ability = GetAbility(id);
        return ability != null && IsFlagged(ability.Id);
    }

    /// <summary>The next ability the character has yet to unlock, in level order; null when all are unlocked or no character is loaded.</summary>
    public Ability NextAbility
    {
        get
        {
            if (!Player.m_localPlayer)
            {
                return null;
            }

            Sort();
            Read();

            foreach (Ability ability in _snapshot)
            {
                if (!ability.Flagged)
                {
                    return ability;
                }
            }

            return null;
        }
    }

    /// <summary>The level the ability unlocks at; -1 for an unknown id.</summary>
    public float UnlockLevel(string id)
    {
        return GetAbility(id)?.Level ?? -1f;
    }

    // ---- Unlock and lock ----

    /// <summary>Unlocks an ability for the loaded character, with its notice and <see cref="Unlocked" /> event. True if it was not unlocked already.</summary>
    public bool Unlock(string id)
    {
        Ability ability = GetAbility(id);
        return ability != null && Unlocks.Grant(this, new[] { ability }) > 0;
    }

    /// <summary>Removes an ability's unlock from the loaded character until their next level-up. True if it was unlocked.</summary>
    public bool Lock(string id)
    {
        return Revoke(GetAbility(id));
    }

    // ---- Disable and enable ----

    /// <summary>
    ///     Disables an ability for a named source, without touching its unlock, until every source enables it again; never saved.
    ///     Takes the full id or key, added yet or not; false without an id or source.
    /// </summary>
    public bool Disable(string id, string source)
    {
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(source))
        {
            return false;
        }

        if (!_disabled.TryGetValue(id, out HashSet<string> sources))
        {
            sources = new HashSet<string>(StringComparer.Ordinal);
            _disabled[id] = sources;
        }

        return sources.Add(source);
    }

    /// <summary>Withdraws one source's <see cref="Disable" /> under either of the ability's names; false if that source had not disabled it.</summary>
    public bool Enable(string id, string source)
    {
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(source))
        {
            return false;
        }

        Ability ability = GetAbility(id);
        bool enabled = Withdraw(id, source);

        if (ability != null)
        {
            enabled |= Withdraw(ability.Id, source);
            enabled |= Withdraw(ability.Key, source);
        }

        return enabled;
    }

    private bool Withdraw(string name, string source)
    {
        if (!_disabled.TryGetValue(name, out HashSet<string> sources) || !sources.Remove(source))
        {
            return false;
        }

        if (sources.Count == 0)
        {
            _disabled.Remove(name);
        }

        return true;
    }

    /// <summary>Whether any source has the ability disabled now, by either form of its id.</summary>
    public bool IsDisabled(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return false;
        }

        Ability ability = GetAbility(id);
        return ability != null ? DisabledNow(ability) : _disabled.ContainsKey(id);
    }

    private bool DisabledNow(Ability ability)
    {
        return _disabled.Count > 0 && (_disabled.ContainsKey(ability.Id) || _disabled.ContainsKey(ability.Key));
    }

    // ---- Saved state ----
    // Stored in the character's data under UnlockKey as "unlocked ids|locked ids", by full id.

    /// <summary>Whether abilities changed since the last catch-up check.</summary>
    internal bool NeedsCheck { get; private set; }

    /// <summary>Rises whenever an ability is added or removed.</summary>
    internal int AbilitiesVersion { get; private set; }

    private bool IsFlagged(string id)
    {
        Read();
        return _unlocked.Contains(id);
    }

    /// <summary>Whether the loaded character has a record for this skill, even an empty one.</summary>
    internal bool HasRecord()
    {
        Player player = Player.m_localPlayer;
        return player && player.m_customData != null && player.m_customData.ContainsKey(UnlockKey);
    }

    /// <summary>Creates the character's record, silently unlocking what the level already covers; does nothing when one exists.</summary>
    internal void EnsureRecord()
    {
        if (HasRecord() || !Player.m_localPlayer)
        {
            return;
        }

        Read();
        float level = StoredLevel;

        foreach (Ability ability in _abilities)
        {
            if (ability.Level <= level)
            {
                _unlocked.Add(ability.Id);
                ability.Flagged = true;
            }
        }

        Write();
    }

    /// <summary>Marks an ability unlocked for the loaded character; true if it was not already.</summary>
    internal bool Grant(Ability ability)
    {
        if (ability == null || GetAbility(ability.Id) != ability || !Player.m_localPlayer)
        {
            return false;
        }

        EnsureRecord();
        Read();
        _locked.Remove(ability.Id);
        bool added = _unlocked.Add(ability.Id);
        ability.Flagged = true;
        Write();
        return added;
    }

    /// <summary>Removes an unlock and keeps it off until the next level-up; true if it was unlocked.</summary>
    internal bool Revoke(Ability ability)
    {
        if (ability == null || !Player.m_localPlayer)
        {
            return false;
        }

        EnsureRecord();
        Read();
        bool was = _unlocked.Remove(ability.Id);
        ability.Flagged = false;
        _locked.Add(ability.Id);
        Write();
        return was;
    }

    /// <summary>Removes every unlock and lock and schedules a catch-up check.</summary>
    internal void RevokeAll()
    {
        if (!Player.m_localPlayer)
        {
            return;
        }

        EnsureRecord();
        Read();
        _unlocked.Clear();
        _locked.Clear();

        foreach (Ability ability in _abilities)
        {
            ability.Flagged = false;
        }

        Write();
        NeedsCheck = true;
        Unlocks.Dirty = true;
    }

    /// <summary>Removes every lock.</summary>
    internal void LiftLocks()
    {
        Read();

        if (_locked.Count > 0)
        {
            _locked.Clear();
            Write();
        }
    }

    /// <summary>Adds the abilities the level covers that are neither unlocked nor locked to the list.</summary>
    internal void Due(float level, List<Ability> into)
    {
        Read();

        Sort();

        foreach (Ability ability in _snapshot)
        {
            if (ability.Level <= level && !ability.Flagged && !_locked.Contains(ability.Id))
            {
                into.Add(ability);
            }
        }
    }

    internal void Checked()
    {
        NeedsCheck = false;
    }

    private void Read()
    {
        Player player = Player.m_localPlayer;
        string text = null;

        if (player && player.m_customData != null)
        {
            player.m_customData.TryGetValue(UnlockKey, out text);
        }

        if (ReferenceEquals(player, _unlockedFor) && ReferenceEquals(text, _unlockedText))
        {
            return;
        }

        _unlockedFor = player;
        _unlockedText = text;
        _unlocked.Clear();
        _locked.Clear();

        if (!string.IsNullOrEmpty(text))
        {
            int bar = text.IndexOf('|');
            Split(bar < 0 ? text : text.Substring(0, bar), _unlocked);

            if (bar >= 0)
            {
                Split(text.Substring(bar + 1), _locked);
            }
        }

        foreach (Ability ability in _abilities)
        {
            ability.Flagged = _unlocked.Contains(ability.Id);
        }
    }

    /// <summary>Drops the cached record, such as when the character changes.</summary>
    internal void Forget()
    {
        _unlockedFor = null;
        _unlockedText = null;
        _unlocked.Clear();
        _locked.Clear();

        foreach (Ability ability in _abilities)
        {
            ability.Flagged = false;
        }
    }

    private static void Split(string text, HashSet<string> into)
    {
        foreach (string id in text.Split(','))
        {
            if (id.Length > 0)
            {
                into.Add(id);
            }
        }
    }

    private void Write()
    {
        Player player = Player.m_localPlayer;

        if (!player || player.m_customData == null)
        {
            return;
        }

        _unlockedText = string.Join(",", _unlocked) + "|" + string.Join(",", _locked);
        _unlockedFor = player;
        player.m_customData[UnlockKey] = _unlockedText;
    }

    /// <summary>Sorts by level, then the order added.</summary>
    private void Sort()
    {
        if (!_dirty)
        {
            return;
        }

        _dirty = false;
        _abilities.Sort((a, b) => a.Level != b.Level ? a.Level.CompareTo(b.Level) : a.Order.CompareTo(b.Order));
        _snapshot = _abilities.ToArray();
    }

    private bool ValidId(string id, string name)
    {
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(name))
        {
            Log.LogWarning($"{Id}: an ability needs an id and a name; ignored.");
            return false;
        }

        foreach (char c in id)
        {
            if (!char.IsLetterOrDigit(c) && c != '_' && c != '-')
            {
                Log.LogWarning($"{Id}: ability id '{id}' may only use letters, digits, '_' and '-'; ignored.");
                return false;
            }
        }

        if (_byKey.ContainsKey(id))
        {
            Log.LogWarning($"{Id} already has an ability '{id}'; ignored.");
            return false;
        }

        return true;
    }

    private bool ValidLevel(float level, string id)
    {
        if (!float.IsNaN(level))
        {
            return true;
        }

        Log.LogWarning($"{Id}: '{id}' was given no level; ignored.");
        return false;
    }
}
