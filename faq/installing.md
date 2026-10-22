# How do I install it?

## Installing

1. Install the BepInEx pack for Valheim and **Jotunn**.
2. Install Mimir through a mod manager, or unzip the package into a folder of its own in
   `BepInEx/plugins`. Mods that need it, such as Valchitecture, list it as a dependency.

On its own Mimir changes nothing in the game unless its `Fortify` setting fortifies every
skill; the mods that use it add the skills.

## Configuration

`BepInEx/config/NullSignal.Mimir.cfg`. Synced settings come from the server when it runs
Mimir; otherwise each player's own value applies.

| Section | Setting | Does | Scope |
|---|---|---|---|
| General | `Unlock Notices` | How unlocks show: `Popup` (default), `Announce`, `Notify` or `Off`; see how notices work | Each player's own |
| | `Verbosity` | How much Mimir writes to the log: `Low` (default), `Medium` or `High` | Each player's own |
| Fortify | `Fortify` | Which skills fortify: `ModsChoose` (default), `AllSkills` (the game's too) or `Off` | Synced |
| | `Rate Per Level` | Share of each gain the fortified level gets per level the skill is above it, 0 to 1 (0.1) | Synced |
| | `Max Rate` | Largest share it gets, 0 to 1 (0.8) | Synced |
| | `Active Rate` | Experience multiplier for fortifying skills, 0.1 to 5 (1.5) | Synced |
| | `Max Gap` | Most levels a fortified level may trail its skill by, 1 to 100 (12) | Synced |
| | `Max Fortified Level` | Highest level a skill can be fortified to, 1 to 100 (100) | Synced |
| | `Fortify Notices` | How fortified level-ups show: `Effects` (default, with blue level-up effects), `Notify` (the notice only) or `Off` | Each player's own |
| | `Show Fortified Levels` | Shows fortified levels and their progress in the skills panel (on) | Each player's own |
| Compendium | `Skill Abilities Page` | Adds the compendium page (on) | Each player's own |
| | `Hide Locked Descriptions` | Hides what an ability does until it unlocks (off) | Synced |
| | `Show Game Skill Effects` | Lists what the game's own skills do at your level as perks on the page (on) | Each player's own |
| | `Absorb Impactful Skills` | Lists Impactful Skills' perks as abilities on the page and removes its Skill Perks page (on) | Each player's own |

The fortify settings are defaults: a mod can set its own for its skills, and those win.
Only `Fortify` set to `Off` applies to every skill.

## Multiplayer

Skills are kept with each character on that player's own game, so a server does not need
Mimir. With it, the server also syncs the settings above and sends kill details and taming
credit for the creatures it owns. Mods that use Mimir may still require themselves on
every client.
