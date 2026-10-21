using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Mimir.Common;
using Mimir.Modules.Tracking.Patches;
using UnityEngine;

namespace Mimir.Modules.Stats.Patches;
/// <summary>Redirects the game's reads of the player's base stat values to hooks that apply the bonuses.</summary>
[HarmonyPatch]
internal static class StatReads
{
    private static readonly Dictionary<FieldInfo, MethodInfo> _reads = new();
    private static FieldInfo _foodRate;
    private static FieldInfo _drawDuration;

    // ---- Hooks; each takes the instance the field was read from ----

    internal static float BaseHP(Player p) => StatTotals.Apply(p, p.m_baseHP, Stat.MaxHealth);
    internal static float BaseStamina(Player p) => StatTotals.Apply(p, p.m_baseStamina, Stat.MaxStamina);
    internal static float CarryWeight(Player p) => StatTotals.Apply(p, p.m_maxCarryWeight, Stat.CarryWeight);
    internal static float PickupRange(Player p) => StatTotals.Apply(p, p.m_autoPickupRange, Stat.PickupRange);
    internal static float InteractRange(Player p) => StatTotals.Apply(p, p.m_maxInteractDistance, Stat.InteractRange);
    internal static float StaminaRegen(Player p) => StatTotals.Apply(p, p.m_staminaRegen, Stat.StaminaRegen);
    internal static float EitrRegen(Player p) => StatTotals.Apply(p, p.m_eiterRegen, Stat.EitrRegen);
    internal static float RunStamina(Player p) => StatTotals.Apply(p, p.m_runStaminaDrain, Stat.RunStamina);
    internal static float SneakStamina(Player p) => StatTotals.Apply(p, p.m_sneakStaminaDrain, Stat.SneakStamina);
    internal static float SwimStaminaLow(Player p) => StatTotals.Apply(p, p.m_swimStaminaDrainMinSkill, Stat.SwimStamina);
    internal static float SwimStaminaHigh(Player p) => StatTotals.Apply(p, p.m_swimStaminaDrainMaxSkill, Stat.SwimStamina);
    internal static float DodgeStamina(Player p) => StatTotals.Apply(p, p.m_dodgeStaminaUsage, Stat.DodgeStamina);

    internal static float WalkSpeed(Character c) => StatTotals.Apply(c, c.m_walkSpeed, Stat.WalkSpeed);
    internal static float JogSpeed(Character c) => StatTotals.Apply(c, c.m_speed, Stat.JogSpeed);
    internal static float RunSpeed(Character c) => StatTotals.Apply(c, c.m_runSpeed, Stat.RunSpeed);
    internal static float CrouchSpeed(Character c) => StatTotals.Apply(c, c.m_crouchSpeed, Stat.CrouchSpeed);
    internal static float SwimSpeed(Character c) => StatTotals.Apply(c, c.m_swimSpeed, Stat.SwimSpeed);
    internal static float JumpForce(Character c) => StatTotals.Apply(c, c.m_jumpForce, Stat.JumpForce);
    internal static float JumpStamina(Character c) => StatTotals.Apply(c, c.m_jumpStaminaUsage, Stat.JumpStamina);

    internal static float BlockStamina(Humanoid h) => StatTotals.Apply(h, h.m_blockStaminaDrain, Stat.BlockStamina);

    /// <summary>A free parry stays free.</summary>
    internal static float ParryStamina(Humanoid h) =>
        h.m_perfectBlockStaminaDrain <= 0f ? h.m_perfectBlockStaminaDrain : StatTotals.Apply(h, h.m_perfectBlockStaminaDrain, Stat.BlockStamina);

    internal static float RestedBase(SE_Rested r) => StatTotals.Apply(r.m_character, r.m_baseTTL, Stat.RestedDuration);
    internal static float RestedPerComfort(SE_Rested r) => StatTotals.Apply(r.m_character, r.m_TTLPerComfortLevel, Stat.RestedDuration);

    /// <summary>Scales food health regen; UpdateFood only runs for the local player.</summary>
    internal static float FoodRegen(ItemDrop.ItemData.SharedData s) => s.m_foodRegen * StatTotals.Multiplier(Player.m_localPlayer, Stat.HealthRegen);

    /// <summary>Scales the parry multiplier of an item that can parry, never below 1.01.</summary>
    internal static float ParryMultiplier(ItemDrop.ItemData.SharedData s)
    {
        float value = s.m_timedBlockBonus;
        return BlockScope.Local && value > 1f ? Mathf.Max(1.01f, StatTotals.Apply(Player.m_localPlayer, value, Stat.ParryBonus)) : value;
    }

    /// <summary>Replaces <c>ldsfld Game.m_foodRate</c> with the rate divided by the food duration multiplier.</summary>
    internal static float FoodRate() => Game.m_foodRate / Mathf.Max(0.1f, StatTotals.Multiplier(Player.m_localPlayer, Stat.FoodDuration));

