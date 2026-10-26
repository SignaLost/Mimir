using System.Reflection;
using HarmonyLib;
using Mimir.Compatibility;

namespace Mimir.Patch;

/// <summary>Adds fortified levels and bars to SeneaL UI's skills panel after each of its updates.</summary>
[HarmonyPatch]
internal static class SeneaL_SkillsView_Tick_Patch
{
    private static MethodBase _target;

    private static bool Prepare()
    {
        _target ??= SeneaLSkills.TickMethod();
        return _target != null;
    }

    private static MethodBase TargetMethod() => _target;

    private static void Postfix(object __instance)
    {
        SeneaLSkills.Apply(__instance);
    }
}
