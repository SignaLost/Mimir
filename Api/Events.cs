using System;
using System.Collections.Generic;
using Mimir.Common;

namespace Mimir;

/// <summary>
///     Events for what the local player does, each raised once per completed action on the main thread.
///     A handler that throws is logged and the rest still run.
/// </summary>
public static class Events
{
    // ---- Combat ----

    /// <summary>An attack or tool use started, with the weapon, its skill and whether it is the secondary attack.</summary>
    public static event Action<Player, ItemDrop.ItemData, Skills.SkillType, bool> Attacked;

    /// <summary>A spell was cast with a staff or other elemental or blood magic weapon, with the weapon and its school.</summary>
    public static event Action<Player, ItemDrop.ItemData, Skills.SkillType> Cast;

    /// <summary>The player hit a creature or another player, before its resistances and armor. The hit's m_skill is the weapon's.</summary>
    public static event Action<Player, Character, HitData> DamageDealt;

    /// <summary>A creature the player damaged died. Raised for every player who damaged it.</summary>
    public static event Action<Player, Kill> Killed;

    // ---- Defense ----

    /// <summary>The player blocked a hit, parries included; a failed block does not count.</summary>
    public static event Action<Player, HitData> Blocked;

    /// <summary>The player parried a hit; raised after <see cref="Blocked" />.</summary>
    public static event Action<Player, HitData> Parried;

    /// <summary>The player dodged, with the stamina paid.</summary>
    public static event Action<Player> Dodged;

    /// <summary>An attack passed through the player while dodging; at most once per dodge.</summary>
    public static event Action<Player> DodgedHit;

    /// <summary>The player lost health to a hit or a damaging effect, with the health lost.</summary>
    public static event Action<Player, HitData, float> DamageTaken;

    // ---- Movement; distances are reported about once a second ----

    /// <summary>Meters moved on foot, not running or sneaking.</summary>
    public static event Action<Player, float> Walked;

    /// <summary>Meters run.</summary>
    public static event Action<Player, float> Ran;

    /// <summary>Meters moved while sneaking.</summary>
    public static event Action<Player, float> Sneaked;

    /// <summary>Meters swum.</summary>
    public static event Action<Player, float> Swam;

    /// <summary>Meters sailed while steering a ship.</summary>
    public static event Action<Player, float> Sailed;

    /// <summary>Meters ridden on a mount.</summary>
    public static event Action<Player, float> Rode;

    /// <summary>The player jumped by input or an attack's leap; launches by traps, catapults and grappling do not count.</summary>
    public static event Action<Player> Jumped;

    /// <summary>The player landed from over 4 m, with the height in meters; raised even when a status effect prevents the damage.</summary>
    public static event Action<Player, float> Fell;

    // ---- Gathering ----

    /// <summary>Something was picked; also raises <see cref="Harvested" /> or <see cref="Foraged" />.</summary>
    public static event Action<Player, Pickable> Picked;

    /// <summary>A crop was harvested.</summary>
    public static event Action<Player, Pickable> Harvested;

    /// <summary>Something other than a crop was picked.</summary>
    public static event Action<Player, Pickable> Foraged;

    /// <summary>An item was picked up off the ground, with the amount.</summary>
    public static event Action<Player, ItemDrop.ItemData, int> PickedUp;

    /// <summary>A seed or sapling was planted. Also raises <see cref="Placed" />.</summary>
    public static event Action<Player, Plant> Planted;

    /// <summary>The player hit a tree or log, damaging or not; also raises <see cref="ObjectHit" />.</summary>
    public static event Action<Player, IDestructible, HitData> Chopped;

    /// <summary>The player hit a rock, ore deposit or anything else with pickaxe damage, damaging or not; also raises <see cref="ObjectHit" />.</summary>
    public static event Action<Player, IDestructible, HitData> Mined;

    /// <summary>The player hit a tree, log, rock, building piece or other destructible object.</summary>
    public static event Action<Player, IDestructible, HitData> ObjectHit;

    /// <summary>A fish was reeled in; the game's fish console command counts too.</summary>
    public static event Action<Player, Fish> Fished;

    /// <summary>Honey was taken from a beehive, with the amount.</summary>
    public static event Action<Player, Beehive, int> HoneyCollected;

    /// <summary>Sap was taken from a sap collector, with the amount.</summary>
    public static event Action<Player, SapCollector, int> SapCollected;

    // ---- Animals ----

    /// <summary>A creature finished taming with this player the closest within 30 m. Needs Mimir on the game that owns the creature.</summary>
    public static event Action<Player, Character> Tamed;

