using System;
using System.Collections.Generic;
using Mimir.Common;
using Mimir.Core;

namespace Mimir;

/// <summary>
///     Events for what the local player does, each raised once per completed action on the main thread.
///     A handler that throws is logged and the rest still run. The game code behind an event is patched the first
///     time a mod subscribes to it, so events nothing listens for cost nothing.
/// </summary>
public static class Events
{
    private static Action<Player, ItemDrop.ItemData, Skills.SkillType, bool> _attacked;

    /// <summary>An attack or tool use started, with the weapon, its skill and whether it is the secondary attack.</summary>
    public static event Action<Player, ItemDrop.ItemData, Skills.SkillType, bool> Attacked
    {
        add => _attacked = Wanting(Need.Attacks, _attacked, value);
        remove => _attacked -= value;
    }

    private static Action<Player, ItemDrop.ItemData, Skills.SkillType> _cast;

    /// <summary>A spell was cast with a staff or other elemental or blood magic weapon, with the weapon and its school.</summary>
    public static event Action<Player, ItemDrop.ItemData, Skills.SkillType> Cast
    {
        add => _cast = Wanting(Need.Attacks, _cast, value);
        remove => _cast -= value;
    }

    private static Action<Player, Character, HitData> _damageDealt;

    /// <summary>The player hit a creature or another player, before its resistances and armor. The hit's m_skill is the weapon's.</summary>
    public static event Action<Player, Character, HitData> DamageDealt
    {
        add => _damageDealt = Wanting(Need.Hits, _damageDealt, value);
        remove => _damageDealt -= value;
    }

    private static Action<Player, Kill> _killed;

    /// <summary>A creature the player damaged died. Raised for every player who damaged it.</summary>
    public static event Action<Player, Kill> Killed
    {
        add => _killed += value;
        remove => _killed -= value;
    }

    private static Action<Player, HitData> _blocked;

    /// <summary>The player blocked a hit, parries included; a failed block does not count.</summary>
    public static event Action<Player, HitData> Blocked
    {
        add => _blocked = Wanting(Need.Blocking, _blocked, value);
        remove => _blocked -= value;
    }

    private static Action<Player, HitData> _parried;

    /// <summary>The player parried a hit; raised after <see cref="Blocked" />.</summary>
    public static event Action<Player, HitData> Parried
    {
        add => _parried = Wanting(Need.Blocking, _parried, value);
        remove => _parried -= value;
    }

    private static Action<Player> _dodged;

    /// <summary>The player dodged, with the stamina paid.</summary>
    public static event Action<Player> Dodged
    {
        add => _dodged = Wanting(Need.Dodging, _dodged, value);
        remove => _dodged -= value;
    }

    private static Action<Player> _dodgedHit;

    /// <summary>An attack passed through the player while dodging; at most once per dodge.</summary>
    public static event Action<Player> DodgedHit
    {
        add => _dodgedHit = Wanting(Need.Dodging, _dodgedHit, value);
        remove => _dodgedHit -= value;
    }

    private static Action<Player, HitData, float> _damageTaken;

    /// <summary>The player lost health to a hit or a damaging effect, with the health lost.</summary>
    public static event Action<Player, HitData, float> DamageTaken
    {
        add => _damageTaken = Wanting(Need.Hurt, _damageTaken, value);
        remove => _damageTaken -= value;
    }

    private static Action<Player, float> _walked;

    /// <summary>Meters moved on foot, not running or sneaking.</summary>
    public static event Action<Player, float> Walked
    {
        add => _walked += value;
        remove => _walked -= value;
    }

    private static Action<Player, float> _ran;

    /// <summary>Meters run.</summary>
    public static event Action<Player, float> Ran
    {
        add => _ran += value;
        remove => _ran -= value;
    }

    private static Action<Player, float> _sneaked;

    /// <summary>Meters moved while sneaking.</summary>
    public static event Action<Player, float> Sneaked
    {
        add => _sneaked += value;
        remove => _sneaked -= value;
    }

    private static Action<Player, float> _swam;

    /// <summary>Meters swum.</summary>
    public static event Action<Player, float> Swam
    {
        add => _swam += value;
        remove => _swam -= value;
    }

