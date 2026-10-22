using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Mimir.Common;
using Mimir.Core;
using UnityEngine;

namespace Mimir.Patch;
/// <summary>
///     Applies stat bonuses where the game reads the player's base stat values. Each read stays in place and the
///     value is adjusted right after it, so other mods' transpilers that look for the same reads still find them. It
///     runs before other transpilers, since some replace those reads. Hooks for the player's own stats take the
///     method's <c>this</c> as the owner; item and attack values need no owner.
/// </summary>
[HarmonyPatch]
[HarmonyPriority(Priority.First)]
[HarmonyBefore("MidnightsFX.ImpactfulSkills")]
internal static class StatReads
{
    private readonly struct Read
    {
        internal Read(MethodInfo hook, Type owner)
        {
            Hook = hook;
            Owner = owner;
        }

        internal MethodInfo Hook { get; }

        /// <summary>The type <c>this</c> must be for the hook to take it; null for a hook on the value alone.</summary>
        internal Type Owner { get; }
    }

    private static readonly Dictionary<FieldInfo, Read> _reads = new();

    internal static float BaseHP(float value, Player p) => StatTotals.Apply(p, value, Stat.MaxHealth);
    internal static float BaseStamina(float value, Player p) => StatTotals.Apply(p, value, Stat.MaxStamina);
    internal static float CarryWeight(float value, Player p) => StatTotals.Apply(p, value, Stat.CarryWeight);
    internal static float PickupRange(float value, Player p) => StatTotals.Apply(p, value, Stat.PickupRange);
    internal static float InteractRange(float value, Player p) => StatTotals.Apply(p, value, Stat.InteractRange);
    internal static float StaminaRegen(float value, Player p) => StatTotals.Apply(p, value, Stat.StaminaRegen);
    internal static float EitrRegen(float value, Player p) => StatTotals.Apply(p, value, Stat.EitrRegen);
    internal static float RunStamina(float value, Player p) => StatTotals.Apply(p, value, Stat.RunStamina);
    internal static float SneakStamina(float value, Player p) => StatTotals.Apply(p, value, Stat.SneakStamina);
    internal static float SwimStamina(float value, Player p) => StatTotals.Apply(p, value, Stat.SwimStamina);
    internal static float DodgeStamina(float value, Player p) => StatTotals.Apply(p, value, Stat.DodgeStamina);

    internal static float WalkSpeed(float value, Character c) => StatTotals.Apply(c, value, Stat.WalkSpeed);
    internal static float JogSpeed(float value, Character c) => StatTotals.Apply(c, value, Stat.JogSpeed);
    internal static float RunSpeed(float value, Character c) => StatTotals.Apply(c, value, Stat.RunSpeed);
    internal static float CrouchSpeed(float value, Character c) => StatTotals.Apply(c, value, Stat.CrouchSpeed);
    internal static float SwimSpeed(float value, Character c) => StatTotals.Apply(c, value, Stat.SwimSpeed);
    internal static float JumpForce(float value, Character c) => StatTotals.Apply(c, value, Stat.JumpForce);
    internal static float JumpStamina(float value, Character c) => StatTotals.Apply(c, value, Stat.JumpStamina);

    internal static float BlockStamina(float value, Humanoid h) => StatTotals.Apply(h, value, Stat.BlockStamina);

    /// <summary>A free parry stays free.</summary>
    internal static float ParryStamina(float value, Humanoid h) => value <= 0f ? value : StatTotals.Apply(h, value, Stat.BlockStamina);

    internal static float RestedDuration(float value, SE_Rested r) => StatTotals.Apply(r.m_character, value, Stat.RestedDuration);

    /// <summary>The bow's draw time for the drawing humanoid.</summary>
    internal static float DrawDuration(float value, Humanoid h) => value * StatTotals.Multiplier(h, Stat.DrawTime, h.GetCurrentWeapon());

