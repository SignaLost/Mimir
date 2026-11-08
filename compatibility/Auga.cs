using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;
using Mimir.Common;
using Mimir.Modules;
using UnityEngine;
using UnityEngine.UI;

namespace Mimir.Compatibility;

/// <summary>
///     Fits Mimir into Auga's interface. Auga rebuilds the skills panel and the compendium, so Mimir's fortified
///     levels go on Auga's skill rows and into its skill tooltips, the Skill Abilities page goes only on the Hugin
///     tab of its compendium, and the page takes Auga's colors. Everything is reached by reflection, so a changed
///     Auga leaves Mimir working with a warning rather than failing.
/// </summary>
internal static class Auga
{
    internal const string Guid = "randyknapp.mods.auga";

    private static bool? _present;
    private static bool _resolved;
    private static Type _api;
    private static Type _controller;
    private static FieldInfo _lore;
    private static MethodInfo _addSkillListener;
    private static MethodInfo _addTextBox;
    private static MethodInfo _addLine;

    internal static bool Present => _present ??= Chainloader.PluginInfos.ContainsKey(Guid);

    /// <summary>Tells Auga to leave Mimir's own interface pieces as they are drawn; Auga restyles what mods that depend on it make unless asked not to.</summary>
    internal static void KeepOwnLook(string pluginGuid)
    {
        if (!Present || !Resolve())
        {
            return;
        }

        try
        {
            AccessTools.Method(_api, "AutoRestyle_Exclude", new[] { typeof(string) })?.Invoke(null, new object[] { pluginGuid });
        }
        catch (Exception e)
        {
            Log.LogWarning($"Auga could not be asked to leave Mimir's interface alone: {e.Message}");
        }
    }

    /// <summary>Auga's color of that name, as a "#RRGGBB" rich text value, or the fallback.</summary>
    internal static string Color(string name, string fallback)
    {
        if (!Present || !Resolve())
        {
            return fallback;
        }

        return AccessTools.Field(_api, name)?.GetValue(null) as string ?? fallback;
    }

    /// <summary>Whether the dialog is the Lore tab of Auga's compendium, which lists lore alone.</summary>
    internal static bool IsLoreTab(TextsDialog dialog)
    {
        if (!Present || !Resolve() || _controller == null || !dialog)
        {
            return false;
        }

        Component controller = dialog.GetComponentInParent(_controller);
        return controller && ReferenceEquals(_lore.GetValue(controller), dialog);
    }

    /// <summary>Adds Mimir's lines to Auga's skill tooltips: the fortified level and the abilities unlocked.</summary>
    internal static void AddTooltipLines()
    {
        if (!Present || !Resolve() || _addSkillListener == null)
        {
            return;
        }

        try
        {
            _addSkillListener.Invoke(null, new object[] { new Action<GameObject, Skills.Skill>(OnSkillTooltip) });
        }
        catch (Exception e)
        {
            Log.LogWarning($"Auga's skill tooltips could not take Mimir's lines: {e.Message}");
        }
    }

    private static void OnSkillTooltip(GameObject tooltip, Skills.Skill skill)
    {
        if (!tooltip || skill?.m_info == null)
        {
            return;
        }

        Skills.SkillType type = skill.m_info.m_skill;
        MimirSkill mimir = MimirAPI.Find(type);
        bool fortified = FortifiedDisplay.Shows(type);

        if (!fortified && mimir == null)
        {
            return;
        }

        try
        {
            GameObject box = (GameObject)_addTextBox.Invoke(null, new object[] { tooltip });

            if (fortified)
            {
                _addLine.Invoke(null, new object[] { box, Tokens.Fortified, Mathf.FloorToInt(Fortifying.LevelOf(type)), true });
            }

            if (mimir != null)
            {
                int unlocked = 0;

                foreach (Ability ability in mimir.Abilities)
                {
                    if (mimir.IsUnlocked(ability.Id))
                    {
                        unlocked++;
                    }
                }

                _addLine.Invoke(null, new object[] { box, Tokens.PageAbilities, $"{unlocked}/{mimir.Abilities.Count}", true });
            }
        }
        catch (Exception e)
        {
            Log.LogWarning($"Auga's skill tooltip could not take Mimir's lines: {e.Message}");
        }
    }

