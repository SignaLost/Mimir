# How do levels and unlocks work?

## Levels

A Mimir skill is an ordinary game skill: it shows in the skills panel, rises with use,
drops on death and is raised by anything that raises skills, such as status effects and
the world's skill gain rate.

## Death and fortifying

A skill loses the game's share of its level on death, or a part of it if its mod says so.

A fortifying skill keeps a second level that trails its own: each gain gives it a share,
larger the further the skill is ahead of it, so it never passes the skill. On death the
skill falls back to that level. It starts where the game's penalty would leave the skill,
so fortifying begins costing no more than the game, and the skill gains faster while it
fortifies. It never trails the skill by more than `Max Gap` levels, 12 by default, so a death
costs at most that much, and a server can cap how high it goes with `Max Fortified Level`.
A world with a lighter death penalty softens the fall, and one without any costs nothing. A mod decides whether its skills fortify; a server can instead fortify every
skill, the game's included, or none.

The skills panel shows each fortifying skill's fortified level in blue, and a thin blue bar
under the progress bar that fills toward the next fortified level. The game's panel shows the
level as "22 (18)"; SeneaL UI's shows it at the end of the blue bar. `Show Fortified Levels`
turns it off. Each fortified level earned shows as the game's own skill-up notice, named
"Run (Fortified)", so UI mods that restyle skill-ups show it the same way. It comes with the
game's level-up effects in blue at half opacity, with fewer particles and a lower pitch; `Fortify Notices`
can leave the effects out or turn the notice off.

Fortify Skills Redux does the same job; run one or the other, since both change skills on death.
Mimir warns in the log when it finds both, and changes nothing.

## Abilities

A mod gives its skill abilities, each at a level. When the skill's level reaches an
ability, the character unlocks it, and the unlock is saved with the character. From then
on it stays unlocked whatever happens to the level: dying, a server raising the ability's
level, or anything else. Status effects that raise a skill for a while never unlock
anything.

Mimir catches up on every level-up, when a character loads, and whenever a mod adds or
moves an ability: anything the level covers that is not unlocked yet unlocks then, with its
notice. So an ability a server moves below your level, or one a mod update adds, arrives
right away. A skill your character has never had before unlocks what its level already
covers silently, the first time it loads.

An ability a mod or the console locks stays locked, even though the level covers it, until
the next level-up.

Mods can let a server change each ability's level, and how fast the skill levels, in their
own settings.

## What mods can change

Mods have full control over abilities, their own and each other's: they can add and
remove abilities, unlock and lock them for a character, and disable and enable them. A
disabled ability keeps its unlock but does nothing, such as during a boss fight, or when a
mod switches off another mod's version of an ability it provides. Disabling is never
saved; it lasts until every mod that disabled the ability enables it, or the game closes.

## Stat bonuses

A mod can give its skill bonuses to the player's stats: health, stamina, eitr, armor,
regeneration, food duration, carry weight, reach, movement speeds, the stamina each action
costs, weapon damage and costs, bow draw and crossbow reload, block power and parrying,
stealth and noise, fall damage, comfort and rest. Weapon bonuses can be limited to one
weapon type. The mod decides how much each grows with the level, and can tie one to an
ability.

They are real stats, not status effects: each value is changed where the game computes it,
so food, equipment, skills and status effects apply as they do for the base game's values.
Bonuses from every mod add up. The player's own values are never changed, so removing a
mod returns the stats to normal.

## Experience events

Mimir reports what the player does, so mods give experience for the same actions the same
way without each patching the game: attacks, hits and kills for each weapon type and
school of magic; blocks, parries and dodges; walking, running, sneaking, swimming,
sailing, riding, jumping and falling; harvesting, foraging, planting, chopping, mining,
fishing, honey and sap; taming and petting; crafting, upgrading, repairing, cooking,
refining and fermenting; building; eating; exploring, sleeping and trading.

