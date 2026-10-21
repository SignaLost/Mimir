using System;
using System.IO;
using System.Reflection;
using System.Text;
using BepInEx;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;
using Mimir.Core;
using Mimir.Common;

namespace Mimir;

/// <summary>
///     The BepInEx entry point of Mimir, a library that gives other mods skills with ability unlocks, stat bonuses
///     and experience events. On load it binds the settings, adds the English translations and the <c>mimir</c>
///     console command, and applies every Harmony patch class on its own. Each frame it drives the unlock checks and
///     movement tracking in <c>Mimir.Core</c>.
/// </summary>
[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInProcess("valheim.exe")]
[BepInProcess("valheim.x86_64")]
[BepInProcess("valheim_server.exe")]
[BepInProcess("valheim_server.x86_64")]
[BepInDependency(Jotunn.Main.ModGuid)]
[NetworkCompatibility(CompatibilityLevel.NotEnforced, VersionStrictness.None)]
public sealed class MimirPlugin : BaseUnityPlugin
{
    public const string PluginName = "Mimir";
    public const string PluginGuid = "NullSignal.Mimir";
    public const string PluginVersion = "1.0.0";

    private const string EnglishFile = "mimir.json";
    private const string EnglishResource = "Mimir.Translations.English.json";

    private readonly Harmony _harmony = new(PluginGuid);

    private void Awake()
    {
        Log.Init(Logger);

        Cfg.Bind(Config);

        RegisterEnglish();
        CommandManager.Instance.AddConsoleCommand(new MimirCommand());
        ApplyPatches();

        Logger.LogInfo($"{PluginName} {PluginVersion} loaded.");
    }

    private void Update()
    {
        Unlocks.Update();
        Movement.Update();
    }

    private void OnDestroy()
    {
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

    /// <summary>Applies each patch class separately; a failed class is logged and the rest still apply.</summary>
    private void ApplyPatches()
    {
        int applied = 0;
        int failed = 0;

        foreach (Type type in AccessTools.GetTypesFromAssembly(Assembly.GetExecutingAssembly()))
        {
            try
            {
                PatchClassProcessor processor = _harmony.CreateClassProcessor(type);

                if (processor.Patch() is { Count: > 0 })
                {
                    applied++;
                }
            }
            catch (Exception e)
            {
                failed++;
                Logger.LogError($"Patch failed: {type.Name} - {e.Message}");
            }
        }

        Logger.LogInfo($"Applied {applied} patch classes" + (failed > 0 ? $", {failed} failed." : "."));
    }
}
