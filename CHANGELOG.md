# Changelog

## 1.0.0

First release, split out of Valchitecture so other mods can share its skill handling.

### Added

- **Skills** registered through Jotunn, with an optional eased pace for early levels, a
  gain rate for a mod's own speed setting, custom level curves and a death loss share.
- **Abilities**, each at a level a mod can change at any time, on new skills and on the
  game's or other mods' skills. An ability unlocks when the level reaches it and stays
  unlocked, saved with the character; dying never takes one away. Mods can add, remove,
  unlock, lock, disable and enable any skill's abilities.
- **Stat bonuses** that grow with a skill, flat or as a percentage, optionally tied to an
  ability: vitals, regeneration, food duration, armor, stagger, adrenaline, carry weight,
  reach, movement, stamina costs, weapon damage and costs (optionally for one weapon type,
  narrowed further by any test of the item), draw and reload time, block power, parrying,
  stealth, noise, fall damage, comfort and rest. They change the player's real stats, not
  status effects.
- **Display-only perks** with `AddPerk`, listing effects a mod applies in its own code
  among the skill's perks.
- **Experience events** for the local player, with a set for each weapon type and school
  of magic: combat, blocking, parrying, dodging, movement by kind, jumping, falling,
  gathering of every kind, taming, crafting, cooking, refining, fermenting, eating,
  building, exploring, sleeping, trading and any skill gain. Kills reach every player who
  damaged the creature, with its level and other details when its owner runs Mimir.
- **Gain modifiers** for one skill or every skill, **level-up events**, `NextAbility`,
  **diminishing returns** keyed by object or position, and `Ramp` helpers for values that
  grow with a level.
- **Fortifying**: a skill can keep a fortified level that trails its own and catches up
  through a share of each gain; death drops the skill back to it. Mods choose per skill;
  the synced `Fortify` settings can fortify every skill, the game's included, or none, and
  set the rates, `Max Gap` (12) and `Max Fortified Level` (100). The skills panel, the
  game's and SeneaL UI's, shows each fortified level and a thin blue bar for its progress.
  Fortified level-ups show as the game's skill-up notice, "Run (Fortified)", with blue
  level-up effects that `Fortify Notices` can leave out or turn off. A log warning says
  to pick one when Fortify Skills Redux is also installed.
- **Unlock Notices** setting: `Popup` (default) shows the game's unlock popup with the
  skill's icon, the ability's name and what it does; `Announce` shows an unlock
  mid-screen, or "New abilities unlocked" with each in the corner when several are earned
  at once; `Notify` lists every ability in the corner; `Off` shows nothing.
- **Skill Abilities page** in the compendium, also in SeneaL UI's window: each skill's
  level, perks and a collapsible list of abilities with their descriptions.
  `Hide Locked Descriptions` keeps what a locked ability does hidden. With
  `Show Game Skill Effects`, the game's own skills list what they do at your level.
- **Works alongside** Impactful Skills' stat changes and VikingsDoSwim's swimming. With
  `Absorb Impactful Skills`, Impactful Skills' perks are listed as abilities on the Skill
  Abilities page in place of its own Skill Perks page.
- `mimir` console command: skills, levels, abilities and bonuses; `level`, `unlock`,
  `reset` and `fortify` change the character and need devcommands. `resetskill` also
  removes the skill's unlocks and fortified level.
- `MimirAPI` for other mods: registering and tracking skills, readers by name, and events
  for each ability unlocked and each level-up.