    private static Action<Player, float> _sailed;

    /// <summary>Meters sailed while steering a ship.</summary>
    public static event Action<Player, float> Sailed
    {
        add => _sailed += value;
        remove => _sailed -= value;
    }

    private static Action<Player, float> _rode;

    /// <summary>Meters ridden on a mount.</summary>
    public static event Action<Player, float> Rode
    {
        add => _rode += value;
        remove => _rode -= value;
    }

    private static Action<Player> _jumped;

    /// <summary>The player jumped by input or an attack's leap; launches by traps, catapults and grappling do not count.</summary>
    public static event Action<Player> Jumped
    {
        add => _jumped = Wanting(Need.Jumping, _jumped, value);
        remove => _jumped -= value;
    }

    private static Action<Player, float> _fell;

    /// <summary>The player landed from over 4 m, with the height in meters; raised even when a status effect prevents the damage.</summary>
    public static event Action<Player, float> Fell
    {
        add => _fell = Wanting(Need.Falling, _fell, value);
        remove => _fell -= value;
    }

    private static Action<Player, Pickable> _picked;

    /// <summary>Something was picked; also raises <see cref="Harvested" /> or <see cref="Foraged" />.</summary>
    public static event Action<Player, Pickable> Picked
    {
        add => _picked = Wanting(Need.Picking, _picked, value);
        remove => _picked -= value;
    }

    private static Action<Player, Pickable> _harvested;

    /// <summary>A crop was harvested.</summary>
    public static event Action<Player, Pickable> Harvested
    {
        add => _harvested = Wanting(Need.Picking, _harvested, value);
        remove => _harvested -= value;
    }

    private static Action<Player, Pickable> _foraged;

    /// <summary>Something other than a crop was picked.</summary>
    public static event Action<Player, Pickable> Foraged
    {
        add => _foraged = Wanting(Need.Picking, _foraged, value);
        remove => _foraged -= value;
    }

    private static Action<Player, ItemDrop.ItemData, int> _pickedUp;

    /// <summary>An item was picked up off the ground, with the amount.</summary>
    public static event Action<Player, ItemDrop.ItemData, int> PickedUp
    {
        add => _pickedUp = Wanting(Need.PickingUp, _pickedUp, value);
        remove => _pickedUp -= value;
    }

    private static Action<Player, Plant> _planted;

    /// <summary>A seed or sapling was planted. Also raises <see cref="Placed" />.</summary>
    public static event Action<Player, Plant> Planted
    {
        add => _planted = Wanting(Need.Placing, _planted, value);
        remove => _planted -= value;
    }

    private static Action<Player, IDestructible, HitData> _chopped;

    /// <summary>The player hit a tree or log, damaging or not; also raises <see cref="ObjectHit" />.</summary>
    public static event Action<Player, IDestructible, HitData> Chopped
    {
        add => _chopped = Wanting(Need.ObjectHits, _chopped, value);
        remove => _chopped -= value;
    }

    private static Action<Player, IDestructible, HitData> _mined;

    /// <summary>The player hit a rock, ore deposit or anything else with pickaxe damage, damaging or not; also raises <see cref="ObjectHit" />.</summary>
    public static event Action<Player, IDestructible, HitData> Mined
    {
        add => _mined = Wanting(Need.ObjectHits, _mined, value);
        remove => _mined -= value;
    }

    private static Action<Player, IDestructible, HitData> _objectHit;

    /// <summary>The player hit a tree, log, rock, building piece or other destructible object.</summary>
    public static event Action<Player, IDestructible, HitData> ObjectHit
    {
        add => _objectHit = Wanting(Need.ObjectHits, _objectHit, value);
        remove => _objectHit -= value;
    }

    private static Action<Player, Fish> _fished;

    /// <summary>A fish was reeled in; the game's fish console command counts too.</summary>
    public static event Action<Player, Fish> Fished
    {
        add => _fished = Wanting(Need.Fishing, _fished, value);
        remove => _fished -= value;
    }

    private static Action<Player, Beehive, int> _honeyCollected;

