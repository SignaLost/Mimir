# How do I use Mimir from my mod?

Everything goes through `MimirAPI`, a public static class in Mimir.dll, and the
`MimirSkill` it hands back. It works for the local player, must be called from the main
thread, and reads 0 while no character is loaded. `MimirAPI.Version` rises when members
are added; existing members keep their meaning.

## Depending on Mimir

Reference Mimir.dll and declare the dependency, so Mimir loads first:

```csharp
[BepInDependency("NullSignal.Mimir")]
public class MyPlugin : BaseUnityPlugin { }
```

On Thunderstore, list `NullSignal-Mimir-1.0.0` in your manifest's dependencies.

## Registering a skill

Register in your plugin's `Awake`, as the game sets up skills when it starts:

```csharp
internal static MimirSkill Foraging;

private void Awake()
{
    Foraging = MimirAPI.Register(new SkillDefinition
    {
        Id = "mymod.foraging",              // your mod, then the skill; never change it after release
        Name = "$mymod_skill_foraging",     // a translation token
        Description = "$mymod_skill_foraging_description",
        Icon = myIcon,
        EarlyLevelCost = 20f                // optional: eases the earliest levels
    });
}
```

The id is hashed into the game's skill type, so changing it loses every character's
progress. `EarlyLevelCost` scales gains so no level costs less than about that many
points; 0 keeps the game's pace. `SkillDefinition` also takes `Key` (the console name,
by default the last part of the id), `IncreaseStep` (the game's multiplier on the skill's
experience), `LevelCurve`, `DeathLoss` and `Fortify`, described below. `Register` returns
the skill already registered under the same id, or null, logged, when the id is invalid
or Jotunn refuses it.

## Abilities

Add them once, after registering. Each takes an id, a name (a translation token), a level
and, optionally, its own unlock notice and a description for the Skill Abilities page
(also tokens):

```csharp
private static Ability Gathering;

Gathering = Foraging.AddAbility("gathering", "$mymod_ability_gathering", 20,
    "$mymod_ability_gathering_unlocked", "$mymod_ability_gathering_description");
Foraging.AddAbility("double", "$mymod_ability_double_pick", 50);
Foraging.AddAbility("seeds", "$mymod_ability_seed_saver", 50);
```

Ids build on each other:

| | Id | Console |
|---|---|---|
| Skill | `mymod.foraging` | `foraging` |
| Ability | `mymod.foraging.gathering` | `gathering` |

An ability's `Id` is its skill's id and its own; unlocks are saved under it. Its `Key` is
its own part, typed in the console after the skill (`mimir unlock foraging gathering`).
Every call that takes an ability accepts either. Ids are lowercased and may use letters,
digits, `_` and `-`; a skill's id may also use `.`. An ability id must be unique within its
skill, so a mod adding to another mod's skill picks one that skill does not use;
`AddAbility` returns null, with a warning, for a taken id.

The unlock notice is a token too, such as `"mymod_ability_gathering_unlocked": "You can
now gather"`; `$1` in it is filled with the ability's name. Without one, Mimir shows its
own "`$1` unlocked". Abilities earned at once are announced together, so the two at 50
above show as "New abilities unlocked" with each listed in the corner.

Each unlocks when the skill's level reaches it and stays unlocked, saved with the
character. Check an ability where the feature runs with `Has`, which is cheap enough for
every frame and also respects removing and disabling (below):

```csharp
if (Foraging.Has(Gathering) && MyConfig.Gathering.Value)
{
    // ...
}
```

`IsUnlocked(id)` says whether the character has the unlock at all, disabled or not.
Use `Level` or `Factor` for effects that should follow the current level, as the game's
own skills do.

## Full control

Every part of an ability is open to mods, including another mod's abilities:

| Do | Call | Lasts |
|---|---|---|
| Add | `AddAbility(id, name, level, unlockText, description)` | Until removed or the game closes |
| Remove | `RemoveAbility(id)` | Until added again |
| Unlock | `Unlock(id)` | Saved with the character |
| Lock | `Lock(id)` | Saved, until the next level-up |
| Disable | `Disable(id, source)` | Until every source enables it, or the game closes |
| Enable | `Enable(id, source)` | |

