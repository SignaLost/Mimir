using System.Reflection;
using HarmonyLib;
using Mimir.Compatibility;

namespace Mimir.Patch;

/// <summary>Adds fortified levels to Auga's skill rows after each of their refreshes.</summary>
[HarmonyPatch]
internal static class Auga_SkillRow_Patch
{
    private static MethodBase _target;

    private static bool Prepare()
    {
        _target ??= Auga.SkillRowMethod();
        return _target != null;
    }

    private static MethodBase TargetMethod() => _target;

    private static bool _warned;

    private static void Postfix(object __instance)
    {
        // A throw here would cut short Auga's own refresh of the rows after this one.
        try
        {
            Auga.MarkSkillRow(__instance);
        }
        catch (System.Exception e)
        {
            if (!_warned)
            {
                _warned = true;
                Mimir.Common.Log.LogError($"Fortified levels on Auga's skills panel failed: {e}");
            }
        }
    }
}
