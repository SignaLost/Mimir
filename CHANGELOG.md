# Changelog

## 1.0.2

- Fixed a fortifying skill's first death costing less than the game's own penalty on
  worlds with a reduced skill loss. A death now takes the game's usual share, but never
  drops below the fortified level.
- A skill no longer appears in the skills panel at level 0 before it has earned anything.
- Fixed a small memory leak from the blue level-up effects.
- SeneaL UI's skills panel is drawn more cheaply.

## 1.0.1

- Fixed a warning in the log for every skill row in SeneaL UI's skills panel.

## 1.0.0

First release. Mimir was split out of Valchitecture so other mods can share its skills.

- Skills from other mods, with abilities that unlock as the skill levels up and stay
  unlocked for good, even after death.
- Mods can also add abilities to the game's own skills and to other mods' skills.
- Stat bonuses that grow with a skill, such as health, stamina, armor, carry weight, speed
  and weapon damage. They are real stats, so food, gear and effects still apply.
- Fortified levels: a skill can keep a backup level that trails behind it, and dying only
  drops the skill back to it. Each mod decides for its own skills, and a server can turn
  it on for every skill or off for all.
- Fortified levels show in blue in the skills panel, including SeneaL UI's, and get their
  own level-up notice.
- A warning in the log when Fortify Skills Redux is also installed, since you should only
  use one of them.
- `Unlock Notices` setting: show new abilities as a popup (default), mid-screen, in the
  corner, or not at all.
- Skill Abilities page in the compendium (also in SeneaL UI's window), listing each
  skill's level, bonuses and abilities. It also shows what the game's own skills do at
  your current level.
- Works alongside Impactful Skills and VikingsDoSwim. `Absorb Impactful Skills` lists
  Impactful Skills' perks on the Skill Abilities page instead of its own page.
- `mimir` console command to see skills, levels, abilities and bonuses, and to change them
  with devcommands on.
- The game's `resetskill` command also clears a skill's unlocks and fortified level.
- For mod authors: `MimirAPI` to register skills, add abilities and bonuses, and give
  experience for the player's actions. See the README.