**Disable** for an ability that should switch off for a while, such as during a boss
fight, or for good, such as another mod's version of an ability yours provides:

```csharp
Foraging.Disable("gathering", "MyMod.BossFight");
// ... the fight ...
Foraging.Enable("gathering", "MyMod.BossFight");

// Switch off another mod's version for the whole session.
MimirAPI.Disable("building", "valchitecture.building.reach", "mymod");
```

The source names who disabled it, so two mods never undo each other: the ability stays off
until every source has enabled it. Disabling keeps the unlock, is never saved, and works
for an ability not added yet, so it can be done from `Awake` once the skill exists;
depend on the mod that registers it.

**Remove** takes the ability out of the skill: `Has` is false for it, even for the mod
holding it, and it leaves the notices and the console. The character's unlock stays saved
under its id, so an ability added again under that id finds it unlocked. To replace
another mod's ability with yours, remove theirs, then add yours: under the same id,
characters who had theirs keep the unlock; under a new id, yours is earned afresh.

**Unlock** grants an ability as reaching its level would, with its notice and event.
**Lock** removes the unlock but leaves the level alone. The ability stays locked, across
relogs, until the character's next level-up, which unlocks again whatever their level
covers. To keep one off, disable it, or remove it.

## Settings for your skill

Mimir keeps no settings of its own for a skill; your mod owns them and decides what to
offer. Pass changes on, so a server's synced values apply for everyone.

**Skill speed.** `GainRate` multiplies every gain, on top of Mimir's pace and the
game's own rules:

```csharp
ConfigEntry<float> gain = Config.Bind("Progression", "Skill Gain", 1f,
    new ConfigDescription("How fast Foraging levels; 2 is twice as fast.", new AcceptableValueRange<float>(0.1f, 5f)));

Foraging.GainRate = gain.Value;
gain.SettingChanged += (_, _) => Foraging.GainRate = gain.Value;
```

**Unlock levels.** One setting per ability:

```csharp
ConfigEntry<int> gatheringLevel = Config.Bind("Progression", "Gathering Level", 20,
    new ConfigDescription("Foraging level that unlocks gathering.", new AcceptableValueRange<int>(0, 100)));

Gathering = Foraging.AddAbility("gathering", "$mymod_ability_gathering", gatheringLevel.Value);
gatheringLevel.SettingChanged += (_, _) => Foraging.SetAbilityLevel(Gathering.Id, gatheringLevel.Value);
```

An ability already unlocked stays unlocked wherever it moves. One moved to or below a
character's level unlocks right away, with its notice.

## Ramps

`Ramp` computes values that grow with a level, so every effect is one line:

```csharp
// 5 m from the ability's level, 1.25 m more every 4 levels, at most 25 m.
float radius = Ramp.Stepped(Foraging.Level, Gathering.Level, 4, 5f, 1.25f, 25f);

// Stamina cost 2% lower every 4 levels, down to nothing.
float stamina = Ramp.Stepped(Foraging.Level, 0, 4, 1f, -0.02f, 0f);

// Up to 50% faster by level 100, evenly.
float speed = Ramp.Linear(Foraging.Level, 0, 100, 1f, 1.5f);
```

`Stepped` returns `below` (0 by default) under its starting level; a negative step shrinks
toward the limit. `Linear` returns its start value under `from` (or `below`, if given) and
holds its end value beyond `to`.

## Stat bonuses

A bonus changes one of the player's stats as the skill levels. Your mod decides the amount
at each level, usually with a ramp:

```csharp
// Up to 150 kg more carry weight by level 100.
Foraging.AddBonus(Stat.CarryWeight, BonusType.Flat, level => Ramp.Linear(level, 0, 100, 0f, 150f));

// Running costs 2% less stamina every 5 levels past 10, at most 40% less.
Foraging.AddBonus(Stat.RunStamina, BonusType.Percent, level => Ramp.Stepped(level, 10, 5, 0f, -2f, -40f));

// 10 more health once Gathering is unlocked.
Foraging.AddBonus(Stat.MaxHealth, BonusType.Flat, _ => 10f, Gathering);

// Bows only: up to 25% more damage and 30% faster draw.
Archery.AddBonus(Stat.Damage, BonusType.Percent, level => level * 0.25f).Weapon = Skills.SkillType.Bows;
Archery.AddBonus(Stat.DrawTime, BonusType.Percent, level => level * -0.3f).Weapon = Skills.SkillType.Bows;
```

