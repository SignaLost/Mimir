using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;
using Mimir.Common;

namespace Mimir.Modules;

/// <summary>
///     Lists what the game's own skills do at the player's level as perks on the Skill Abilities page, turned on by
///     <c>Show Game Skill Effects</c>. Each amount repeats the game's formula for it; values the game keeps on prefabs,
///     such as swim stamina or the fishing float, are read from those prefabs. Weapon effects follow the weapon's skill.
/// </summary>
internal static class SkillEffects
{
    internal static ConfigEntry<bool> Show;

    internal static readonly Skills.SkillType[] Types =
    {
        Skills.SkillType.Swords, Skills.SkillType.Knives, Skills.SkillType.Clubs, Skills.SkillType.Polearms,
        Skills.SkillType.Spears, Skills.SkillType.Blocking, Skills.SkillType.Axes, Skills.SkillType.Bows,
        Skills.SkillType.ElementalMagic, Skills.SkillType.BloodMagic, Skills.SkillType.Unarmed, Skills.SkillType.Pickaxes,
        Skills.SkillType.WoodCutting, Skills.SkillType.Crossbows, Skills.SkillType.Jump, Skills.SkillType.Sneak,
        Skills.SkillType.Run, Skills.SkillType.Swim, Skills.SkillType.Fishing, Skills.SkillType.Cooking,
        Skills.SkillType.Farming, Skills.SkillType.Crafting, Skills.SkillType.Dodge, Skills.SkillType.Ride
    };

    private static HashSet<Skills.SkillType> _buildSkills;
    private static FishingFloat _float;
    private static Attack _harvest;
    private static ObjectDB _scanned;

    internal static bool Enabled => Show != null && Show.Value;

    /// <summary>Adds the skill's game effects at the player's level.</summary>
    internal static void Collect(Skills.SkillType type, Skills skills, Player player, PerkList perks)
    {
        Scan();
        float f = skills.GetSkillFactor(type);

        switch (type)
        {
            case Skills.SkillType.Swords:
            case Skills.SkillType.Knives:
            case Skills.SkillType.Clubs:
            case Skills.SkillType.Polearms:
            case Skills.SkillType.Spears:
            case Skills.SkillType.Axes:
            case Skills.SkillType.Unarmed:
            case Skills.SkillType.Pickaxes:
                Damage(type, skills, perks, "damage");
                perks.Add(Effect("attack_stamina"), -33f * f, "%");
                break;
            case Skills.SkillType.WoodCutting:
                Damage(type, skills, perks, "chop_damage");
                break;
            case Skills.SkillType.ElementalMagic:
                Damage(type, skills, perks, "damage");
                perks.Add(Effect("attack_eitr"), -33f * f, "%");
                break;
            case Skills.SkillType.BloodMagic:
                Damage(type, skills, perks, "damage");
                perks.Add(Effect("attack_eitr"), -33f * f, "%");
                perks.Add(Effect("attack_health"), -33f * f, "%");
                break;
            case Skills.SkillType.Bows:
                Damage(type, skills, perks, "damage");
                perks.Add(Effect("draw_time"), -(1f - Mathf.Lerp(1f, 0.2f, f)) * 100f, "%");
                perks.Add(Effect("draw_stamina"), -33f * f, "%");
                break;
            case Skills.SkillType.Crossbows:
                Damage(type, skills, perks, "damage");
                perks.Add(Effect("reload_time"), -(1f - Mathf.Lerp(1f, 0.5f, f)) * 100f, "%");
                break;
            case Skills.SkillType.Blocking:
                perks.Add(Effect("block_power"), 50f * f, "%");
                break;
            case Skills.SkillType.Jump:
                perks.Add(Effect("jump_force"), 40f * f, "%");
                break;
            case Skills.SkillType.Run:
                perks.Add(Effect("run_stamina"), -(1f - Mathf.Lerp(1f, 0.5f, f)) * 100f, "%");
                perks.Add(Effect("run_speed"), 25f * f, "%");
                break;
            case Skills.SkillType.Swim:
                if (player.m_swimStaminaDrainMinSkill > 0f)
                {
                    perks.Add(Effect("swim_stamina"), Change(player.m_swimStaminaDrainMinSkill, Mathf.Lerp(player.m_swimStaminaDrainMinSkill, player.m_swimStaminaDrainMaxSkill, f)), "%");
                }

                break;
            case Skills.SkillType.Dodge:
                perks.Add(Effect("dodge_stamina"), -(1f - Mathf.Lerp(1f, 0.5f, f)) * 100f, "%");
                break;
            case Skills.SkillType.Sneak:
                perks.Add(Effect("sneak_stamina"), -(1f - Mathf.Lerp(1f, 0.25f, Mathf.Pow(f, 0.5f))) * 100f, "%");
                // Crouched visibility in full light and in full dark.
                perks.Add(Effect("visibility_light"), Change(1f, Mathf.Lerp(1f, 0.6f, f)), "%");
                perks.Add(Effect("visibility_dark"), Change(0.5f, Mathf.Lerp(0.5f, 0.2f, f)), "%");
                break;
            case Skills.SkillType.Ride:
                perks.Add(Effect("mount_stamina"), -(1f - Mathf.Lerp(1f, 0.5f, f)) * 100f, "%");
                perks.Add(Effect("mount_speed"), 25f * f, "%");
                break;
            case Skills.SkillType.Fishing:
                Fishing(f, perks);
                break;
            case Skills.SkillType.Cooking:
                if (InventoryGui.instance)
                {
                    perks.Add(Effect("bonus_chance"), f * InventoryGui.instance.m_craftBonusChance * 100f, "%");
                }

                break;
            case Skills.SkillType.Crafting:
                if (InventoryGui.instance)
                {
                    perks.Add(Effect("craft_time"), -f * InventoryGui.instance.m_craftDurationSkillMaxDecrease * 100f, "%");
                    perks.Add(Effect("bonus_chance"), f * InventoryGui.instance.m_craftBonusChance * 100f, "%");
                }

                break;
            case Skills.SkillType.Farming:
                if (_harvest != null && _harvest.m_harvestRadius > 0f)
                {
                    perks.Add(Effect("harvest_radius"), Change(_harvest.m_harvestRadius, Mathf.Lerp(_harvest.m_harvestRadius, _harvest.m_harvestRadiusMaxLevel, f)), "%");
                }

                break;
        }

        if (_buildSkills != null && _buildSkills.Contains(type))
        {
            perks.Add(Effect("build_stamina"), -50f * f, "%");
        }
    }