    /// <summary>The method Auga's skill rows refresh with, patched to add the fortified level; null without Auga.</summary>
    internal static MethodBase SkillRowMethod()
    {
        if (!Present)
        {
            return null;
        }

        Type row = AccessTools.TypeByName("AugaUnity.SkillsPanelSkillController");
        MethodInfo method = row != null ? AccessTools.Method(row, "UpdateSkill") : null;

        if (method == null || AccessTools.Field(row, "SkillType") == null || AccessTools.Field(row, "LevelText") == null
            || AccessTools.Field(row, "ProgressBarLevel") == null || AccessTools.Field(row, "ProgressBarAccumulator") == null)
        {
            Log.LogWarning("Auga's skills panel has another shape than expected; fortified levels are not shown on it.");
            return null;
        }

        return method;
    }

    /// <summary>
    ///     Puts the fortified level after the level on one of Auga's skill rows, and redraws its rings: the yellow ring
    ///     to level 100 stays Auga's; a white ring of Mimir's own fills the room beneath it with progress to the next
    ///     level; and a blue copy of Auga's white ring, where that one used to be, fills toward the next fortified level.
    ///     Each ring has a see-through dark track under it, shaded like the yellow ring's.
    /// </summary>
    internal static void MarkSkillRow(object row)
    {
        Type kind = row.GetType();

        if (!(AccessTools.Field(kind, "SkillType")?.GetValue(row) is Skills.SkillType type))
        {
            return;
        }

        bool shows = FortifiedDisplay.Shows(type);
        Image level = AccessTools.Field(kind, "ProgressBarLevel")?.GetValue(row) as Image;
        Image progress = AccessTools.Field(kind, "ProgressBarAccumulator")?.GetValue(row) as Image;
        Rings rings = level ? level.transform.parent.GetComponent<Rings>() : null;

        // A row whose skill does not fortify is Auga's to draw.
        if (!shows)
        {
            if (rings)
            {
                rings.Show(false);
            }

            return;
        }

        if (AccessTools.Field(kind, "LevelText")?.GetValue(row) is TMPro.TMP_Text text)
        {
            FortifiedDisplay.AppendNumber(text, type);
        }

        if (!level || !progress)
        {
            return;
        }

        if (!rings)
        {
            float start = AccessTools.Field(kind, "StartFill")?.GetValue(row) as float? ?? 0f;
            float end = AccessTools.Field(kind, "EndFill")?.GetValue(row) as float? ?? 1f;
            rings = level.transform.parent.gameObject.AddComponent<Rings>();
            rings.Build(level, progress, start, end);
        }

        rings.Show(true);
        rings.Refresh(level, progress, Fortifying.NextFraction(type));
    }

    /// <summary>
    ///     The rings Mimir draws on one of Auga's skill rows in place of Auga's own: yellow to level 100, white to the
    ///     next level and blue to the next fortified level, nested from the outside in, each over a see-through track.
    ///     They keep Auga's colors, arc, thickness and outer edge, so the row looks as it did with one more ring.
    /// </summary>
    private sealed class Rings : MonoBehaviour
    {
        // Each ring's thickness next to the yellow's: Auga draws the white thinner than the yellow, and the blue is
        // thinner still. They sit flush against one another.
        private static readonly float[] Thickness = { 1f, 0.43f, 0.2f };

        private Image _yellow;
        private Image _white;
        private Image _blue;
        private readonly List<Image> _drawn = new();
        private readonly List<(float Inner, float Outer)> _radii = new();
        private readonly List<Image> _hidden = new();
        private float _start;
        private float _end;

