# How do I install it?

## Installing

1. Install the BepInEx pack for Valheim and **Jotunn**.
2. Install Mimir with a mod manager. To install by hand, unzip it into a folder of its
   own inside `BepInEx/plugins`.

Mods that need Mimir, such as Valchitecture, list it as a dependency, so a mod manager
installs it for you. On its own Mimir changes nothing in the game, unless its `Fortify`
setting is set to fortify every skill.

## Settings

The settings are in `BepInEx/config/NullSignal.Mimir.cfg`. Settings marked "Server" come
from the server when it runs Mimir; otherwise each player's own value is used.

| Section | Setting | Does (default) | Set by |
|---|---|---|---|
| General | `Unlock Notices` | How new abilities show: `Popup` (default), `Announce`, `Notify` or `Off` | Player |
| | `Verbosity` | How much Mimir writes to the log: `Low` (default), `Medium` or `High` | Player |
| Fortify | `Fortify` | Which skills fortify: `ModsChoose` (default), `AllSkills` (the game's too) or `Off` | Server |
| | `Rate Per Level` | How much faster the fortified level catches up for each level it is behind (0.1) | Server |
| | `Max Rate` | The fastest it can catch up (0.8) | Server |
| | `Active Rate` | Experience boost for skills while they fortify (1.5) | Server |
| | `Max Gap` | Most levels a fortified level can fall behind (12) | Server |
| | `Max Fortified Level` | Highest level a skill can be fortified to (100) | Server |
| | `Fortify Notices` | How fortified level-ups show: `Effects` (default), `Notify` (no effects) or `Off` | Player |
| | `Show Fortified Levels` | Shows fortified levels in the skills panel (on) | Player |
| Compendium | `Skill Abilities Page` | Adds the Skill Abilities page (on) | Player |
| | `Hide Locked Descriptions` | Hides what an ability does until it unlocks (off) | Server |
| | `Show Game Skill Effects` | Shows what the game's own skills do at your level (on) | Player |
| | `Absorb Impactful Skills` | Lists Impactful Skills' perks on the page and removes its own page (on) | Player |

A mod can choose its own fortify settings for its skills, and those take priority. Only
setting `Fortify` to `Off` applies to every skill.

## Multiplayer

Your skills are saved with your character on your own computer, so a server doesn't need
Mimir. If the server has it, it also shares the server settings above and makes kills and
taming count for creatures the server controls. Some mods that use Mimir may still need
every player to install them.