    /// <summary>Scales food health regen; UpdateFood only runs for the local player.</summary>
    internal static float FoodRegen(float value) => value * StatTotals.Multiplier(Player.m_localPlayer, Stat.HealthRegen);

    /// <summary>Scales the parry multiplier of an item that can parry, never below 1.01.</summary>
    internal static float ParryMultiplier(float value) =>
        BlockScope.Local && value > 1f ? Mathf.Max(1.01f, StatTotals.Apply(Player.m_localPlayer, value, Stat.ParryBonus)) : value;

    /// <summary>The food drain rate divided by the food duration multiplier.</summary>
    internal static float FoodRate(float value) => value / Mathf.Max(0.1f, StatTotals.Multiplier(Player.m_localPlayer, Stat.FoodDuration));

    private static IEnumerable<MethodBase> TargetMethods()
    {
        Type flt = typeof(float);
        Type fltRef = flt.MakeByRefType();

        (Type Type, string Name, Type[] Args)[] targets =
        {
            (typeof(Player), "GetTotalFoodValue", new[] { fltRef, fltRef, fltRef }),
            (typeof(Player), nameof(Player.GetBaseFoodHP), Type.EmptyTypes),
            (typeof(Player), nameof(Player.GetMaxCarryWeight), Type.EmptyTypes),
            (typeof(Player), "UpdateStats", new[] { flt }),
            (typeof(Player), "UpdateFood", new[] { flt, typeof(bool) }),
            (typeof(Player), "CheckRun", new[] { typeof(Vector3), flt }),
            (typeof(Player), "OnSneaking", new[] { flt }),
            (typeof(Player), "OnSwimming", new[] { typeof(Vector3), flt }),
            (typeof(Player), "OnJump", Type.EmptyTypes),
            (typeof(Player), "GetDodgeStaminaUse", Type.EmptyTypes),
            (typeof(Player), "AutoPickup", new[] { flt }),
            (typeof(Player), "FindHoverObject", new[] { typeof(GameObject).MakeByRefType(), typeof(Character).MakeByRefType() }),
            (typeof(Player), "UpdateDoodadControls", new[] { flt }),
            (typeof(Character), "UpdateWalking", new[] { flt }),
            (typeof(Character), "UpdateSwimming", new[] { flt }),
            (typeof(Character), nameof(Character.Jump), new[] { typeof(bool) }),
            (typeof(Humanoid), "BlockAttack", new[] { typeof(HitData), typeof(Character) }),
            (typeof(Humanoid), nameof(Humanoid.GetAttackDrawPercentage), Type.EmptyTypes),
            (typeof(Hud), "UpdateFood", new[] { typeof(Player) }),
            (typeof(SE_Rested), "UpdateTTL", Type.EmptyTypes)
        };

        foreach ((Type type, string name, Type[] args) in targets)
        {
            MethodInfo method = AccessTools.DeclaredMethod(type, name, args);

            if (method != null)
            {
                yield return method;
            }
            else
            {
                Log.LogWarning($"Stat bonuses: {type.Name}.{name} not found; its stats are left as they are.");
            }
        }
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
    {
        if (_reads.Count == 0)
        {
            MapReads();
        }

        List<CodeInstruction> codes = new();
        int adjusted = 0;

        foreach (CodeInstruction code in instructions)
        {
            codes.Add(code);

            if ((code.opcode != OpCodes.Ldfld && code.opcode != OpCodes.Ldsfld) || code.operand is not FieldInfo field
                || !_reads.TryGetValue(field, out Read read))
            {
                continue;
            }

            if (read.Owner != null)
            {
                if (original.IsStatic || !read.Owner.IsAssignableFrom(original.DeclaringType))
                {
                    continue;
                }

                codes.Add(new CodeInstruction(OpCodes.Ldarg_0));
            }

            codes.Add(new CodeInstruction(OpCodes.Call, read.Hook));
            adjusted++;
        }

        if (adjusted == 0)
        {
            Log.LogWarning($"Stat bonuses: no stat reads found in {original.DeclaringType?.Name}.{original.Name}.");
        }

        return codes;
    }

    private static void MapReads()
    {
        Map<Player>(typeof(Player), nameof(Player.m_baseHP), BaseHP);
        Map<Player>(typeof(Player), nameof(Player.m_baseStamina), BaseStamina);
        Map<Player>(typeof(Player), nameof(Player.m_maxCarryWeight), CarryWeight);
        Map<Player>(typeof(Player), nameof(Player.m_autoPickupRange), PickupRange);
        Map<Player>(typeof(Player), nameof(Player.m_maxInteractDistance), InteractRange);
        Map<Player>(typeof(Player), nameof(Player.m_staminaRegen), StaminaRegen);
        Map<Player>(typeof(Player), nameof(Player.m_eiterRegen), EitrRegen);
        Map<Player>(typeof(Player), nameof(Player.m_runStaminaDrain), RunStamina);
        Map<Player>(typeof(Player), nameof(Player.m_sneakStaminaDrain), SneakStamina);
        Map<Player>(typeof(Player), nameof(Player.m_swimStaminaDrainMinSkill), SwimStamina);
        Map<Player>(typeof(Player), nameof(Player.m_swimStaminaDrainMaxSkill), SwimStamina);
        Map<Player>(typeof(Player), nameof(Player.m_dodgeStaminaUsage), DodgeStamina);
        Map<Character>(typeof(Character), nameof(Character.m_walkSpeed), WalkSpeed);
        Map<Character>(typeof(Character), nameof(Character.m_speed), JogSpeed);
        Map<Character>(typeof(Character), nameof(Character.m_runSpeed), RunSpeed);
        Map<Character>(typeof(Character), nameof(Character.m_crouchSpeed), CrouchSpeed);
        Map<Character>(typeof(Character), nameof(Character.m_swimSpeed), SwimSpeed);
        Map<Character>(typeof(Character), nameof(Character.m_jumpForce), JumpForce);
        Map<Character>(typeof(Character), nameof(Character.m_jumpStaminaUsage), JumpStamina);
        Map<Humanoid>(typeof(Humanoid), nameof(Humanoid.m_blockStaminaDrain), BlockStamina);
        Map<Humanoid>(typeof(Humanoid), nameof(Humanoid.m_perfectBlockStaminaDrain), ParryStamina);
        Map<SE_Rested>(typeof(SE_Rested), nameof(SE_Rested.m_baseTTL), RestedDuration);
        Map<SE_Rested>(typeof(SE_Rested), nameof(SE_Rested.m_TTLPerComfortLevel), RestedDuration);
        Map<Humanoid>(typeof(Attack), nameof(Attack.m_drawDurationMin), DrawDuration);
        Map(typeof(ItemDrop.ItemData.SharedData), nameof(ItemDrop.ItemData.SharedData.m_foodRegen), FoodRegen);
        Map(typeof(ItemDrop.ItemData.SharedData), nameof(ItemDrop.ItemData.SharedData.m_timedBlockBonus), ParryMultiplier);
        Map(typeof(Game), nameof(Game.m_foodRate), FoodRate);
    }

    /// <summary>Maps a field to a hook taking the value and the reading method's <c>this</c>.</summary>
    private static void Map<TOwner>(Type declaring, string field, Func<float, TOwner, float> hook)
    {
        Add(declaring, field, new Read(hook.Method, typeof(TOwner)));
    }

    /// <summary>Maps a field to a hook taking the value alone.</summary>
    private static void Map(Type declaring, string field, Func<float, float> hook)
    {
        Add(declaring, field, new Read(hook.Method, null));
    }

    private static void Add(Type declaring, string field, Read read)
    {
        FieldInfo info = AccessTools.DeclaredField(declaring, field);

        if (info != null)
        {
            _reads[info] = read;
        }
        else
        {
            Log.LogWarning($"Stat bonuses: {declaring.Name}.{field} not found; it is left as it is.");
        }
    }
}