    /// <summary>Honey was taken from a beehive, with the amount.</summary>
    public static event Action<Player, Beehive, int> HoneyCollected
    {
        add => _honeyCollected = Wanting(Need.Honey, _honeyCollected, value);
        remove => _honeyCollected -= value;
    }

    private static Action<Player, SapCollector, int> _sapCollected;

    /// <summary>Sap was taken from a sap collector, with the amount.</summary>
    public static event Action<Player, SapCollector, int> SapCollected
    {
        add => _sapCollected = Wanting(Need.Sap, _sapCollected, value);
        remove => _sapCollected -= value;
    }

    private static Action<Player, Character> _tamed;

    /// <summary>A creature finished taming with this player the closest within 30 m. Needs Mimir on the game that owns the creature.</summary>
    public static event Action<Player, Character> Tamed
    {
        add => _tamed += value;
        remove => _tamed -= value;
    }

    private static Action<Player, Character> _petted;

    /// <summary>The player petted a tamed creature.</summary>
    public static event Action<Player, Character> Petted
    {
        add => _petted = Wanting(Need.Animals, _petted, value);
        remove => _petted -= value;
    }

    private static Action<Player, Character> _commanded;

    /// <summary>The player told a tamed creature to follow or stay.</summary>
    public static event Action<Player, Character> Commanded
    {
        add => _commanded = Wanting(Need.Animals, _commanded, value);
        remove => _commanded -= value;
    }

    private static Action<Player, Recipe, ItemDrop.ItemData, int> _crafted;

    /// <summary>An item was crafted, with the amount made.</summary>
    public static event Action<Player, Recipe, ItemDrop.ItemData, int> Crafted
    {
        add => _crafted = Wanting(Need.Crafting, _crafted, value);
        remove => _crafted -= value;
    }

    private static Action<Player, Recipe, ItemDrop.ItemData> _upgraded;

    /// <summary>An item was upgraded to a higher quality; a failed upgrade does not count.</summary>
    public static event Action<Player, Recipe, ItemDrop.ItemData> Upgraded
    {
        add => _upgraded = Wanting(Need.Crafting, _upgraded, value);
        remove => _upgraded -= value;
    }

    private static Action<Player, ItemDrop.ItemData> _itemRepaired;

    /// <summary>A worn item was repaired at a station.</summary>
    public static event Action<Player, ItemDrop.ItemData> ItemRepaired
    {
        add => _itemRepaired = Wanting(Need.ItemRepairs, _itemRepaired, value);
        remove => _itemRepaired -= value;
    }

    private static Action<Player, CookingStation, string, bool> _cooked;

    /// <summary>Food was taken off a cooking station, with the item's prefab name and whether it was burnt.</summary>
    public static event Action<Player, CookingStation, string, bool> Cooked
    {
        add => _cooked = Wanting(Need.Cooking, _cooked, value);
        remove => _cooked -= value;
    }

    private static Action<Player, Smelter, ItemDrop.ItemData> _refined;

    /// <summary>An item was put in a smelter, furnace, kiln, windmill, spinning wheel or other refining station.</summary>
    public static event Action<Player, Smelter, ItemDrop.ItemData> Refined
    {
        add => _refined = Wanting(Need.Refining, _refined, value);
        remove => _refined -= value;
    }

    private static Action<Player, Smelter> _fueled;

    /// <summary>Fuel was put in a smelter, furnace or other refining station.</summary>
    public static event Action<Player, Smelter> Fueled
    {
        add => _fueled = Wanting(Need.Refining, _fueled, value);
        remove => _fueled -= value;
    }

    private static Action<Player, Fermenter, ItemDrop.ItemData> _fermented;

    /// <summary>An item was put in a fermenter.</summary>
    public static event Action<Player, Fermenter, ItemDrop.ItemData> Fermented
    {
        add => _fermented = Wanting(Need.Fermenting, _fermented, value);
        remove => _fermented -= value;
    }

    private static Action<Player, Fermenter> _tapped;

    /// <summary>A finished fermenter was tapped.</summary>
    public static event Action<Player, Fermenter> Tapped
    {
        add => _tapped = Wanting(Need.Fermenting, _tapped, value);
        remove => _tapped -= value;
    }

    private static Action<Player, ItemDrop.ItemData> _ate;

