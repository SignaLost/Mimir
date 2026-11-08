using System.IO;
using System.Text;
using BepInEx;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;
using Mimir.Core;
using Mimir.Common;
using Mimir.Compatibility;

namespace Mimir;

/// <summary>
///     The BepInEx entry point of Mimir, a library that gives other mods skills with ability unlocks, stat bonuses
///     and experience events. On load it binds the settings, adds the English translations and the <c>mimir</c>
///     console command, and applies the Harmony patches every game needs; the rest apply as mods ask for events and
///     stats (<see cref="PatchSets" />). Each frame it drives the unlock checks and movement tracking in <c>Mimir.Core</c>.
/// </summary>
[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInProcess("valheim.exe")]
[BepInProcess("valheim.x86_64")]
[BepInProcess("valheim_server.exe")]
[BepInProcess("valheim_server.x86_64")]
[BepInDependency(Jotunn.Main.ModGuid)]
[BepInDependency("seneaL.valheim.ui", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("blacks7ar.VikingsDoSwim", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(Auga.Guid, BepInDependency.DependencyFlags.SoftDependency)]
[NetworkCompatibility(CompatibilityLevel.NotEnforced, VersionStrictness.None)]
public sealed class MimirPlugin : BaseUnityPlugin
{
    public const string PluginName = "Mimir";
    public const string PluginGuid = "NullSignal.Mimir";
    public const string PluginVersion = "1.1.1";

    private const string EnglishFile = "mimir.json";
    private const string EnglishResource = "Mimir.Translations.English.json";

    private readonly Harmony _harmony = new(PluginGuid);
    private ConfigWatcher _configWatcher;

    private void Awake()
    {
        Log.Init(Logger);
        Auga.KeepOwnLook(PluginGuid);

        Cfg.Bind(Config);
        _configWatcher = new ConfigWatcher(Config);

        RegisterEnglish();
        CommandManager.Instance.AddConsoleCommand(new MimirCommand());
        PatchSets.ApplyCore(_harmony);

        Logger.LogInfo($"{PluginName} {PluginVersion} loaded.");
    }

    private void Start()
    {
        FortifySkillsRedux.Check();
        Auga.AddTooltipLines();
    }

    private void Update()
    {
        _configWatcher?.Tick();
        PatchSets.Flush();
        Unlocks.Update();
        Movement.Update();
    }

    private void OnDestroy()
    {
        _configWatcher?.Dispose();
        _harmony?.UnpatchSelf();
    }

    /// <summary>Adds the embedded English translations when no <c>Translations/English/mimir.json</c> is on disk for Jotunn to load.</summary>
    private void RegisterEnglish()
    {
        string onDisk = Path.Combine(Path.GetDirectoryName(Info.Location) ?? "", "Translations", "English", EnglishFile);

        if (File.Exists(onDisk))
        {
            return;
        }

        byte[] bytes = EmbeddedResources.Read(EnglishResource);

        if (bytes != null)
        {
            LocalizationManager.Instance.GetLocalization().AddJsonFile("English", Encoding.UTF8.GetString(bytes));
        }
    }

}