    /// <summary>The player petted a tamed creature.</summary>
    public static event Action<Player, Character> Petted;

    /// <summary>The player told a tamed creature to follow or stay.</summary>
    public static event Action<Player, Character> Commanded;

    // ---- Crafting and food ----

    /// <summary>An item was crafted, with the amount made.</summary>
    public static event Action<Player, Recipe, ItemDrop.ItemData, int> Crafted;

    /// <summary>An item was upgraded to a higher quality; a failed upgrade does not count.</summary>
    public static event Action<Player, Recipe, ItemDrop.ItemData> Upgraded;

    /// <summary>A worn item was repaired at a station.</summary>
    public static event Action<Player, ItemDrop.ItemData> ItemRepaired;

    /// <summary>Food was taken off a cooking station, with the item's prefab name and whether it was burnt.</summary>
    public static event Action<Player, CookingStation, string, bool> Cooked;

    /// <summary>An item was put in a smelter, furnace, kiln, windmill, spinning wheel or other refining station.</summary>
    public static event Action<Player, Smelter, ItemDrop.ItemData> Refined;

    /// <summary>Fuel was put in a smelter, furnace or other refining station.</summary>
    public static event Action<Player, Smelter> Fueled;

    /// <summary>An item was put in a fermenter.</summary>
    public static event Action<Player, Fermenter, ItemDrop.ItemData> Fermented;

    /// <summary>A finished fermenter was tapped.</summary>
    public static event Action<Player, Fermenter> Tapped;

    /// <summary>Food was eaten; also raises <see cref="Consumed" />.</summary>
    public static event Action<Player, ItemDrop.ItemData> Ate;

    /// <summary>Food, a mead or another consumable was used.</summary>
    public static event Action<Player, ItemDrop.ItemData> Consumed;

    // ---- Building ----

    /// <summary>A piece was placed with a build tool, with the piece just placed.</summary>
    public static event Action<Player, Piece> Placed;

    /// <summary>A piece was taken down with the hammer. The piece is being destroyed and must not be kept.</summary>
    public static event Action<Player, Piece> Removed;

    /// <summary>A damaged piece was repaired.</summary>
    public static event Action<Player, Piece> Repaired;

    // ---- Other ----

    /// <summary>New map was revealed, in map pixels.</summary>
    public static event Action<Player, int> Explored;

    /// <summary>The player woke up after sleeping through the night.</summary>
    public static event Action<Player> Slept;

    /// <summary>An item was bought from a trader, with the coins paid.</summary>
    public static event Action<Player, Trader.TradeItem, int> Bought;

    /// <summary>An item was sold to a trader, with the coins received.</summary>
    public static event Action<Player, ItemDrop.ItemData, int> Sold;

    /// <summary>Any skill, the game's or a mod's, gained a level through experience, with the new level.</summary>
    public static event Action<Player, Skills.SkillType, int> LeveledUp;

    /// <summary>
    ///     Any skill was raised, with the amount after status effects and Mimir's modifiers but before the increase step and world rate.
    ///     Raised even when nothing is gained.
    /// </summary>
    public static event Action<Player, Skills.SkillType, float> SkillRaised;

    // ---- One weapon type ----

    private static readonly Dictionary<Skills.SkillType, WeaponEvents> _weapons = new();

    /// <summary>The events of one weapon type, the game's or a mod's weapon skill.</summary>
    public static WeaponEvents Weapon(Skills.SkillType skill)
    {
        if (!_weapons.TryGetValue(skill, out WeaponEvents events))
        {
            events = new WeaponEvents(skill);
            _weapons[skill] = events;
        }

        return events;
    }

    public static WeaponEvents Swords => Weapon(Skills.SkillType.Swords);
    public static WeaponEvents Knives => Weapon(Skills.SkillType.Knives);
    public static WeaponEvents Clubs => Weapon(Skills.SkillType.Clubs);
    public static WeaponEvents Polearms => Weapon(Skills.SkillType.Polearms);
    public static WeaponEvents Spears => Weapon(Skills.SkillType.Spears);
    public static WeaponEvents Axes => Weapon(Skills.SkillType.Axes);
    public static WeaponEvents Bows => Weapon(Skills.SkillType.Bows);
    public static WeaponEvents Crossbows => Weapon(Skills.SkillType.Crossbows);
    public static WeaponEvents Unarmed => Weapon(Skills.SkillType.Unarmed);
    public static WeaponEvents Pickaxes => Weapon(Skills.SkillType.Pickaxes);
    public static WeaponEvents ElementalMagic => Weapon(Skills.SkillType.ElementalMagic);
    public static WeaponEvents BloodMagic => Weapon(Skills.SkillType.BloodMagic);

