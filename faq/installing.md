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
| `Active Rate` | Experience multiplier for fortifying skills. Synced |
| `Skill Abilities Page` | Adds the compendium page. Each player's own |
| `Hide Locked Descriptions` | Hides what an ability does until it unlocks. Synced |

Synced settings come from the server when it runs Mimir; on a server without it, each
player's own apply to their game.

## Multiplayer

Skills are kept with each character on that player's own game, so a server does not need
Mimir. With it, the server also syncs the fortify settings and sends kill details and
taming credit for the creatures it owns. Mods that use Mimir may still require themselves
on every client.