    /// <summary>Replaces the bow's draw time, with the drawing humanoid pushed after it.</summary>
    internal static float DrawDuration(Attack attack, Humanoid humanoid)
    {
        return attack.m_drawDurationMin * StatTotals.Multiplier(humanoid, Stat.DrawTime, humanoid.GetCurrentWeapon());
    }

    // ---- Patch ----

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
        int replaced = 0;

        foreach (CodeInstruction code in instructions)
        {
            if (code.opcode == OpCodes.Ldfld && code.operand is FieldInfo field)
            {
                if (_reads.TryGetValue(field, out MethodInfo hook))
                {
                    code.opcode = OpCodes.Call;
                    code.operand = hook;
                    replaced++;
                }
                else if (field == _drawDuration && original.DeclaringType == typeof(Humanoid))
                {
                    // The attack is already on the stack; the humanoid (this) is pushed after it.
                    code.opcode = OpCodes.Ldarg_0;
                    code.operand = null;
                    codes.Add(code);
                    codes.Add(new CodeInstruction(OpCodes.Call, Hook<Func<Attack, Humanoid, float>>(DrawDuration)));
                    replaced++;
                    continue;
                }
            }
            else if (code.opcode == OpCodes.Ldsfld && code.operand is FieldInfo staticField && staticField == _foodRate)
            {
                code.opcode = OpCodes.Call;
                code.operand = Hook<Func<float>>(FoodRate);
                replaced++;
            }

            codes.Add(code);
        }

        if (replaced == 0)
        {
            Log.LogWarning($"Stat bonuses: no stat reads found in {original.DeclaringType?.Name}.{original.Name}.");
        }

        return codes;
    }

    private static void MapReads()
    {
        Map<Player>(nameof(Player.m_baseHP), BaseHP);
        Map<Player>(nameof(Player.m_baseStamina), BaseStamina);
        Map<Player>(nameof(Player.m_maxCarryWeight), CarryWeight);
        Map<Player>(nameof(Player.m_autoPickupRange), PickupRange);
        Map<Player>(nameof(Player.m_maxInteractDistance), InteractRange);
        Map<Player>(nameof(Player.m_staminaRegen), StaminaRegen);
        Map<Player>(nameof(Player.m_eiterRegen), EitrRegen);
        Map<Player>(nameof(Player.m_runStaminaDrain), RunStamina);
        Map<Player>(nameof(Player.m_sneakStaminaDrain), SneakStamina);
        Map<Player>(nameof(Player.m_swimStaminaDrainMinSkill), SwimStaminaLow);
        Map<Player>(nameof(Player.m_swimStaminaDrainMaxSkill), SwimStaminaHigh);
        Map<Player>(nameof(Player.m_dodgeStaminaUsage), DodgeStamina);
        Map<Character>(nameof(Character.m_walkSpeed), WalkSpeed);
        Map<Character>(nameof(Character.m_speed), JogSpeed);
        Map<Character>(nameof(Character.m_runSpeed), RunSpeed);
        Map<Character>(nameof(Character.m_crouchSpeed), CrouchSpeed);
        Map<Character>(nameof(Character.m_swimSpeed), SwimSpeed);
        Map<Character>(nameof(Character.m_jumpForce), JumpForce);
        Map<Character>(nameof(Character.m_jumpStaminaUsage), JumpStamina);
        Map<Humanoid>(nameof(Humanoid.m_blockStaminaDrain), BlockStamina);
        Map<Humanoid>(nameof(Humanoid.m_perfectBlockStaminaDrain), ParryStamina);
        Map<SE_Rested>(nameof(SE_Rested.m_baseTTL), RestedBase);
        Map<SE_Rested>(nameof(SE_Rested.m_TTLPerComfortLevel), RestedPerComfort);
        Map<ItemDrop.ItemData.SharedData>(nameof(ItemDrop.ItemData.SharedData.m_foodRegen), FoodRegen);
        Map<ItemDrop.ItemData.SharedData>(nameof(ItemDrop.ItemData.SharedData.m_timedBlockBonus), ParryMultiplier);

        _foodRate = AccessTools.DeclaredField(typeof(Game), nameof(Game.m_foodRate));
        _drawDuration = AccessTools.DeclaredField(typeof(Attack), nameof(Attack.m_drawDurationMin));
    }

    private static void Map<T>(string field, Func<T, float> hook)
    {
        FieldInfo info = AccessTools.DeclaredField(typeof(T), field);

        if (info != null)
        {
            _reads[info] = hook.Method;
        }
        else
        {
            Log.LogWarning($"Stat bonuses: {typeof(T).Name}.{field} not found; it is left as it is.");
        }
    }

    private static MethodInfo Hook<T>(T hook) where T : Delegate => hook.Method;
}