A kill is reported to every player who damaged the creature; its level and other details
come from Mimir on the game that owned it, so they are missing when that game, another
player's or a dedicated server's, doesn't run Mimir. Taming likewise needs Mimir on the
game that owns the creature.

## Notices

Mimir announces each unlock as it happens, whatever raised the skill: experience, the
console or another mod. The `Unlock Notices` setting decides how:

| Setting | Shows |
|---|---|
| **Announce** | One ability mid-screen; several at once show "New abilities unlocked" mid-screen and each ability in the corner |
| **Popup** (default) | The game's unlock popup for each ability: the skill's icon, the ability's name, and its description or unlock notice |
| **Notify** | Each ability in the corner |
| **Off** | Nothing |

The game shows one message mid-screen at a time, so notices from separate moments take
turns, four seconds apart; the game queues its popups the same way. UI mods that take
over the game's notices, such as SeneaL UI, show these in their own style. Loading a
character announces nothing it already had.

## Skill Abilities page

The compendium gets a Skill Abilities page, after Active Effects and Logs. Each skill with
perks or abilities shows its level, then its perks: the stat bonuses it gives right now.
Click "Abilities" to show or hide its list. Unlocked abilities show their name and what
they do; locked ones are greyed out with the level they unlock at, and with
`Hide Locked Descriptions` only their name. With a gamepad every list shows, since links
cannot be clicked. Abilities a mod or setting has disabled are left out.

With `Show Game Skill Effects`, the game's own skills list what they do at your level among
their perks, worked out with the game's own formulas: weapon damage range and attack
stamina, block power, jump force, run, swim, dodge and sneak stamina, mount stamina and
speed, fishing, craft time and bonus yield, harvest radius and build stamina. Values the
game keeps on its prefabs, such as swim stamina or the fishing float, are read from them.

With Impactful Skills installed and `Absorb Impactful Skills` on, its perks are listed as
abilities of their skills, with its own unlock levels and descriptions, and its Skill Perks
page is removed. Perks it turns off, and skills it hides, are left out.

With SeneaL UI, the page shows in its compendium window and the links work there; the
page keeps its scroll position when a list opens or closes, and it takes its colors from
the theme SeneaL API shares.

## Pace and gain modifiers

A mod can ease its skill's earliest levels. Gains are scaled so no level costs less than
a set number of points, which makes the first unlocks take real play, while high levels
cost about what the game asks for its own skills. Only the gain is scaled, so the world's
skill gain rate still applies. A mod can offer a speed setting for its skill, and mods can
scale gains of one skill or of every skill, the game's included; all of these multiply.
Set Mimir's `Verbosity` to `High` and the log prints each scaled gain.

## The console

`mimir help` lists the subcommands. Readouts are open to all; changes need devcommands.

| Command | Does |
|---|---|
| `mimir list` | Every skill Mimir keeps, by console name |
| `mimir stats <skill>` | Level, progress, fortified level, each ability (unlocked, its level, and whether disabled) and each bonus now |
| `mimir level <skill> <n, +n or -n>` | Sets or changes the level; raising it unlocks what it passes, and nothing is ever locked |
| `mimir unlock <skill> <ability or all>` | Unlocks without changing the level |
| `mimir reset <skill> <ability or all>` | Removes the unlock without changing the level; the next level-up restores it |
| `mimir fortify <skill or all> <level or reset>` | Sets the fortified level, within `Max Gap` and `Max Fortified Level`, or starts it over, without changing the skill. Takes any skill the character has, by Mimir name, the game's name or the name shown in game |

The game's `resetskill` resets the skill, its fortified level, and all of its unlocks.

## Other mods

Stat bonuses are applied right after the game reads each stat, before other mods' changes,
so mods that change the same stats, such as Impactful Skills, add on top. VikingsDoSwim
replaces the game's swimming with its own, and Mimir applies swim speed, swim stamina and
stamina regen there too.