The function gets the skill's `Level`, status effects included, and returns the amount: in
the stat's own units for `Flat`, or a percentage for `Percent` (10 is 10% more, -20 is 20%
less). Bonuses from every skill and mod add up, then apply as
`value × (1 + percent ÷ 100) + flat`, never below 0. Health, block power and the stagger
limit never go below 1, and a negative value, such as a stamina refund, keeps its sign.

These are real stats, not status effects. Mimir changes each value where the game computes
it, so food, equipment, skills and status effects apply as they do in the base game, and
it never writes the player's own values, so removing a bonus or uninstalling Mimir returns
them to normal. Max health, stamina and eitr follow when the game next updates food. Food
duration can shorten to a tenth at most.

| Group | Stats | Types |
|---|---|---|
| Vitals | `MaxHealth`, `MaxStamina` (without food), `StaminaRegen`, `EitrRegen`, `Armor`, `StaggerLimit`, `MaxAdrenaline` | Both |
| | `MaxEitr` (without food; the base is 0) | Flat |
| | `HealthRegen` (food healing), `FoodDuration`, `FallDamage` | Percent |
| Carrying and reach | `CarryWeight`, `PickupRange`, `InteractRange` | Both |
| Movement | `WalkSpeed`, `JogSpeed`, `RunSpeed`, `CrouchSpeed` (also encumbered), `SwimSpeed`, `JumpForce` | Both |
| Stamina costs | `RunStamina`, `SneakStamina`, `SwimStamina`, `JumpStamina`, `DodgeStamina`, `BlockStamina` (parries too), `BuildStamina` (build tool swings) | Both |
| Weapons | `AttackStamina`, `AttackEitr`, `AttackHealth` | Both |
| | `Damage`, `DrawTime` (bows), `ReloadTime` (crossbows) | Percent |
| Defense | `BlockPower`, `ParryBonus` (the parry multiplier; never takes parrying away) | Both |
| Stealth | `Visibility` (while sneaking), `Noise` | Percent |
| Rest | `Comfort` | Flat |
| | `RestedDuration` | Percent |

A type a stat doesn't take throws. `AttackEitr` and `AttackHealth` cover spells too.
Attack costs only change on attacks that have one, so a flat bonus never adds a cost.
Attack speed has no stat: the game has no value for it, only the speeds set in each
attack's animation.

**Limiting a weapon stat.** Weapon stats apply to every weapon unless the bonus says
otherwise. `Weapon` limits it to one weapon type, the skill the weapon trains (the item's
`m_shared.m_skillType`). `WeaponFilter` narrows it further with any test of the item:

```csharp
// Swords only.
Swordplay.AddBonus(Stat.AttackStamina, BonusType.Percent, level => level * -0.3f).Weapon = Skills.SkillType.Swords;

// Two-handed swords only.
Bonus heavy = Swordplay.AddBonus(Stat.Damage, BonusType.Percent, level => level * 0.2f);
heavy.Weapon = Skills.SkillType.Swords;
heavy.WeaponFilter = item => item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon;

// One weapon by its prefab.
bonus.WeaponFilter = item => item.m_dropPrefab && item.m_dropPrefab.name == "SwordIron";
```

A new weapon type is a new skill: register it, and set your weapon prefab's
`m_shared.m_skillType` to its `Type`. The game then raises that skill when the weapon hits
and scales its damage by it, and `Weapon` and `Events.Weapon(type)` treat it like any other.

A setting can switch a bonus off without removing it:

```csharp
Bonus carry = Foraging.AddBonus(Stat.CarryWeight, BonusType.Flat, level => level * 1.5f);
carry.Enabled = MyConfig.CarryBonus.Value;
MyConfig.CarryBonus.SettingChanged += (_, _) => carry.Enabled = MyConfig.CarryBonus.Value;
```