    /// <summary>The weapon type's events, or null when none were ever requested.</summary>
    internal static WeaponEvents Existing(Skills.SkillType skill)
    {
        return _weapons.TryGetValue(skill, out WeaponEvents events) ? events : null;
    }

    // ---- Raising ----

    private const string Name = "Events.";

    /// <summary>Whether any mod listens for a distance event.</summary>
    internal static bool WantsMovement => Walked != null || Ran != null || Sneaked != null || Swam != null || Sailed != null || Rode != null;

    internal static void RaiseAttacked(Player player, ItemDrop.ItemData weapon, bool secondary)
    {
        Skills.SkillType skill = weapon.m_shared.m_skillType;
        Handlers.Invoke(Attacked, Name + nameof(Attacked), player, weapon, skill, secondary);

        if (skill == Skills.SkillType.ElementalMagic || skill == Skills.SkillType.BloodMagic)
        {
            Handlers.Invoke(Cast, Name + nameof(Cast), player, weapon, skill);
        }

        Existing(skill)?.RaiseAttacked(player, weapon, secondary);
    }

    internal static void RaiseDamageDealt(Player player, Character target, HitData hit)
    {
        Handlers.Invoke(DamageDealt, Name + nameof(DamageDealt), player, target, hit);
        Existing(hit.m_skill)?.RaiseHit(player, target, hit);
    }

    internal static void RaiseKilled(Player player, Kill kill)
    {
        Handlers.Invoke(Killed, Name + nameof(Killed), player, kill);

        if (kill.LastHit && kill.Skill != Skills.SkillType.None)
        {
            Existing(kill.Skill)?.RaiseKilled(player, kill);
        }
    }

    internal static void RaiseBlocked(Player player, HitData hit, bool parried)
    {
        Handlers.Invoke(Blocked, Name + nameof(Blocked), player, hit);

        if (parried)
        {
            Handlers.Invoke(Parried, Name + nameof(Parried), player, hit);
        }
    }

    internal static void RaiseDodged(Player player) => Handlers.Invoke(Dodged, Name + nameof(Dodged), player);
    internal static void RaiseDodgedHit(Player player) => Handlers.Invoke(DodgedHit, Name + nameof(DodgedHit), player);
    internal static void RaiseDamageTaken(Player player, HitData hit, float damage) =>
        Handlers.Invoke(DamageTaken, Name + nameof(DamageTaken), player, hit, damage);

    internal static void RaiseWalked(Player player, float meters) => Handlers.Invoke(Walked, Name + nameof(Walked), player, meters);
    internal static void RaiseRan(Player player, float meters) => Handlers.Invoke(Ran, Name + nameof(Ran), player, meters);
    internal static void RaiseSneaked(Player player, float meters) => Handlers.Invoke(Sneaked, Name + nameof(Sneaked), player, meters);
    internal static void RaiseSwam(Player player, float meters) => Handlers.Invoke(Swam, Name + nameof(Swam), player, meters);
    internal static void RaiseSailed(Player player, float meters) => Handlers.Invoke(Sailed, Name + nameof(Sailed), player, meters);
    internal static void RaiseRode(Player player, float meters) => Handlers.Invoke(Rode, Name + nameof(Rode), player, meters);
    internal static void RaiseJumped(Player player) => Handlers.Invoke(Jumped, Name + nameof(Jumped), player);
    internal static void RaiseFell(Player player, float height) => Handlers.Invoke(Fell, Name + nameof(Fell), player, height);

    internal static void RaisePicked(Player player, Pickable pickable)
    {
        Handlers.Invoke(Picked, Name + nameof(Picked), player, pickable);

        if (pickable.m_harvestStat == PlayerStatType.HarvestCrop)
        {
            Handlers.Invoke(Harvested, Name + nameof(Harvested), player, pickable);
        }
        else
        {
            Handlers.Invoke(Foraged, Name + nameof(Foraged), player, pickable);
        }
    }