    /// <summary>Food was eaten; also raises <see cref="Consumed" />.</summary>
    public static event Action<Player, ItemDrop.ItemData> Ate
    {
        add => _ate = Wanting(Need.Eating, _ate, value);
        remove => _ate -= value;
    }

    private static Action<Player, ItemDrop.ItemData> _consumed;

    /// <summary>Food, a mead or another consumable was used.</summary>
    public static event Action<Player, ItemDrop.ItemData> Consumed
    {
        add => _consumed = Wanting(Need.Eating, _consumed, value);
        remove => _consumed -= value;
    }

    private static Action<Player, Piece> _placed;

    /// <summary>A piece was placed with a build tool, with the piece just placed.</summary>
    public static event Action<Player, Piece> Placed
    {
        add => _placed = Wanting(Need.Placing, _placed, value);
        remove => _placed -= value;
    }

    private static Action<Player, Piece> _removed;

    /// <summary>A piece was taken down with the hammer. The piece is being destroyed and must not be kept.</summary>
    public static event Action<Player, Piece> Removed
    {
        add => _removed = Wanting(Need.Removing, _removed, value);
        remove => _removed -= value;
    }

    private static Action<Player, Piece> _repaired;

    /// <summary>A damaged piece was repaired.</summary>
    public static event Action<Player, Piece> Repaired
    {
        add => _repaired = Wanting(Need.PieceRepairs, _repaired, value);
        remove => _repaired -= value;
    }

    private static Action<Player, int> _explored;

    /// <summary>New map was revealed, in map pixels.</summary>
    public static event Action<Player, int> Explored
    {
        add => _explored = Wanting(Need.Exploring, _explored, value);
        remove => _explored -= value;
    }

    private static Action<Player> _slept;

    /// <summary>The player woke up after sleeping through the night.</summary>
    public static event Action<Player> Slept
    {
        add => _slept = Wanting(Need.Sleeping, _slept, value);
        remove => _slept -= value;
    }

    private static Action<Player, Trader.TradeItem, int> _bought;

    /// <summary>An item was bought from a trader, with the coins paid.</summary>
    public static event Action<Player, Trader.TradeItem, int> Bought
    {
        add => _bought = Wanting(Need.Trading, _bought, value);
        remove => _bought -= value;
    }

    private static Action<Player, ItemDrop.ItemData, int> _sold;

    /// <summary>An item was sold to a trader, with the coins received.</summary>
    public static event Action<Player, ItemDrop.ItemData, int> Sold
    {
        add => _sold = Wanting(Need.Trading, _sold, value);
        remove => _sold -= value;
    }

    private static Action<Player, Skills.SkillType, int> _leveledUp;

    /// <summary>Any skill, the game's or a mod's, gained a level through experience, with the new level.</summary>
    public static event Action<Player, Skills.SkillType, int> LeveledUp
    {
        add => _leveledUp = Wanting(Need.LevelUps, _leveledUp, value);
        remove => _leveledUp -= value;
    }

    private static Action<Player, Skills.SkillType, float> _skillRaised;

    /// <summary>
    ///     Any skill was raised, with the amount after status effects and Mimir's modifiers but before the increase step and world rate.
    ///     Raised even when nothing is gained.
    /// </summary>
    public static event Action<Player, Skills.SkillType, float> SkillRaised
    {
        add => _skillRaised = Wanting(Need.SkillRaises, _skillRaised, value);
        remove => _skillRaised -= value;
    }

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

    private const string Name = "Events.";

    /// <summary>Adds a handler and has the patches the event needs applied.</summary>
    internal static T Wanting<T>(Need need, T handlers, T added) where T : Delegate
    {
        PatchSets.Want(need);
        return (T)Delegate.Combine(handlers, added);
    }

    /// <summary>Whether any mod listens for a distance event.</summary>
    internal static bool WantsMovement => _walked != null || _ran != null || _sneaked != null || _swam != null || _sailed != null || _rode != null;

