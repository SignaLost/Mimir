# How do I install it?

## Installing

1. Install the BepInEx pack for Valheim and **Jotunn**.
2. Install it through a mod manager, or unzip the package into a folder of its own in
   `BepInEx/plugins`. Mods that need it, such as Valchitecture, install it for you.

On its own Mimir changes nothing in the game unless its `Fortify` setting fortifies every
skill; the mods that use it add the skills.

## Configuration

`BepInEx/config/NullSignal.Mimir.cfg`:

| Setting | Does |
|---|---|
| `Unlock Notices` | `Announce`, `Popup`, `Notify` or `Off`; see how notices work. Each player's own |
| `Verbosity` | How much it writes to the log. Each player's own |
| `Fortify` | Which skills fortify: `ModsChoose`, `AllSkills` (the game's too) or `Off`. Synced |
| `Rate Per Level`, `Max Rate` | How fast fortified levels catch up. Synced |
| `Max Gap` | Most levels a fortified level may trail its skill by, 1 to 100 (12). Synced |
| `Max Fortified Level` | Highest level a skill can be fortified to, 1 to 100 (100). Synced |
| `Active Rate` | Experience multiplier for fortifying skills. Synced |
| `Fortify Notices` | `Effects` (default), `Notify` or `Off`: a fortified level-up's notice with blue level-up effects, the notice only, or nothing. Each player's own |
| `Show Fortified Levels` | Shows fortified levels and their progress in the skills panel. Each player's own |
| `Skill Abilities Page` | Adds the compendium page. Each player's own |
| `Hide Locked Descriptions` | Hides what an ability does until it unlocks. Synced |
| `Show Game Skill Effects` | Lists what the game's own skills do at your level as perks on the page. Each player's own |
| `Absorb Impactful Skills` | Lists Impactful Skills' perks as abilities on the page and removes its Skill Perks page. Each player's own |

Synced settings come from the server when it runs Mimir; on a server without it, each
player's own apply to their game.

The fortify settings are defaults: a mod can set its own for its skills, and those win.
Only `Fortify` set to `Off` applies to every skill.

## Multiplayer

Skills are kept with each character on that player's own game, so a server does not need
Mimir. With it, the server also syncs the fortify settings and sends kill details and
taming credit for the creatures it owns. Mods that use Mimir may still require themselves
on every client.
