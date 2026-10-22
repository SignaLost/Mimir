using BepInEx.Configuration;
using UnityEngine;
using Mimir.Common;

namespace Mimir.Modules;

/// <summary>How fortified level-ups show.</summary>
internal enum FortifyNotice
{
    Effects,
    Notify,
    Off
}

/// <summary>
///     Announces a fortified level earned through gains, by the <c>Fortify Notices</c> setting. The notice uses the game's
///     own skill-up message, so UI mods that restyle skill-ups show it the same way, named as the skill "(Fortified)".
///     <c>Effects</c> also plays the game's level-up effects in blue at half opacity, with fewer particles and a lower pitch.
/// </summary>
internal static class FortifyNotices
{
    private const float Fewer = 0.75f;
    private const float Opacity = 0.5f;
    private const float Pitch = 0.8f;

    internal static ConfigEntry<FortifyNotice> Setting;

    private static readonly float BlueHue = Hue(FortifiedDisplay.Blue);

    /// <summary>Shows that a skill's fortified level rose to the given level.</summary>
    internal static void LeveledUp(Skills.SkillType type, int level)
    {
        FortifyNotice notice = Setting?.Value ?? FortifyNotice.Effects;
        Player player = Player.m_localPlayer;

        if (notice == FortifyNotice.Off || !player)
        {
            return;
        }

        if (notice == FortifyNotice.Effects && player.m_head)
        {
            Recolor(player.m_skillLevelupEffects.Create(player.m_head.position, player.m_head.rotation, player.m_head, 1f, -1, player.GetZDOID()));
        }

        // The game's skill-up text, left untranslated so UI mods can read the skill and level from it.
        string skill = "$skill_" + type.ToString().ToLowerInvariant();
        Sprite icon = player.GetSkills().GetSkillDef(type)?.m_icon;
        UnlockNotices.Notify($"$msg_skillup {skill} ({Tokens.Fortified}): {level}", icon);
    }

    /// <summary>Turns the level-up effects blue at the same brightness and half opacity, with fewer particles and a lower pitch.</summary>
    private static void Recolor(GameObject[] effects)
    {
        if (effects == null)
        {
            return;
        }

        // Set in the frame they are made, before their particles first emit.
        foreach (GameObject effect in effects)
        {
            if (!effect)
            {
                continue;
            }

            foreach (ParticleSystem particles in effect.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = particles.main;
                main.maxParticles = Mathf.Max(1, Mathf.RoundToInt(main.maxParticles * Fewer));
                main.startColor = Faded(Blue(main.startColor));

                ParticleSystem.EmissionModule emission = particles.emission;
                emission.rateOverTimeMultiplier *= Fewer;

                ParticleSystem.ColorOverLifetimeModule overLifetime = particles.colorOverLifetime;

                if (overLifetime.enabled)
                {
                    overLifetime.color = Blue(overLifetime.color);
                }

                ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();

                if (renderer && renderer.material)
                {
                    Blue(renderer.material);
                }
            }

            foreach (Light light in effect.GetComponentsInChildren<Light>(true))
            {
                light.color = Blue(light.color);
            }

            foreach (AudioSource audio in effect.GetComponentsInChildren<AudioSource>(true))
            {
                audio.pitch *= Pitch;
            }
        }
    }

    /// <summary>The particles' start color at half its opacity, which the rest of their color multiplies.</summary>
    private static ParticleSystem.MinMaxGradient Faded(ParticleSystem.MinMaxGradient color)
    {
        switch (color.mode)
        {
            case ParticleSystemGradientMode.Color:
                color.color = Faded(color.color);
                break;
            case ParticleSystemGradientMode.TwoColors:
                color.colorMin = Faded(color.colorMin);
                color.colorMax = Faded(color.colorMax);
                break;
            case ParticleSystemGradientMode.TwoGradients:
                color.gradientMin = Faded(color.gradientMin);
                color.gradientMax = Faded(color.gradientMax);
                break;
            default:
                color.gradient = Faded(color.gradient);
                break;
        }

        return color;
    }

    private static Color Faded(Color color)
    {
        color.a *= Opacity;
        return color;
    }

    private static Gradient Faded(Gradient gradient)
    {
        if (gradient == null)
        {
            return null;
        }

        GradientAlphaKey[] keys = gradient.alphaKeys;

        for (int i = 0; i < keys.Length; i++)
        {
            keys[i].alpha *= Opacity;
        }

        gradient.SetKeys(gradient.colorKeys, keys);
        return gradient;
    }

    /// <summary>The color with the blue's hue, keeping its saturation, brightness and alpha.</summary>
    private static Color Blue(Color color)
    {
        Color.RGBToHSV(color, out _, out float saturation, out float value);
        Color blue = Color.HSVToRGB(BlueHue, saturation, value, true);
        blue.a = color.a;
        return blue;
    }

    private static Gradient Blue(Gradient gradient)
    {
        if (gradient == null)
        {
            return null;
        }

        GradientColorKey[] keys = gradient.colorKeys;

        for (int i = 0; i < keys.Length; i++)
        {
            keys[i].color = Blue(keys[i].color);
        }

        Gradient blue = new() { mode = gradient.mode };
        blue.SetKeys(keys, gradient.alphaKeys);
        return blue;
    }

    private static ParticleSystem.MinMaxGradient Blue(ParticleSystem.MinMaxGradient color)
    {
        switch (color.mode)
        {
            case ParticleSystemGradientMode.Color:
                color.color = Blue(color.color);
                break;
            case ParticleSystemGradientMode.TwoColors:
                color.colorMin = Blue(color.colorMin);
                color.colorMax = Blue(color.colorMax);
                break;
            case ParticleSystemGradientMode.TwoGradients:
                color.gradientMin = Blue(color.gradientMin);
                color.gradientMax = Blue(color.gradientMax);
                break;
            default:
                color.gradient = Blue(color.gradient);
                break;
        }

        return color;
    }

    /// <summary>Recolors a particle material's tint; the renderer's own copy, so other effects keep theirs.</summary>
    private static void Blue(Material material)
    {
        foreach (string property in new[] { "_Color", "_TintColor", "_EmissionColor" })
        {
            if (material.HasProperty(property))
            {
                material.SetColor(property, Blue(material.GetColor(property)));
            }
        }
    }

    private static float Hue(Color color)
    {
        Color.RGBToHSV(color, out float hue, out _, out _);
        return hue;
    }
}