A bonus tied to an ability applies while `Has` holds for it; passing a null ability, such
as one `AddAbility` refused, throws rather than applying from level 0. `Current` gives the
amount applied right now, for a tooltip or readout; `mimir stats` lists each one, and the
Skill Abilities page shows each that gives something as a perk, under the stat's name or
the token set in `Name`. `RemoveBonus` takes one out. A function or filter that throws is
logged once and the bonus switched off until the game restarts.

## Perks for your own effects

An effect your mod applies in its own code can still be listed among the skill's perks on
the Skill Abilities page. `AddPerk` takes a name (a translation token), the amount at a
level, a unit shown after it, and optionally an ability it needs:

```csharp
Foraging.AddPerk("$mymod_perk_yield", level => Ramp.Stepped(level, 0, 10, 0f, 5f, 50f), "%");
```

It only displays; the effect stays your mod's to apply, so have the function read the same
values your code uses. It shows while its amount is not 0. `Enabled` hides it,
`RemovePerk` takes it out, and a function that throws is logged once and hidden until the
game restarts.

## Experience events

`Events` reports what the local player does, so a skill gives experience for the actions
it is about and its mod can spend its code on abilities:

```csharp
Events.Harvested += (player, crop) => Farming.Raise(player, 1f);
Events.Swords.Hit += (player, target, hit) => Swordplay.Raise(player, 1f);
Events.ElementalMagic.Attacked += (player, staff, secondary) => Pyromancy.Raise(player, 0.5f);
Events.Ran += (player, meters) => Athletics.Raise(player, meters * 0.01f);
Events.Killed += (player, kill) => Hunting.Raise(player, kill.Detailed ? kill.Level : 1f);
```

Every handler gets the player first. Each event fires once per action, as the game carries
it out, on the local player's game; a failed action raises nothing: a block that breaks, a
failed upgrade, a dodge without stamina, a piece another mod stopped from being placed. A
handler that throws is logged and the rest still run.

**One weapon type.** `Events.Swords`, `Knives`, `Clubs`, `Polearms`, `Spears`, `Axes`,
`Bows`, `Crossbows`, `Unarmed`, `Pickaxes`, `ElementalMagic` and `BloodMagic` each have:

| Event | Gives | Raised when |
|---|---|---|
| `Attacked` | weapon, secondary | A swing, shot or cast starts; each swing of a combo counts |
| `Hit` | creature, hit | It hits a creature or another player, before resistances and armor |
| `Killed` | `Kill` | The player lands the killing blow with it (needs the kill's details, below) |

`Events.Weapon(skillType)` gives the same for any weapon skill, a mod's included. The
general events below are raised as well.

| Group | Event | Gives | Raised when |
|---|---|---|---|
| Combat | `Attacked` | weapon, skill, secondary | Any attack starts, tools included |
| | `Cast` | weapon, school | An elemental or blood magic attack starts |
| | `DamageDealt` | creature, hit | The player hits a creature or another player |
| | `Killed` | `Kill` | A creature the player damaged dies |
| Defense | `Blocked` | hit | A block goes through, parries included |
| | `Parried` | hit | A parry goes through |
| | `Dodged` | | A dodge starts, stamina paid |
| | `DodgedHit` | | An attack passes through the player mid-dodge, as the game counts it; once per dodge |
| | `DamageTaken` | hit, health lost | The player loses health, to hits, burning, poison and the like |
| Movement | `Walked`, `Ran`, `Sneaked`, `Swam` | meters | Moving on foot, running, sneaking, swimming; about once a second |
| | `Sailed`, `Rode` | meters | Steering a ship, riding a mount; about once a second |
| | `Jumped` | | The player jumps, by input or an attack's leap; launches by traps or catapults don't count |
| | `Fell` | height | The player lands from over 4 m, where falls start to hurt, even if an effect prevents the damage |
| Gathering | `Picked` | pickable | Anything is picked; also raises one of the two below |
| | `Harvested` | pickable | A crop is picked, as the game tags crops for its harvest count |
| | `Foraged` | pickable | Anything else is picked: berries, mushrooms, flowers and the like |
| | `PickedUp` | item, amount | An item is picked up off the ground |
| | `Planted` | plant | A seed or sapling is planted; also raises `Placed` |
| | `Chopped` | object, hit | A tree or log is hit, damaging or not; also raises `ObjectHit` |
| | `Mined` | object, hit | A rock or deposit is hit, or anything with pickaxe damage, damaging or not; also raises `ObjectHit` |
| | `ObjectHit` | object, hit | Any tree, log, rock, building piece or other object is hit |
| | `Fished` | fish | A fish is reeled in; the game's fish console command counts too |
| | `HoneyCollected`, `SapCollected` | station, amount | Honey or sap is taken |
| Animals | `Tamed` | creature | A creature finishes taming near the player (below) |
| | `Petted`, `Commanded` | creature | A tamed creature is petted, or told to follow or stay |
| Crafting | `Crafted` | recipe, item, amount | An item is crafted |
| | `Upgraded` | recipe, item | An item is upgraded |
| | `ItemRepaired` | item | A worn item is repaired at a station |
| | `Cooked` | station, item prefab name, burnt | Food is taken off a cooking station |
| | `Refined`, `Fueled` | station, (item) | An item or fuel goes into a smelter, kiln, windmill, spinning wheel or similar |
| | `Fermented`, `Tapped` | fermenter, (item) | A fermenter is filled or tapped |
| | `Ate`, `Consumed` | item | Food is eaten; anything consumed, meads included |
| Building | `Placed` | piece | A piece is built with a build tool |
| | `Removed` | piece | A piece is taken down; read it, don't keep it |
| | `Repaired` | piece | A damaged piece is repaired |
| Other | `LeveledUp` | skill type, level | Any skill gains a level through experience |
| | `Explored` | pixels | New map is revealed |
| | `Slept` | | The player wakes after sleeping through the night |
| | `Bought`, `Sold` | item, coins | Trading with a merchant |
| | `SkillRaised` | skill type, amount | Any skill is raised, with the amount after status effects and Mimir's scaling; even when nothing is gained |

**Kills.** The game tells every player who damaged a creature about its death, so `Killed`
works whoever owned it, with its name, whether it was a boss, how it was hurt (`Weapon`),
how many players took part and whether cheats were used. Its prefab, level, maximum
health, the skill of the killing blow and whether this player landed it come from Mimir
on the game that owned it: `Detailed` is false, and those are empty, when that game,
another player's or a dedicated server's, doesn't run Mimir. The weapon types' `Killed`
needs those details.

**Taming** finishes on the game that owns the creature, which credits the closest player
within 30 m, as the game's own message does; it needs Mimir on that game. Feeding and
breeding have no event: the game never records which player was responsible.

Mimir doesn't scale or limit these; a mod that gives experience for picking items up, say,
decides for itself whether items the player dropped should count.

## Giving experience

```csharp
Foraging.Raise(player, 1f);
```

This goes through the game's own path, so status effects, the world's skill gain rate and
the skill's increase step all apply. Calling `player.RaiseSkill(Foraging.Type, 1f)` does the
same; Mimir applies the pace, `GainRate` and modifiers either way.

## Gain modifiers

Scale gains from any source, on the local player's game. Return 1 for no change; several
modifiers multiply:

```csharp
// This skill only: 10% faster while rested.
Foraging.AddGainModifier(player => player.GetSEMan().HaveStatusEffect(SEMan.s_statusEffectRested) ? 1.1f : 1f);

// Every skill, the game's and other mods': Run 20% faster.
MimirAPI.AddGainModifier((player, type) => type == Skills.SkillType.Run ? 1.2f : 1f);
```

Keep the delegate to remove it later with `RemoveGainModifier`.

## Diminishing returns

`Diminishing` makes repeating one action on one thing pay less, so a skill can't be farmed
on a training dummy or by placing and removing one piece:

```csharp
// Each repeat on the same target within a minute pays half the last; a minute's rest resets it.
static readonly Diminishing Dummies = new(window: 60f, falloff: 0.5f);
Events.Swords.Hit += (player, target, hit) => Swordplay.Raise(player, 1f * Dummies.Next(target));

// By place: pieces within the same 2 m cell count as the same spot, down to a tenth.
static readonly Diminishing Spots = new(window: 300f, falloff: 0.8f, floor: 0.1f, cell: 2f);
Events.Placed += (player, piece) => Building.Raise(player, 1f * Spots.Next(piece.transform.position));
```

`Next` returns the share to pay, from 1 down to `floor`, and counts the use. Every use
restarts the window, so a key pays in full again only after a full window untouched. With
`falloff: 0`, a key pays once, then nothing until it is left alone that long. `Clear`
forgets every use.

## Level curves

`LevelCurve` sets the experience each level needs, given the whole level; the game's curve
is used when it is null, and for a result that is not a positive number. `Curves` has the
usual ones:

```csharp
Foraging.LevelCurve = Curves.Scaled(2f);              // every level twice the game's cost
Foraging.LevelCurve = Curves.Linear(10f, 2f);         // 10, 12, 14, ...
Foraging.LevelCurve = Curves.Flat(25f);               // every level the same
Foraging.LevelCurve = level => level < 20 ? 5f : Curves.Game(level);
```

It applies wherever the game asks, so progress bars and level-ups follow it, and so does
`EarlyLevelCost`. It can be set on a tracked game skill too.

## Death and fortifying

`DeathLoss` scales the game's death penalty for the skill: 1 as the game, 0.5 half,
0 none, which also keeps the progress toward the next level. It is unused while the
skill fortifies.

A fortifying skill instead keeps a fortified level that trails its own in the background.
It gets a share of every gain, `Rate Per Level` (0.1) more for each level the skill is
above it up to `Max Rate` (0.8), so it never passes the skill, and on death the skill falls
back to it. The skill's own gains are multiplied by `Active Rate` (1.5) meanwhile; the
fortified level's share is of the gain before that boost. The fortified level starts where
a death would leave the skill in the game, so fortifying begins costing no more than the
game. It never trails the skill by more than `Max Gap` levels (12) and never passes
`Max Fortified Level` (100); one pulled up to a limit starts its next level from nothing.
A world with a lighter death penalty softens the fall to match, and one without any costs
nothing. A skill that stops fortifying forgets its fortified level, which starts over the
same way if it fortifies again.

```csharp
Foraging.Fortify.Enabled = true;
Foraging.Fortify.MaxRate = 0.5f;   // optional; null follows Mimir's settings
Foraging.Fortify.MaxGap = 8;        // optional
```

`FortifySettings` has `Enabled`, `RatePerLevel`, `MaxRate`, `ActiveRate`, `MaxGap` and
`MaxLevel`; each left null follows the matching Mimir setting, which a server sets. With
`Fortify` set to every skill, a skill fortifies unless its mod sets `Enabled` to false;
set to none, nothing fortifies. `Active` says whether the skill fortifies now, and
`Level` and `Progress` read the character's fortified level; `mimir stats` shows it.
Resetting a skill, by `resetskill` or otherwise, resets it, and a world that resets
skills on death clears it.

## The game's skills and other mods'

`Track` takes on a skill Mimir did not register, so it can have abilities:

```csharp
MimirSkill woodcutting = MimirAPI.Track(Skills.SkillType.WoodCutting);
woodcutting.AddAbility("timber", "$mymod_ability_timber", 40);
```

Tracking a skill again returns the same `MimirSkill`, so several mods can add abilities to
one skill. Its id and console name are the type's name, lowercased, unless `Track` is
given another console name.

## Reacting to unlocks and level-ups

```csharp
Foraging.Unlocked += ability => { /* refresh a hint, add a recipe */ };
MimirAPI.Unlocked += (skill, ability) => { /* every skill */ };

Foraging.LeveledUp += level => { /* level is the new level */ };
MimirAPI.LeveledUp += (skill, level) => { /* every skill Mimir keeps */ };

Ability next = Foraging.NextAbility;   // the next one to unlock, or null
```

Unlock events are raised for each ability as it unlocks, whatever the `Unlock Notices`
setting. Level-ups are raised whatever raised the level, the console or a mod included,
once with the new level, so a jump of several levels is one event. Loading a character
raises neither for what it already had, so check `Has` where a feature runs
rather than relying on the events alone. `Events.LeveledUp` covers every skill, the
game's included, when experience raises it.

## MimirSkill

| Member | Gives |
|---|---|
| `Type`, `Id`, `Key`, `Name` | The game's skill type, id, console name, display name token |
| `Level`, `Factor` | The level with status effects, and the same as 0 to 1 |
| `StoredLevel` | The level without status effects |
| `Progress`, `LevelCost` | Experience toward the next level, and what it needs |
| `Raise(player, amount)` | Gives experience through the game's own path |
| `SetLevel(level)` | Sets the level and clears progress; raising it unlocks what it passes, nothing is ever locked |
| `AddAbility(id, name, level, unlockText, description)`, `SetAbilityLevel(id, level)` | Adds an ability, or moves it |
| `Abilities`, `GetAbility(id)` | Abilities in level order, one by id or key |
| `Has(ability)` | Still part of the skill, unlocked and not disabled: whether the feature should run |
| `IsUnlocked(id)`, `UnlockLevel(id)` | Whether unlocked at all, and at what level |
| `RemoveAbility(id)` | Takes an ability out of the skill |
| `Unlock(id)`, `Lock(id)` | Sets or unsets the character's unlock |
| `Disable(id, source)`, `Enable(id, source)`, `IsDisabled(id)` | Switches an ability off, without touching the unlock |
| `NextAbility` | The next ability to unlock, or null |
| `GainRate` | How fast the skill levels, as a multiplier on every gain |
| `AddGainModifier`, `RemoveGainModifier` | Scaling this skill's gains |
| `LevelCurve` | The experience each level needs |
| `DeathLoss`, `Fortify` | The death penalty, and fortifying |
| `AddBonus(stat, type, amountAtLevel, requires)`, `RemoveBonus(bonus)`, `Bonuses` | Stat bonuses that grow with the skill; `requires` is an ability or its id |
| `AddPerk(name, amountAtLevel, unit, requires)`, `RemovePerk(perk)`, `Perks` | Display-only perks for effects the mod applies itself |
| `Unlocked`, `LeveledUp` | Raised for each ability as it unlocks, and when the level rises |

`Ability` has `Id` (skill id and its own), `Key` (its own part), `Name`, `UnlockText`,
`Description` and `Level`. `Bonus` has `Skill`, `Stat`, `Type`, `Requires` (an ability's
id, or null), `Weapon`, `WeaponFilter`, `Name`, `Enabled` and `Current`. `Perk` has
`Skill`, `Name`, `Unit`, `Requires`, `Enabled` and `Current`. `Kill` has `Name`,
`Boss`, `Weapon`, `Attackers`, `Cheated`, `Detailed`, `Prefab`, `Level`, `MaxHealth`,
`LastHit` and `Skill`.

## MimirAPI

| Member | Gives |
|---|---|
| `Version`, `Guid` | The API version, and `NullSignal.Mimir` |
| `Register(definition)` | Registers a new skill |
| `Track(type, key)` | Takes on an existing skill |
| `Find(name)`, `Find(type)` | A skill by id or console name, or by type |
| `All` | Every skill Mimir keeps |
| `Level(skill)` | The level by skill name |
| `Has(skill, id)`, `IsUnlocked(skill, id)`, `UnlockLevel(skill, id)`, `AbilityIds(skill)` | Abilities by skill name |
| `AddAbility`, `RemoveAbility`, `Unlock`, `Lock`, `Disable`, `Enable` | The same controls by skill and ability name |
| `AddGainModifier`, `RemoveGainModifier` | Scaling every skill's gains |
| `Unlocked`, `LeveledUp` | Raised for each ability and level-up of any skill Mimir keeps |

Alongside it, `Ramp` (`Stepped`, `Linear`) computes values that grow with a level,
`Curves` level curves, `Diminishing` diminishing returns, and `Events` reports what the
local player does, with `Events.Swords` and the like for one weapon type.

## Without a reference

The readers that take names use only system types, so reflection works:

```csharp
if (Chainloader.PluginInfos.TryGetValue("NullSignal.Mimir", out PluginInfo info))
{
    Type api = info.Instance.GetType().Assembly.GetType("Mimir.MimirAPI");
    MethodInfo isUnlocked = api?.GetMethod("IsUnlocked");
    bool edit = isUnlocked != null && (bool)isUnlocked.Invoke(null, new object[] { "building", "edit" });
}
```
