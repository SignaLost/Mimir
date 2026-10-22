using BepInEx.Bootstrap;
using Mimir.Common;
using Mimir.Modules;

namespace Mimir.Compatibility;

/// <summary>Warns when Fortify Skills Redux is installed alongside Mimir's fortifying; both change skills on death, so only one should run.</summary>
internal static class FortifySkillsRedux
{
    private const string Guid = "Searica.Valheim.FortifySkillsRedux";

    /// <summary>Logs the conflict once at startup; changes nothing.</summary>
    internal static void Check()
    {
        if (!Chainloader.PluginInfos.ContainsKey(Guid) || Fortifying.Mode?.Value == FortifyMode.Off)
        {
            return;
        }

        Log.LogWarning("Fortify Skills Redux is also installed. Both change skill levels on death and will conflict. "
                       + "Use one: remove Fortify Skills Redux, or set Mimir's Fortify setting to Off.");
    }
}