        internal void Build(Image yellow, Image white, float start, float end)
        {
            _start = start;
            _end = end;

            Image yellowTrack = TrackUnder(yellow);
            Color trackColor = yellowTrack ? yellowTrack.color : TrackColor;
            Transform parent = yellow.transform.parent;
            int at = Mathf.Min(yellow.transform.GetSiblingIndex(), white.transform.GetSiblingIndex());

            // Auga's outermost edge and ring thickness set the scale; the three rings nest inward from there, each thinner.
            float ringOuter = yellow.rectTransform.rect.width * 0.5f * yellow.rectTransform.localScale.x;
            float yellowThickness = ThicknessOf(yellow);

            for (int i = 0; i < 3; i++)
            {
                float thickness = yellowThickness * Thickness[i];

                if (i > 0)
                {
                    ringOuter -= yellowThickness * Thickness[i - 1];
                }

                Sprite ring = RingSprite(ringOuter - thickness, ringOuter);
                Color color = i == 0 ? yellow.color : i == 1 ? white.color : FortifiedDisplay.Blue;
                string name = i == 0 ? "mimir_level" : i == 1 ? "mimir_progress" : FortifiedDisplay.BarName;

                Image track = Draw(ring, parent, name + "_track", trackColor, end, yellow, ringOuter);
                Image fill = Draw(ring, parent, name, color, 0f, yellow, ringOuter);
                track.transform.SetSiblingIndex(at++);
                fill.transform.SetSiblingIndex(at++);
                _drawn.Add(track);
                _drawn.Add(fill);
                _radii.Add((ringOuter - thickness, ringOuter));
                _radii.Add((ringOuter - thickness, ringOuter));

                if (i == 0) _yellow = fill;
                else if (i == 1) _white = fill;
                else _blue = fill;
            }

            // Auga's own rings and their tracks give way.
            foreach (Image image in new[] { yellow, white, yellowTrack, TrackUnder(white) })
            {
                if (image)
                {
                    _hidden.Add(image);
                }
            }
        }

        internal void Show(bool on)
        {
            foreach (Image image in _drawn)
            {
                if (image)
                {
                    image.enabled = on;
                }
            }

            foreach (Image image in _hidden)
            {
                if (image)
                {
                    image.enabled = !on;
                }
            }
        }

        internal void Refresh(Image yellow, Image white, float fortifiedFraction)
        {
            // A ring picture the game let go of, as can happen across a settings change, is drawn again.
            for (int i = 0; i < _drawn.Count; i++)
            {
                if (_drawn[i] && !_drawn[i].sprite)
                {
                    _drawn[i].sprite = RingSprite(_radii[i].Inner, _radii[i].Outer);
                }
            }

            if (_yellow)
            {
                _yellow.fillAmount = yellow.fillAmount;
                _yellow.color = yellow.color;
            }

            if (_white)
            {
                _white.fillAmount = white.fillAmount;
                _white.color = white.color;
            }

            if (_blue)
            {
                _blue.fillAmount = Mathf.Lerp(_start, _end, Mathf.Clamp01(fortifiedFraction));
            }
        }

        /// <summary>A ring of Mimir's own, centered where the model ring is, turned and filled the way it is.</summary>
        private static Image Draw(Sprite ring, Transform parent, string name, Color color, float fill, Image model, float outer)
        {
            GameObject holder = new(name, typeof(RectTransform), typeof(Image));
            holder.transform.SetParent(parent, false);

            // Centered on the model ring with a fixed size, whatever anchoring the model uses.
            RectTransform rect = (RectTransform)holder.transform;
            RectTransform at = model.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.one * (outer * 2f);
            rect.localRotation = at.localRotation;
            rect.position = at.TransformPoint(at.rect.center);

            Image image = holder.GetComponent<Image>();
            image.sprite = ring;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Radial360;
            image.fillOrigin = model.fillOrigin;
            image.fillClockwise = model.fillClockwise;
            image.fillAmount = fill;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>The dark ring under a ring, as its unfilled part: a sibling drawn before it, the same size and scale, dark and filled at least as far.</summary>
        private static Image TrackUnder(Image ring)
        {
            Image found = null;
            Vector2 size = ring.rectTransform.rect.size;
            Vector3 scale = ring.rectTransform.localScale;

            foreach (Transform sibling in ring.transform.parent)
            {
                Image image = sibling.GetComponent<Image>();

                if (!image || image == ring || sibling.GetSiblingIndex() > ring.transform.GetSiblingIndex() || image.fillAmount < ring.fillAmount
                    || (image.rectTransform.rect.size - size).sqrMagnitude > 4f || (image.rectTransform.localScale - scale).sqrMagnitude > 0.0001f
                    || image.color.grayscale > 0.45f || image.name.StartsWith("mimir", StringComparison.Ordinal))
                {
                    continue;
                }

                if (!found || sibling.GetSiblingIndex() > found.transform.GetSiblingIndex())
                {
                    found = image;
                }
            }

            return found;
        }

        /// <summary>How thick the yellow ring is drawn, in the row's pixels, as a share of its radius.</summary>
        private static float ThicknessOf(Image ring)
        {
            return ring.rectTransform.rect.width * 0.5f * ring.rectTransform.localScale.x * YellowThickness;
        }
    }

