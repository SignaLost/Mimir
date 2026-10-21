# Building from source

Works on Linux, macOS and Windows with the .NET SDK alone; no Mono or Visual Studio.

## What you need

- **.NET SDK** 8.0 or newer (`dotnet --list-sdks`). The project targets .NET Framework
  4.8 and pulls the reference assemblies for it from NuGet, so the first build needs
  network access.
- **Valheim** installed.
- **BepInEx** and **Jotunn** installed where the mod will run: the game folder for a
  manual install, or a mod manager profile.

## Build

```bash
dotnet build -c Release
```

The build finds Valheim in the usual Steam locations, BepInEx in the game folder or a
Gale/r2modman `Default` profile, and Jotunn in that BepInEx's `plugins`. It prints what
it found, publicizes the game assemblies as it goes, and installs the whole package into
`BepInEx/plugins/NullSignal.Mimir`, the way a mod manager would.

If any of those guesses is wrong, say so once in a `Directory.Build.props` beside the
`.csproj` (it is in `.gitignore`):

```xml
<Project>
  <PropertyGroup>
    <GamePath>/path/to/steamapps/common/Valheim</GamePath>
    <BepInExPath>/path/to/profile/BepInEx</BepInExPath>
    <!-- Only if Jotunn is not in that profile's plugins: -->
    <!-- <JotunnPath>/path/to/Jotunn.dll</JotunnPath> -->
  </PropertyGroup>
</Project>
```

`GamePath` is the folder that contains `valheim_Data`. `BepInExPath` is both what the
build compiles against and where it deploys, so point it at the profile you launch.
Any property can also be passed per build: `dotnet build -c Release -p:GamePath=...`.

## Packaging

The build also lays out `bin/Release` the way a mod manager expects, so it can be zipped
as it stands:

```
manifest.json  Mimir.dll  Mimir.pdb  Mimir.xml  icon.png  README.md  CHANGELOG.md  LICENSE.md  faq/  Translations/
```

The manifest, icon and README have to sit at the archive root. `icon.png` must be exactly
256x256. The `faq/` pages take their titles from each file's first line.

`manifest.json` is generated from the `.csproj`, so the version lives in one place:
`Version` is the release number, `Description` the blurb the mod list shows (256
characters at most), `ManifestWebsite` the repository URL, and the dependency lines in
the `PackageFiles` target pin the BepInEx pack and Jotunn.

`PluginGuid` names the folder deployed into and the config file, and the DLL stays
`Mimir.dll`. The build fails if it stops matching the constant in
`MimirPlugin.cs`, as does a version that drifts from `Version`.

`Mimir.xml` carries the API's documentation, so mods that reference the DLL see it
in their editor.

## Source layout

Only the plugin entry point sits at the root. Namespaces follow folders, so a type's
namespace says where its file is. Each module keeps its Harmony patches in a `Patches/`
folder beside the code they serve; logic never lives in a patch file.

| Folder | Contents |
|---|---|
| `Api/` | The public API: `API`, `MimirSkill`, abilities, bonuses, stats, events, ramps, curves, diminishing returns |
| `Common/` | Settings, logging, localization, embedded resources, safe event invocation |
| `Modules/Leveling/` | The skill registry, gains and modifiers, unlocks and notices, fortifying, death |
| `Modules/Stats/` | Bonus totals, and the game reads and results they adjust |
| `Modules/Tracking/` | The experience events: what the local player does, kills, taming, movement |
| `Modules/Commands/` | The `mimir` console command |
| `Thunderstore/` | The package icon, copied into the output folder |
| `faq/` | The mod page's FAQ entries, copied into the output folder |
| `Translations/` | The English text, one key per string; copied into the output folder and built into the DLL as a fallback |

## Building mods that use it

Build Mimir first. It deploys into the same profile, where mods that reference it,
such as Valchitecture, find `Mimir.dll`.
