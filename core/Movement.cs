using UnityEngine;

namespace Mimir.Core;
/// <summary>
///     Adds up how far the local player walks, runs, sneaks, swims, sails and rides, and raises the matching distance
///     events about once a second. Counts only while a mod listens for a distance event; teleports, debug flying,
///     time in the air and riding a ship without steering it do not count.
/// </summary>
internal static class Movement
{
    private const float ReportInterval = 1f;

    /// <summary>Meters per frame above which a step counts as a teleport, not movement.</summary>
    private const float MaxStep = 10f;

    private enum Way
    {
        Walk,
        Run,
        Sneak,
        Swim,
        Sail,
        Ride,
        Count
    }

    private static readonly float[] _meters = new float[(int)Way.Count];
    private static Player _player;
    private static Vector3 _last;
    private static float _nextReport;

    /// <summary>Called every frame.</summary>
    internal static void Update()
    {
        Player player = Player.m_localPlayer;

        if (!ReferenceEquals(player, _player))
        {
            _player = player;
            System.Array.Clear(_meters, 0, _meters.Length);

            if (player)
            {
                _last = player.transform.position;
            }

            _nextReport = Time.time + ReportInterval;
        }

        if (!player)
        {
            return;
        }

        Vector3 position = player.transform.position;
        float step = Vector3.Distance(position, _last);
        _last = position;

        if (!Events.WantsMovement)
        {
            return;
        }

        if (step > 0f && step < MaxStep && Current(player) is Way way)
        {
            _meters[(int)way] += step;
        }

        if (Time.time >= _nextReport)
        {
            _nextReport = Time.time + ReportInterval;
            Report(player);
        }
    }

    private static Way? Current(Player player)
    {
        if (player.IsDead() || player.IsTeleporting() || player.IsDebugFlying())
        {
            return null;
        }

        if (player.IsRiding())
        {
            return Way.Ride;
        }

        if (player.GetControlledShip())
        {
            return Way.Sail;
        }

        if (player.IsAttached() || player.GetStandingOnShip())
        {
            return null;
        }

        if (player.IsSwimming())
        {
            return Way.Swim;
        }

        if (!player.IsOnGround())
        {
            return null;
        }

        if (player.IsCrouching())
        {
            return Way.Sneak;
        }

        return player.IsRunning() ? Way.Run : Way.Walk;
    }

    private static void Report(Player player)
    {
        for (int i = 0; i < _meters.Length; i++)
        {
            float meters = _meters[i];
            _meters[i] = 0f;

            if (meters <= 0f)
            {
                continue;
            }

            switch ((Way)i)
            {
                case Way.Walk:
                    Events.RaiseWalked(player, meters);
                    break;
                case Way.Run:
                    Events.RaiseRan(player, meters);
                    break;
                case Way.Sneak:
                    Events.RaiseSneaked(player, meters);
                    break;
                case Way.Swim:
                    Events.RaiseSwam(player, meters);
                    break;
                case Way.Sail:
                    Events.RaiseSailed(player, meters);
                    break;
                case Way.Ride:
                    Events.RaiseRode(player, meters);
                    break;
            }
        }
    }
}