    private static string Effect(string name)
    {
        return Names.Localize(Tokens.EffectPrefix + name);
    }

    /// <summary>The percent change from <paramref name="from" /> to <paramref name="to" />.</summary>
    private static float Change(float from, float to)
    {
        return (to / from - 1f) * 100f;
    }

    /// <summary>Adds the damage range as a percent of the weapon's damage, from the game's own random factor.</summary>
    private static void Damage(Skills.SkillType type, Skills skills, PerkList perks, string name)
    {
        skills.GetRandomSkillRange(out float min, out float max, type);
        perks.AddSpan(Effect(name), min * 100f, max * 100f, "%");
    }

    private static void Fishing(float f, PerkList perks)
    {
        if (_float == null)
        {
            return;
        }

        if (_float.m_hookedStaminaPerSec > 0f)
        {
            perks.Add(Effect("hooked_stamina"), Change(_float.m_hookedStaminaPerSec, Mathf.Lerp(_float.m_hookedStaminaPerSec, _float.m_hookedStaminaPerSecMaxSkill, f)), "%");
        }

        perks.Add(Effect("reel_stamina"), Change(1f, Mathf.Lerp(1f, _float.m_pullStaminaUseMaxSkillMultiplier, f)), "%");

        if (_float.m_pullLineSpeed > 0f)
        {
            perks.Add(Effect("reel_speed"), Change(_float.m_pullLineSpeed, Mathf.Lerp(_float.m_pullLineSpeed, _float.m_pullLineSpeedMaxSkill, f)), "%");
        }
    }

    /// <summary>Finds the prefab values once per object database: the skills of build tables, the fishing float and the harvesting attack.</summary>
    private static void Scan()
    {
        if (!ObjectDB.instance || ObjectDB.instance == _scanned || ObjectDB.instance.m_items.Count == 0)
        {
            return;
        }

        _scanned = ObjectDB.instance;
        _float = null;
        _harvest = null;
        _buildSkills = new HashSet<Skills.SkillType>();

        foreach (GameObject prefab in ObjectDB.instance.m_items)
        {
            ItemDrop.ItemData.SharedData shared = prefab && prefab.GetComponent<ItemDrop>() is ItemDrop drop ? drop.m_itemData.m_shared : null;

            if (shared == null)
            {
                continue;
            }

            if (shared.m_buildPieces && shared.m_buildPieces.m_skill != Skills.SkillType.None)
            {
                _buildSkills.Add(shared.m_buildPieces.m_skill);
            }

            Attack attack = shared.m_attack;

            if (attack == null)
            {
                continue;
            }

            if (_harvest == null && attack.m_harvest)
            {
                _harvest = attack;
            }

            if (_float == null && shared.m_skillType == Skills.SkillType.Fishing && attack.m_attackProjectile
                && attack.m_attackProjectile.GetComponent<FishingFloat>() is FishingFloat fishingFloat)
            {
                _float = fishingFloat;
            }
        }
    }
}