    internal static void RaiseAttacked(Player player, ItemDrop.ItemData weapon, bool secondary)
    {
        Skills.SkillType skill = weapon.m_shared.m_skillType;
        Handlers.Invoke(_attacked, Name + nameof(Attacked), player, weapon, skill, secondary);

        if (skill == Skills.SkillType.ElementalMagic || skill == Skills.SkillType.BloodMagic)
        {
            Handlers.Invoke(_cast, Name + nameof(Cast), player, weapon, skill);
        }

        Existing(skill)?.RaiseAttacked(player, weapon, secondary);
    }

    internal static void RaiseDamageDealt(Player player, Character target, HitData hit)
    {
        Handlers.Invoke(_damageDealt, Name + nameof(DamageDealt), player, target, hit);
        Existing(hit.m_skill)?.RaiseHit(player, target, hit);
    }

    internal static void RaiseKilled(Player player, Kill kill)
    {
        Handlers.Invoke(_killed, Name + nameof(Killed), player, kill);

        if (kill.LastHit && kill.Skill != Skills.SkillType.None)
        {
            Existing(kill.Skill)?.RaiseKilled(player, kill);
        }
    }

    internal static void RaiseBlocked(Player player, HitData hit, bool parried)
    {
        Handlers.Invoke(_blocked, Name + nameof(Blocked), player, hit);

        if (parried)
        {
            Handlers.Invoke(_parried, Name + nameof(Parried), player, hit);
        }
    }

    internal static void RaiseDodged(Player player) => Handlers.Invoke(_dodged, Name + nameof(Dodged), player);
    internal static void RaiseDodgedHit(Player player) => Handlers.Invoke(_dodgedHit, Name + nameof(DodgedHit), player);
    internal static void RaiseDamageTaken(Player player, HitData hit, float damage) =>
        Handlers.Invoke(_damageTaken, Name + nameof(DamageTaken), player, hit, damage);

    internal static void RaiseWalked(Player player, float meters) => Handlers.Invoke(_walked, Name + nameof(Walked), player, meters);
    internal static void RaiseRan(Player player, float meters) => Handlers.Invoke(_ran, Name + nameof(Ran), player, meters);
    internal static void RaiseSneaked(Player player, float meters) => Handlers.Invoke(_sneaked, Name + nameof(Sneaked), player, meters);
    internal static void RaiseSwam(Player player, float meters) => Handlers.Invoke(_swam, Name + nameof(Swam), player, meters);
    internal static void RaiseSailed(Player player, float meters) => Handlers.Invoke(_sailed, Name + nameof(Sailed), player, meters);
    internal static void RaiseRode(Player player, float meters) => Handlers.Invoke(_rode, Name + nameof(Rode), player, meters);
    internal static void RaiseJumped(Player player) => Handlers.Invoke(_jumped, Name + nameof(Jumped), player);
    internal static void RaiseFell(Player player, float height) => Handlers.Invoke(_fell, Name + nameof(Fell), player, height);

    internal static void RaisePicked(Player player, Pickable pickable)
    {
        Handlers.Invoke(_picked, Name + nameof(Picked), player, pickable);

        if (pickable.m_harvestStat == PlayerStatType.HarvestCrop)
        {
            Handlers.Invoke(_harvested, Name + nameof(Harvested), player, pickable);
        }
        else
        {
            Handlers.Invoke(_foraged, Name + nameof(Foraged), player, pickable);
        }
    }

    internal static void RaisePickedUp(Player player, ItemDrop.ItemData item, int amount) =>
        Handlers.Invoke(_pickedUp, Name + nameof(PickedUp), player, item, amount);
    internal static void RaiseChopped(Player player, IDestructible target, HitData hit) =>
        Handlers.Invoke(_chopped, Name + nameof(Chopped), player, target, hit);
    internal static void RaiseMined(Player player, IDestructible target, HitData hit) =>
        Handlers.Invoke(_mined, Name + nameof(Mined), player, target, hit);
    internal static void RaiseObjectHit(Player player, IDestructible target, HitData hit) =>
        Handlers.Invoke(_objectHit, Name + nameof(ObjectHit), player, target, hit);
    internal static void RaiseFished(Player player, Fish fish) => Handlers.Invoke(_fished, Name + nameof(Fished), player, fish);
    internal static void RaiseHoneyCollected(Player player, Beehive hive, int amount) =>
        Handlers.Invoke(_honeyCollected, Name + nameof(HoneyCollected), player, hive, amount);
    internal static void RaiseSapCollected(Player player, SapCollector collector, int amount) =>
        Handlers.Invoke(_sapCollected, Name + nameof(SapCollected), player, collector, amount);

