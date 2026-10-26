namespace Mimir;

/// <summary>A stat of the player that a <see cref="Bonus" /> raises or lowers. Some take only one <see cref="BonusType" />; adding the other throws.</summary>
public enum Stat
{
    /// <summary>Health without food, in points.</summary>
    MaxHealth,

    /// <summary>Stamina without food, in points.</summary>
    MaxStamina,

    /// <summary>Eitr without food, in points; flat only.</summary>
    MaxEitr,

    /// <summary>Base stamina regeneration, before the game's own multipliers.</summary>
    StaminaRegen,

    /// <summary>Base eitr regeneration, before the game's own multipliers.</summary>
    EitrRegen,

    /// <summary>Health food heals every 10 seconds; percent only.</summary>
    HealthRegen,

    /// <summary>How long food lasts; percent only.</summary>
    FoodDuration,

    /// <summary>Armor from equipment.</summary>
    Armor,

    /// <summary>Damage taken from falling; percent only.</summary>
    FallDamage,

    /// <summary>Damage the player takes before staggering.</summary>
    StaggerLimit,

    /// <summary>Adrenaline the player can build up.</summary>
    MaxAdrenaline,

    /// <summary>Carry weight, in kg.</summary>
    CarryWeight,

    /// <summary>Range items are picked up automatically from, in m.</summary>
    PickupRange,

    /// <summary>Range the player can interact from, in m, capped at 50 m by the game's hover ray.</summary>
    InteractRange,

    /// <summary>Walking speed, in m/s.</summary>
    WalkSpeed,

    /// <summary>Normal moving speed, in m/s.</summary>
    JogSpeed,

    /// <summary>Running speed, in m/s.</summary>
    RunSpeed,

    /// <summary>Sneaking and encumbered speed, in m/s.</summary>
    CrouchSpeed,

    /// <summary>Swimming speed, in m/s.</summary>
    SwimSpeed,

    /// <summary>Jump force.</summary>
    JumpForce,

    /// <summary>Stamina used per second running.</summary>
    RunStamina,

    /// <summary>Stamina used per second sneaking.</summary>
    SneakStamina,

    /// <summary>Stamina used per second swimming.</summary>
    SwimStamina,

    /// <summary>Stamina used per jump.</summary>
    JumpStamina,

    /// <summary>Stamina used per dodge.</summary>
    DodgeStamina,

    /// <summary>Stamina used per block and parry.</summary>
    BlockStamina,

    /// <summary>Stamina used per swing of a hammer, hoe, cultivator or other build tool.</summary>
    BuildStamina,

    /// <summary>Damage of the player's attacks, shots and spells; percent only.</summary>
    Damage,

    /// <summary>Stamina used per attack.</summary>
    AttackStamina,

    /// <summary>Eitr used per attack or spell.</summary>
    AttackEitr,

    /// <summary>Health used per attack or spell.</summary>
    AttackHealth,

    /// <summary>Time to fully draw a bow; percent only.</summary>
    DrawTime,

    /// <summary>Time to reload a crossbow; percent only.</summary>
    ReloadTime,

    /// <summary>Block power of the shield or weapon blocking.</summary>
    BlockPower,

    /// <summary>The parry multiplier on block power, only for items that can parry.</summary>
    ParryBonus,

    /// <summary>How visible the player is while sneaking; percent only.</summary>
    Visibility,

    /// <summary>How far the player's noise carries; percent only.</summary>
    Noise,

    /// <summary>Comfort level; flat only.</summary>
    Comfort,

    /// <summary>How long the Rested effect lasts; percent only.</summary>
    RestedDuration
}

/// <summary>How a <see cref="Bonus" /> amount applies to its stat.</summary>
public enum BonusType
{
    /// <summary>Added to the value, in the stat's units.</summary>
    Flat,

    /// <summary>A percentage of the value, added to it.</summary>
    Percent
}
