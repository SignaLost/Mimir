# Mimir

Mimir is a helper mod that other Valheim mods use to add skills. It takes care of skill
levels, unlocking abilities, stat bonuses and unlock notices, so every skill mod that uses
it works the same way.

On its own it adds nothing to the game, unless a server turns on fortifying for every
skill. You install it because a mod you want needs it, such as **Valchitecture**, which
uses it for its Building skill.

## What it does

**Abilities you keep.** Skills unlock abilities as they level up. Once you unlock one, it
is yours for good, even if you die and lose levels. Temporary skill boosts from status
effects don't unlock anything. Mods can also add abilities to the game's own skills, such
as Woodcutting or Sneak.

**Unlock notices.** Each new ability pops up with the skill's icon, the ability's name and
what it does. The `Unlock Notices` setting can show them mid-screen, in the corner, or not
at all, for every mod's skills at once.

**Stat bonuses.** Skills can raise your health, stamina, carry weight, speed, weapon
damage and more as they level. These are real stats, not status effects, so food, gear and
effects still work on top of them as usual.

**Fortified levels.** A skill can keep a backup level that trails a little behind it. When
you die, the skill only drops back to that level. Each mod decides for its own skills, and
a server can turn this on for every skill, the game's own included.

**Skill Abilities page.** A new compendium page lists each skill's level, its bonuses and
its abilities, with what each one does and the level the locked ones unlock at. It also
shows what the game's own skills do at your level, and lists Impactful Skills' perks if
you use that mod.

**Console command.** `mimir` lists every skill with its levels and unlocks. With
devcommands on, it can also change them for testing.

## For mod authors

Mimir is a library. Through `MimirAPI`, a mod registers a skill (or tracks an existing
one), adds abilities and stat bonuses, scales experience gains, and listens for the
player's actions to give experience for. The API FAQ has a guide with examples.

## More

The FAQ explains how levels and unlocks work, how to install and set up Mimir, and how to
translate it. Mod developers will also find the API guide and build instructions there.

## Credits

- **MidnightsFX**: *Impactful Skills*, the inspiration for skills that unlock perks as
  they level, for changing stats where the game reads them, and for a compendium page of
  skill perks.
- **Searica**: *Fortify Skills Redux*, and **Merlyn42** for the original *Fortify Skills*,
  the inspiration for fortified levels.

## License

GNU General Public License v3.0; see [LICENSE.md](LICENSE.md).
