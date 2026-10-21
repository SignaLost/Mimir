using BepInEx.Configuration;
using Mimir.Modules;
using Mimir.Core;

namespace Mimir.Common;

/// <summary>Binds the mod's settings.</summary>
internal static class Cfg
{
    private const string SecGeneral = "1. General";
    private const string SecFortify = "2. Fortify";
    private const string SecCompendium = "3. Compendium";
    private const string LocalTag = "[Not Synced with Server]";
    private const string SyncedTag = "[Synced with Server]";

    internal static void Bind(ConfigFile config)
    {
        Log.Verbosity = config.Bind(SecGeneral, "Verbosity", Log.InfoLevel.Low, Unsynced("How much this mod writes to the log."));
        UnlockNotices.Setting = config.Bind(SecGeneral, "Unlock Notices", UnlockNotice.Popup, Unsynced("How skill unlocks show."));

        Fortifying.Mode = config.Bind(SecFortify, "Fortify", FortifyMode.ModsChoose,
            Synced("Which skills fortify: those their mods choose, every skill, or none."));
        Fortifying.RatePerLevel = config.Bind(SecFortify, "Rate Per Level", 0.1f,
            Synced("Share of experience the fortified level gets per level the skill is above it.", new AcceptableValueRange<float>(0f, 1f)));
        Fortifying.MaxRate = config.Bind(SecFortify, "Max Rate", 0.8f,
            Synced("Largest share of experience the fortified level gets.", new AcceptableValueRange<float>(0f, 1f)));
        Fortifying.ActiveRate = config.Bind(SecFortify, "Active Rate", 1.5f,
            Synced("Experience multiplier for fortifying skills.", new AcceptableValueRange<float>(0.1f, 5f)));

        AbilityPage.Show = config.Bind(SecCompendium, "Skill Abilities Page", true, Unsynced("Adds a compendium page listing skill perks and abilities."));
        AbilityPage.HideLocked = config.Bind(SecCompendium, "Hide Locked Descriptions", false,
            Synced("Hides what an ability does until it unlocks."));
    }

    private static ConfigDescription Unsynced(string description)
    {
        return new ConfigDescription($"{description} {LocalTag}");
    }

    private static ConfigDescription Synced(string description, AcceptableValueBase range = null)
    {
        return new ConfigDescription($"{description} {SyncedTag}", range, new ConfigurationManagerAttributes { IsAdminOnly = true });
    }
}