    internal static void RaisePickedUp(Player player, ItemDrop.ItemData item, int amount) =>
        Handlers.Invoke(PickedUp, Name + nameof(PickedUp), player, item, amount);
    internal static void RaiseChopped(Player player, IDestructible target, HitData hit) =>
        Handlers.Invoke(Chopped, Name + nameof(Chopped), player, target, hit);
    internal static void RaiseMined(Player player, IDestructible target, HitData hit) =>
        Handlers.Invoke(Mined, Name + nameof(Mined), player, target, hit);
    internal static void RaiseObjectHit(Player player, IDestructible target, HitData hit) =>
        Handlers.Invoke(ObjectHit, Name + nameof(ObjectHit), player, target, hit);
    internal static void RaiseFished(Player player, Fish fish) => Handlers.Invoke(Fished, Name + nameof(Fished), player, fish);
    internal static void RaiseHoneyCollected(Player player, Beehive hive, int amount) =>
        Handlers.Invoke(HoneyCollected, Name + nameof(HoneyCollected), player, hive, amount);
    internal static void RaiseSapCollected(Player player, SapCollector collector, int amount) =>
        Handlers.Invoke(SapCollected, Name + nameof(SapCollected), player, collector, amount);

    internal static void RaiseTamed(Player player, Character creature) => Handlers.Invoke(Tamed, Name + nameof(Tamed), player, creature);
    internal static void RaisePetted(Player player, Character creature) => Handlers.Invoke(Petted, Name + nameof(Petted), player, creature);
    internal static void RaiseCommanded(Player player, Character creature) =>
        Handlers.Invoke(Commanded, Name + nameof(Commanded), player, creature);

    internal static void RaiseCrafted(Player player, Recipe recipe, ItemDrop.ItemData item, int amount) =>
        Handlers.Invoke(Crafted, Name + nameof(Crafted), player, recipe, item, amount);
    internal static void RaiseUpgraded(Player player, Recipe recipe, ItemDrop.ItemData item) =>
        Handlers.Invoke(Upgraded, Name + nameof(Upgraded), player, recipe, item);
    internal static void RaiseItemRepaired(Player player, ItemDrop.ItemData item) =>
        Handlers.Invoke(ItemRepaired, Name + nameof(ItemRepaired), player, item);
    internal static void RaiseCooked(Player player, CookingStation station, string item, bool burnt) =>
        Handlers.Invoke(Cooked, Name + nameof(Cooked), player, station, item, burnt);
    internal static void RaiseRefined(Player player, Smelter smelter, ItemDrop.ItemData item) =>
        Handlers.Invoke(Refined, Name + nameof(Refined), player, smelter, item);
    internal static void RaiseFueled(Player player, Smelter smelter) => Handlers.Invoke(Fueled, Name + nameof(Fueled), player, smelter);
    internal static void RaiseFermented(Player player, Fermenter fermenter, ItemDrop.ItemData item) =>
        Handlers.Invoke(Fermented, Name + nameof(Fermented), player, fermenter, item);
    internal static void RaiseTapped(Player player, Fermenter fermenter) => Handlers.Invoke(Tapped, Name + nameof(Tapped), player, fermenter);

    internal static void RaiseConsumed(Player player, ItemDrop.ItemData item)
    {
        if (item.m_shared.m_food > 0f)
        {
            Handlers.Invoke(Ate, Name + nameof(Ate), player, item);
        }

        Handlers.Invoke(Consumed, Name + nameof(Consumed), player, item);
    }

    internal static void RaisePlaced(Player player, Piece piece)
    {
        Handlers.Invoke(Placed, Name + nameof(Placed), player, piece);
        Plant plant = piece.GetComponent<Plant>();

        if (plant)
        {
            Handlers.Invoke(Planted, Name + nameof(Planted), player, plant);
        }
    }

    internal static void RaiseRemoved(Player player, Piece piece) => Handlers.Invoke(Removed, Name + nameof(Removed), player, piece);
    internal static void RaiseRepaired(Player player, Piece piece) => Handlers.Invoke(Repaired, Name + nameof(Repaired), player, piece);

    internal static void RaiseExplored(Player player, int pixels) => Handlers.Invoke(Explored, Name + nameof(Explored), player, pixels);
    internal static void RaiseSlept(Player player) => Handlers.Invoke(Slept, Name + nameof(Slept), player);
    internal static void RaiseBought(Player player, Trader.TradeItem item, int coins) =>
        Handlers.Invoke(Bought, Name + nameof(Bought), player, item, coins);
    internal static void RaiseSold(Player player, ItemDrop.ItemData item, int coins) =>
        Handlers.Invoke(Sold, Name + nameof(Sold), player, item, coins);

    internal static void RaiseLeveledUp(Player player, Skills.SkillType type, int level) =>
        Handlers.Invoke(LeveledUp, Name + nameof(LeveledUp), player, type, level);

    internal static void RaiseSkillRaised(Player player, Skills.SkillType type, float amount) =>
        Handlers.Invoke(SkillRaised, Name + nameof(SkillRaised), player, type, amount);
}
