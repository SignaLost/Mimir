# How do levels and unlocks work?

## Levels

A Mimir skill is a normal Valheim skill. It shows in the skills panel, goes up as you use
it and drops when you die. Anything that raises skills, such as status effects or the
world's skill gain rate, raises it too.

## Abilities

Each skill has abilities that unlock at certain levels. When your skill reaches that
level, the ability unlocks and is saved with your character. From then on it stays
unlocked, even if you die and lose levels, or a server later raises the level it needs.
Temporary skill boosts from status effects never unlock anything.

If a server lowers an ability's level below yours, or a mod update adds a new one you
already qualify for, it unlocks right away. When you first load a character with a new
skill mod, abilities your level already covers unlock quietly, without notices.

An ability locked by a mod or the console stays locked until your next level-up.

A mod can also switch an ability off for a while, for example during a boss fight. You
keep the unlock, but the ability does nothing until the mod turns it back on or you
restart the game.

## Death and fortifying

When you die, a skill loses part of its level, as usual in Valheim. Some mods make their
skills lose less.

A skill can also fortify. It then keeps a second, fortified level that slowly follows the
real one as you gain experience, and the skill gains experience faster while it does.
When you die, the skill only drops back to its fortified level. The fortified level never
falls more than `Max Gap` levels behind (12 by default), so a death costs at most that
many levels. At the start, a death costs no more than it would without fortifying. A
server can cap how high fortified levels go with `Max Fortified Level`. In a world with a
lighter death penalty you lose less, and with none you lose nothing.

Each mod decides whether its skills fortify. A server can instead use the `Fortify`
setting to fortify every skill, the game's own included, or none.

In the skills panel, fortified levels show in blue, such as "22 (18)", with a thin blue
bar that fills toward the next fortified level. `Show Fortified Levels` hides this. Each
new fortified level gets its own level-up notice, such as "Run (Fortified)", with blue
effects. `Fortify Notices` can leave out the effects or turn the notice off.

Fortify Skills Redux does the same job, so use one or the other, not both. Mimir writes a
warning in the log if it finds both, unless `Fortify` is `Off`.

## Stat bonuses

Skills can raise your stats as they level: health, stamina, eitr, armor, regeneration,
carry weight, speed, stamina costs, weapon damage, blocking, stealth, fall damage, rest
and more. Some bonuses only apply to one weapon type, and some come with an ability.

These are real stats, not status effects, so food, gear and effects work on top of them as
usual. Bonuses from different mods add up. Removing a mod puts your stats back to normal.

## Experience

Skill mods give experience for things you do: fighting, blocking, moving, gathering,
taming, crafting, cooking, building, eating, exploring, sleeping, trading and more.

In multiplayer, every player who damaged a creature gets credit for the kill. For kills
and taming to count fully, the game that controls the creature (often the server or
another player's game) also needs Mimir.

## Notices

You get a notice for each new ability, however the skill went up. The `Unlock Notices`
setting decides how it shows:

| Setting | Shows |
|---|---|
| `Popup` (default) | The game's unlock popup, with the skill's icon, the ability's name and what it does |
| `Announce` | A message mid-screen. Several at once show "New abilities unlocked" and list them in the corner |
| `Notify` | A message in the corner for each ability |
| `Off` | Nothing |

When several unlocks happen close together, they show one after another. UI mods such as
SeneaL UI show them in their own style. Loading a character doesn't repeat old unlocks.

## Skill Abilities page

The compendium has a new Skill Abilities page, after Active Effects and Logs. For each
skill it shows the level, the bonuses the skill gives you right now, and an "Abilities"
list you can click to open or close. Unlocked abilities show what they do. Locked ones
show the level they unlock at, and with `Hide Locked Descriptions` on, only their name.
With a gamepad, every list is open. Abilities a mod has switched off are left out.

With `Show Game Skill Effects` on, the game's own skills also show what they do at your
level, such as weapon damage, run speed, stamina costs or craft time.

If you use Impactful Skills, `Absorb Impactful Skills` lists its perks here as abilities
and removes its own Skill Perks page. Perks and skills it hides stay hidden.

With SeneaL UI, the page shows in its compendium window and matches its colors.

## Skill speed

Some mods make the first few levels of a skill a bit slower, so the first abilities take
real play. Mods can also offer a speed setting for their skill, or speed up or slow down
skills in general, the game's own included. These all stack, and the world's skill gain
rate still applies on top. To check the result, set Mimir's `Verbosity` to `High` and the
log shows each experience gain.

## The console

`mimir help` lists the commands. Anyone can look; changing anything needs devcommands.

| Command | Does |
|---|---|
| `mimir list` | Every Mimir skill, by console name, with its level |
| `mimir stats <skill>` | Level, fortified level, abilities and bonuses |
| `mimir level <skill> <n, +n or -n>` | Sets or changes the level. Raising it unlocks abilities; lowering it never locks any |
| `mimir unlock <skill> <ability or all>` | Unlocks without changing the level |
| `mimir reset <skill> <ability or all>` | Locks again without changing the level, until the next level-up |
| `mimir fortify <skill or all> <level or reset>` | Sets the fortified level, or starts it over. Works on any skill, the game's own included |

The game's own `resetskill` command also clears the skill's unlocks and fortified level.

## Other mods

Mimir works alongside Impactful Skills: when both change the same stat, the bonuses add
up. With VikingsDoSwim, bonuses to swim speed, swim stamina and stamina regen still work.
