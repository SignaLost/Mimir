using System;
using System.Collections.Generic;
using Mimir.Common;

namespace Mimir.Core;
/// <summary>Gain modifiers for every skill, from <see cref="MimirAPI.AddGainModifier" />.</summary>
internal static class Gain
{
    internal static readonly List<Func<Player, Skills.SkillType, float>> Modifiers = new();

    internal static float Global(Player player, Skills.SkillType type)
    {
        float multiplier = 1f;

        for (int i = 0; i < Modifiers.Count; i++)
        {
            try
            {
                multiplier *= Modifiers[i](player, type);
            }
            catch (Exception e)
            {
                Log.LogError($"A gain modifier failed for {type}: {e.Message}");
            }
        }

        return multiplier;
    }
}