    internal static void RaiseTamed(Player player, Character creature) => Handlers.Invoke(_tamed, Name + nameof(Tamed), player, creature);
    internal static void RaisePetted(Player player, Character creature) => Handlers.Invoke(_petted, Name + nameof(Petted), player, creature);
    internal static void RaiseCommanded(Player player, Character creature) =>
        Handlers.Invoke(_commanded, Name + nameof(Commanded), player, creature);

    internal static void RaiseCrafted(Player player, Recipe recipe, ItemDrop.ItemData item, int amount) =>
        Handlers.Invoke(_crafted, Name + nameof(Crafted), player, recipe, item, amount);
    internal static void RaiseUpgraded(Player player, Recipe recipe, ItemDrop.ItemData item) =>
        Handlers.Invoke(_upgraded, Name + nameof(Upgraded), player, recipe, item);
    internal static void RaiseItemRepaired(Player player, ItemDrop.ItemData item) =>
        Handlers.Invoke(_itemRepaired, Name + nameof(ItemRepaired), player, item);
    internal static void RaiseCooked(Player player, CookingStation station, string item, bool burnt) =>
        Handlers.Invoke(_cooked, Name + nameof(Cooked), player, station, item, burnt);
    internal static void RaiseRefined(Player player, Smelter smelter, ItemDrop.ItemData item) =>
        Handlers.Invoke(_refined, Name + nameof(Refined), player, smelter, item);
    internal static void RaiseFueled(Player player, Smelter smelter) => Handlers.Invoke(_fueled, Name + nameof(Fueled), player, smelter);
    internal static void RaiseFermented(Player player, Fermenter fermenter, ItemDrop.ItemData item) =>
        Handlers.Invoke(_fermented, Name + nameof(Fermented), player, fermenter, item);
    internal static void RaiseTapped(Player player, Fermenter fermenter) => Handlers.Invoke(_tapped, Name + nameof(Tapped), player, fermenter);

    internal static void RaiseConsumed(Player player, ItemDrop.ItemData item)
    {
        if (item.m_shared.m_food > 0f)
        {
            Handlers.Invoke(_ate, Name + nameof(Ate), player, item);
        }

        Handlers.Invoke(_consumed, Name + nameof(Consumed), player, item);
    }

    internal static void RaisePlaced(Player player, Piece piece)
    {
        Handlers.Invoke(_placed, Name + nameof(Placed), player, piece);
        Plant plant = piece.GetComponent<Plant>();

        if (plant)
        {
            Handlers.Invoke(_planted, Name + nameof(Planted), player, plant);
        }
    }

    internal static void RaiseRemoved(Player player, Piece piece) => Handlers.Invoke(_removed, Name + nameof(Removed), player, piece);
    internal static void RaiseRepaired(Player player, Piece piece) => Handlers.Invoke(_repaired, Name + nameof(Repaired), player, piece);

    internal static void RaiseExplored(Player player, int pixels) => Handlers.Invoke(_explored, Name + nameof(Explored), player, pixels);
    internal static void RaiseSlept(Player player) => Handlers.Invoke(_slept, Name + nameof(Slept), player);
    internal static void RaiseBought(Player player, Trader.TradeItem item, int coins) =>
        Handlers.Invoke(_bought, Name + nameof(Bought), player, item, coins);
    internal static void RaiseSold(Player player, ItemDrop.ItemData item, int coins) =>
        Handlers.Invoke(_sold, Name + nameof(Sold), player, item, coins);

    internal static void RaiseLeveledUp(Player player, Skills.SkillType type, int level) =>
        Handlers.Invoke(_leveledUp, Name + nameof(LeveledUp), player, type, level);

    internal static void RaiseSkillRaised(Player player, Skills.SkillType type, float amount) =>
        Handlers.Invoke(_skillRaised, Name + nameof(SkillRaised), player, type, amount);
}
