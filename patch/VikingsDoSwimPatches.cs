using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Mimir.Common;
using Mimir.Compatibility;

namespace Mimir.Patch;

/// <summary>Applies Mimir's swim speed, swim stamina and stamina regen bonuses inside VikingsDoSwim's swimming prefixes.</summary>
[HarmonyPatch]
[ServesStat(Stat.SwimSpeed, Stat.SwimStamina, Stat.StaminaRegen)]
internal static class VikingsDoSwim_Prefixes_Patch
{
    private static bool Prepare() => VikingsDoSwim.Targets().Any();

    private static IEnumerable<MethodBase> TargetMethods() => VikingsDoSwim.Targets();

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
    {
        return VikingsDoSwim.Rewrite(instructions, original);
    }
}
