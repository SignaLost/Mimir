using System.Collections.Generic;
using UnityEngine;

namespace Mimir;

/// <summary>
///     Diminishing returns for experience: the first use of a key pays in full, each use within the window pays
///     <c>falloff</c> times the one before, down to <c>floor</c>. A key left alone for a full window pays in full again.
/// </summary>
public sealed class Diminishing
{
    private readonly float _window;
    private readonly float _falloff;
    private readonly float _floor;
    private readonly float _cell;
    private readonly Table<object> _objects = new();
    private readonly Table<Vector3Int> _cells = new();

    /// <summary>Window in seconds, falloff and floor as shares of 1, and the size in meters of the cells positions are grouped into.</summary>
    public Diminishing(float window, float falloff = 0.5f, float floor = 0f, float cell = 2f)
    {
        _window = Mathf.Max(0f, window);
        _falloff = Mathf.Clamp01(falloff);
        _floor = Mathf.Clamp01(floor);
        _cell = Mathf.Max(0.01f, cell);
    }

    /// <summary>The share of experience this use pays, from 1 down to the floor; counts the use.</summary>
    public float Next(object key)
    {
        return key == null ? 1f : Share(_objects.Use(key, _window));
    }

    /// <summary>As <see cref="Next(object)" />, keyed by the cell the position falls in.</summary>
    public float Next(Vector3 position)
    {
        return Share(_cells.Use(Vector3Int.FloorToInt(position / _cell), _window));
    }

    /// <summary>Forgets every use.</summary>
    public void Clear()
    {
        _objects.Clear();
        _cells.Clear();
    }

    private float Share(int repeats)
    {
        return Mathf.Max(_floor, Mathf.Pow(_falloff, repeats));
    }

    private sealed class Table<TKey>
    {
        private const int PruneAbove = 256;

        private readonly Dictionary<TKey, (float Last, int Count)> _uses = new();
        private readonly List<TKey> _stale = new();
        private float _lastPrune;

        /// <summary>Counts a use and returns how many came before it within the window.</summary>
        internal int Use(TKey key, float window)
        {
            float now = Time.time;
            int count = _uses.TryGetValue(key, out (float Last, int Count) use) && now - use.Last < window ? use.Count : 0;

            if (_uses.Count > PruneAbove && now - _lastPrune >= window)
            {
                _lastPrune = now;
                Prune(now, window);
            }

            _uses[key] = (now, count + 1);
            return count;
        }

        internal void Clear()
        {
            _uses.Clear();
        }

        private void Prune(float now, float window)
        {
            foreach (KeyValuePair<TKey, (float Last, int Count)> entry in _uses)
            {
                if (now - entry.Value.Last >= window)
                {
                    _stale.Add(entry.Key);
                }
            }

            foreach (TKey key in _stale)
            {
                _uses.Remove(key);
            }

            _stale.Clear();
        }
    }
}
