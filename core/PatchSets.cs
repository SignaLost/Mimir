using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;
using Mimir.Common;
using Mimir.Patch;

namespace Mimir.Core;

/// <summary>
///     Applies Mimir's Harmony patches: the ones every game needs at startup, and the rest only once a mod asks for
///     what they serve, an event through <see cref="Events" /> or a stat through a <see cref="Bonus" />. A game with
///     mods that use only unlocks and the compendium page is left with a handful of patches, so there is less for
///     other mods to collide with and less to break when the game changes. Each class is applied on its own, so a
///     failed one is logged and the rest still apply. The log names every game method patched and the mod it was
///     patched for, so what Mimir has changed can be read off it.
/// </summary>
internal static class PatchSets
{
    private static Harmony _harmony;
    private static readonly List<Type> _classes = new();
    private static readonly HashSet<Type> _applied = new();
    private static readonly Dictionary<Need, string> _needs = new();
    private static readonly Dictionary<Stat, string> _stats = new();
    private static readonly List<string> _methods = new();

    // Methods patched for each mod since the last flush, so one line covers everything a mod asked for.
    private static readonly Dictionary<string, List<string>> _pending = new();
    private static int _failures;

    /// <summary>Applies the classes that serve nothing in particular, which every game needs, and whatever was asked for before this ran.</summary>
    internal static void ApplyCore(Harmony harmony)
    {
        _harmony = harmony;
        _classes.Clear();
        _applied.Clear();
        _methods.Clear();
        int failed = 0;

        foreach (Type type in AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly()))
        {
            if (!type.IsDefined(typeof(HarmonyPatch), false) || type == typeof(StatReads))
            {
                continue;
            }

            _classes.Add(type);

            if (!type.IsDefined(typeof(ServesAttribute), false) && !type.IsDefined(typeof(ServesStatAttribute), false))
            {
                Apply(type, ref failed);
            }
        }

        Log.LogInfo($"Mimir patched: {Listed()}" + Failures(failed));

        foreach (KeyValuePair<Need, string> need in _needs)
        {
            ApplyFor(need.Key, need.Value);
        }

        foreach (KeyValuePair<Stat, string> stat in _stats)
        {
            ApplyFor(stat.Key, stat.Value);
        }
    }

    /// <summary>Applies the classes serving the need, once, for the mod asking.</summary>
    internal static void Want(Need need)
    {
        if (_needs.ContainsKey(need))
        {
            return;
        }

        string asker = Asker();
        _needs[need] = asker;

        if (_harmony != null)
        {
            ApplyFor(need, asker);
        }
    }

    /// <summary>Applies the classes serving the stat, and the stat's reads, once, for the mod asking.</summary>
    internal static void Want(Stat stat)
    {
        if (_stats.ContainsKey(stat))
        {
            return;
        }

        string asker = Asker();
        _stats[stat] = asker;

        if (_harmony != null)
        {
            ApplyFor(stat, asker);
        }
    }

    private static void ApplyFor(Need need, string asker)
    {
        _methods.Clear();
        int failed = 0;

        foreach (Type type in _classes)
        {
            if (!_applied.Contains(type) && type.GetCustomAttribute<ServesAttribute>() is ServesAttribute serves && Array.IndexOf(serves.Needs, need) >= 0)
            {
                Apply(type, ref failed);
            }
        }

        Queue(asker, failed);
    }

    private static void ApplyFor(Stat stat, string asker)
    {
        _methods.Clear();
        int failed = 0;

        foreach (Type type in _classes)
        {
            if (!_applied.Contains(type) && type.GetCustomAttribute<ServesStatAttribute>() is ServesStatAttribute serves && Array.IndexOf(serves.Stats, stat) >= 0)
            {
                Apply(type, ref failed);
            }
        }

        StatReads.Ensure(_harmony, stat, _methods);
        Queue(asker, failed);
    }

    private static void Queue(string asker, int failed)
    {
        _failures += failed;

        if (_methods.Count == 0)
        {
            return;
        }

        if (!_pending.TryGetValue(asker, out List<string> methods))
        {
            _pending[asker] = methods = new List<string>();
        }

        methods.AddRange(_methods);
    }

    /// <summary>Writes one line per mod for what was patched for it since the last call; called once a frame.</summary>
    internal static void Flush()
    {
        if (_pending.Count == 0 && _failures == 0)
        {
            return;
        }

        foreach (KeyValuePair<string, List<string>> mod in _pending)
        {
            Log.LogInfo($"{mod.Key} patched: {string.Join(", ", mod.Value)}" + Failures(_failures));
            _failures = 0;
        }

        if (_failures > 0)
        {
            Log.LogInfo("Some patches failed, see above.");
        }

        _pending.Clear();
        _failures = 0;
    }

    private static readonly HashSet<MethodBase> _before = new();

    /// <summary>Applies one class and notes the game methods newly patched by it, read off Harmony's own list.</summary>
    private static void Apply(Type type, ref int failed)
    {
        _applied.Add(type);
        _before.Clear();
        _before.UnionWith(_harmony.GetPatchedMethods());

        try
        {
            _harmony.CreateClassProcessor(type).Patch();
        }
        catch (Exception e)
        {
            failed++;
            Log.LogError($"Patch failed: {type.Name} - {e.Message}");
        }

        foreach (MethodBase method in _harmony.GetPatchedMethods())
        {
            if (!_before.Contains(method))
            {
                _methods.Add(NameOf(method));
            }
        }
    }

    /// <summary>The game method's name as a player would recognize it, such as Player.TryPlacePiece.</summary>
    internal static string NameOf(MethodBase method)
    {
        return (method.DeclaringType?.Name ?? "?") + "." + method.Name;
    }

    private static string Listed()
    {
        return _methods.Count > 0 ? string.Join(", ", _methods) : "nothing";
    }

    private static string Failures(int failed)
    {
        return failed > 0 ? $"; {failed} failed, see above." : ".";
    }

    /// <summary>The mod asking: the first assembly on the call stack that is not Mimir's, or the game's own code.</summary>
    private static string Asker()
    {
        Assembly own = typeof(PatchSets).Assembly;

        foreach (StackFrame frame in new StackTrace(false).GetFrames() ?? Array.Empty<StackFrame>())
        {
            Assembly assembly = frame.GetMethod()?.DeclaringType?.Assembly;

            if (assembly != null && assembly != own)
            {
                return assembly.GetName().Name;
            }
        }

        return "Mimir";
    }
}
