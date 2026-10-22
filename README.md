# Mimir

Skills for Valheim mods. Mimir registers a mod's skill, unlocks its abilities at levels the
mod or its server sets, raises the player's stats as it levels, and reports the actions it
gives experience for, so every mod that uses it works the same way.

On its own it adds nothing to the game, unless a server turns on fortifying for every
skill. Mods that use it, such as **Valchitecture** and its Building skill, get the same
rules, notices and console command.

## What it handles

**Unlocks that stay earned.** An ability unlocks when the level reaches it and is saved
with the character, so dying never takes one away. Status effects never unlock anything.
Mods can add, remove, unlock, lock, disable and enable abilities, their own or another
mod's, such as switching one off during a boss fight.

**Notices.** Each ability is announced as it is earned, in the game's unlock popup with the
skill's icon, the ability's name and what it does. The `Unlock Notices` setting can switch
to mid-screen messages, the corner, or nothing, for every mod's skills at once.

**Pacing.** A mod can ease a skill's earliest levels so its first unlocks take real play,
offer a speed setting for its skill, and scale gains of one skill or all of them. The
world's skill gain rate and status effects still apply.

**Stat bonuses.** A mod can raise the player's stats as its skill levels: health, stamina,
eitr, armor, regeneration, carry weight, reach, speeds, stamina costs, weapon damage and
costs per weapon type, draw and reload, blocking, stealth, fall damage and rest. They are
real stats, not status effects, so food, equipment and effects apply as usual.

**Experience events.** Attacks, hits and kills for each weapon type and school of magic,
blocks, parries and dodges, every way of moving, gathering of every kind, taming,
crafting, cooking, refining, building, eating, exploring, sleeping and trading are reported
to mods, so a skill mod writes its abilities instead of patching the game.

**Death and fortifying.** A mod can soften a skill's death penalty, or have it fortify: a
second level trails the skill's own in the background, and death only drops it back to
that. A server can fortify every skill, the game's included.

**Experience tools.** Custom level curves, level-up events, the next ability to unlock,
and diminishing returns so repeated actions on one thing can't farm a skill.

**The game's skills too.** A mod can give abilities to Woodcutting, Sneak or another mod's
skill without registering a skill of its own.

**Skill Abilities page.** A compendium page lists each skill's level, its perks and its
abilities, with what each does and the level the locked ones unlock at. The game's own
skills show what they do at your level, and Impactful Skills' perks are listed there too.

**One console command.** `mimir` lists every skill, shows its levels and unlocks, and with
devcommands sets levels, unlocks and fortified levels for testing.

## For mod authors

`MimirAPI` registers or tracks a skill and reads levels and unlocks. The `MimirSkill` it
hands back adds abilities and moves their levels, adds stat bonuses and scales gains.
`Ramp` grows values with a level, and `Events` reports the player's actions for giving
experience. The FAQ has a guide with examples.

## More

The FAQ covers how levels and unlocks work, the console command, the API, installing,
translating and building from source.

## License

GNU General Public License v3.0; see [LICENSE.md](LICENSE.md).