    // How thick the yellow ring is next to its radius, matched by eye to Auga's own.
    private const float YellowThickness = 0.22f;

    // Auga's tracks are dark and see-through; this stands in when none of its own can be read.
    private static readonly Color TrackColor = new(0.08f, 0.07f, 0.06f, 0.4f);

    private static readonly Dictionary<(int, int), Sprite> _rings = new();

    /// <summary>A soft-edged ring picture with these radii in pixels, drawn once per size.</summary>
    private static Sprite RingSprite(float inner, float outer)
    {
        (int, int) key = (Mathf.RoundToInt(inner * 4f), Mathf.RoundToInt(outer * 4f));

        if (_rings.TryGetValue(key, out Sprite made) && made && made.texture)
        {
            return made;
        }

        // Drawn at twice the size it shows at, so its edges stay smooth.
        int size = Mathf.Max(8, Mathf.CeilToInt(outer * 4f));
        float center = size * 0.5f;
        float scale = size / (outer * 2f);
        float innerPx = inner * scale;
        float outerPx = outer * scale;
        Texture2D texture = new(size, size, TextureFormat.RGBA32, false) { name = "MimirRing", wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
        Color32[] pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Mathf.Sqrt((x + 0.5f - center) * (x + 0.5f - center) + (y + 0.5f - center) * (y + 0.5f - center));
                float alpha = Mathf.Clamp01(outerPx - distance + 0.5f) * Mathf.Clamp01(distance - innerPx + 0.5f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        made = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        made.name = "MimirRing";
        made.hideFlags = HideFlags.HideAndDontSave;
        _rings[key] = made;
        return made;
    }

    private static bool Resolve()
    {
        if (_resolved)
        {
            return _api != null;
        }

        _resolved = true;
        _api = AccessTools.TypeByName("Auga.API");
        _controller = AccessTools.TypeByName("AugaUnity.AugaCompendiumController");
        _lore = _controller != null ? AccessTools.Field(_controller, "LoreCompendium") : null;

        if (_api == null)
        {
            Log.LogWarning("Auga's API was not found; Mimir keeps its own look.");
            return false;
        }

        if (_lore == null || _lore.FieldType != typeof(TextsDialog))
        {
            _controller = null;
        }

        _addSkillListener = AccessTools.Method(_api, "ComplexTooltip_AddSkillTooltipCreatedListener", new[] { typeof(Action<GameObject, Skills.Skill>) });
        _addTextBox = AccessTools.Method(_api, "ComplexTooltip_AddTwoColumnTextBox", new[] { typeof(GameObject) });
        _addLine = AccessTools.Method(_api, "TooltipTextBox_AddLine", new[] { typeof(GameObject), typeof(object), typeof(object), typeof(bool) });

        if (_addSkillListener == null || _addTextBox == null || _addLine == null)
        {
            Log.LogWarning("Auga's tooltip API has another shape than expected; its skill tooltips show no fortified levels or abilities.");
            _addSkillListener = null;
        }

        return true;
    }
}
