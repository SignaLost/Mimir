# Changelog

## 1.0.0

First release, split out of Valchitecture so other mods can share its skill handling.

### Added

- **Skills** registered through Jotunn, with an optional eased pace for early levels.
- **Abilities**, each at its own level, on new skills and on the game's or other mods'
  skills. Ids build on the skill's (`valchitecture.building.reach`), and names are
  translation tokens given at registration. An ability unlocks when the level reaches it
  and stays unlocked, saved with the character; dying never takes one away.
- **Full control** for mods over any skill's abilities: add, remove, unlock, lock, and
  disable or enable without touching the unlock.
- **Ability levels** a mod can change at any time, such as from a server setting.
- **Gain rate** per skill, for a mod's own speed setting.
- **Gain modifiers** for one skill or every skill, on top of the optional early-level pace.
- **Stat bonuses** that grow with a skill, flat or as a percentage, optionally tied to an
  ability: vitals, regeneration, food duration, armor, stagger, adrenaline, carry weight,
  reach, movement, stamina costs, weapon damage and costs (optionally for one weapon type,
  narrowed further by any test of the item), draw and reload time, block power, parrying,
  stealth, noise, fall damage, comfort and rest. They change the player's real stats, not
  status effects.
- **Experience events** for the local player, with a set for each weapon type and school of
  magic (attacks, hits and killing blows): combat, blocking, parrying, dodging, movement by
  kind, jumping, falling, harvesting, foraging, planting, chopping, mining, fishing, honey,
  sap, taming, petting, crafting, upgrading, repairing, cooking, refining, fermenting,
  eating, building, exploring, sleeping, trading and any skill gain. Kills reach every
  player who damaged the creature, with its level and other details when its owner runs
  Mimir.
- **Fortifying**: a skill can keep a fortified level that trails its own and catches up
  through a share of each gain; death drops the skill back to it. Mods choose per skill;
  synced `Fortify` settings can fortify every skill, the game's included, or none, and set
  the rates.
- **Death loss** per skill, as a share of the game's penalty.
- **Level curves** per skill, with common curves in `Curves`.
- **Level-up events** for Mimir's skills and for any skill, and `NextAbility`.
- **Diminishing returns** for experience, keyed by object or position.
- **Ramp** helpers for values that grow with a level, in steps or evenly.
- **Unlock Notices** setting: Popup (default) shows the game's unlock popup with the skill's
  icon, the ability's name and what it does; Announce shows an unlock mid-screen, or "New
  abilities unlocked" with each in the corner when several are earned at once; Notify
  lists every ability in the corner; Off shows nothing.
- **Skill Abilities page** in the compendium: each skill's level, perks and a collapsible
  list of abilities with their descriptions. `Hide Locked Descriptions` keeps what a
  locked ability does hidden.
  Works in SeneaL UI's compendium window.
- **Display-only perks** with `AddPerk`, listing effects a mod applies in its own code
  among the skill's perks.
- `mimir` console command: skills, levels, abilities and bonuses; level and unlock
  changes require devcommands; unlock and reset change only the unlock, never the level.
  `resetskill` also removes the skill's unlocks and fortified level.
- `MimirAPI` for other mods: registering and tracking skills, abilities, bonuses, gain
  modifiers, ability controls, readers by name, and events for each ability unlocked and
  each level-up.
