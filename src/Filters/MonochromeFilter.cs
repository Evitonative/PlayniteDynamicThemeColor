using System;
using System.Collections.Generic;
using System.Linq;
using DynamicThemeColor.Model;
using DynamicThemeColor.Model.Filters;
using DynamicThemeColor.Utils;
using JetBrains.Annotations;
using MaterialColorUtilities.ColorAppearance;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace DynamicThemeColor.Filters {
    [UsedImplicitly]
    [SchemeFilter("dc90aca7-ce80-4867-bc05-229ae6fc6963",
        "Monochrome Filter", "Evito", 1,
        "Detect sepia and black and white artwork and color them appropriately instead of cyan.")]
    public class MonochromeFilter : SchemeFilter<MonochromeFilterSettings> {
        private const uint sepiaArgbFallback = 0xFFFF7A00;
        private static readonly HashSet<Guid> bwGames = new HashSet<Guid>();
        private static readonly HashSet<Guid> sepiaGames = new HashSet<Guid>();
        private static readonly HashSet<Guid> otherGames = new HashSet<Guid>();
        private static MonochromeFilterSettings sharedSettings;

        public override uint ApplySeedFilter(
            uint currentSeed, ColorExtractionResults colorExtractionResults,
            Game game, IPlayniteAPI playniteApi,
            Func<AddonSettings> addonSettings) {
            if (Settings.SeedStageSettings) sharedSettings = Settings;

            if (game != null && !addonSettings().CacheDisabled) {
                if (bwGames.Contains(game.Id) || otherGames.Contains(game.Id)) return FilterDefault();
                if (sepiaGames.Contains(game.Id)) return MaterialColorHelper.ConvertHexToArgb(Settings.SepiaHex) ?? sepiaArgbFallback;
            }

            switch (GetMonochromeType(colorExtractionResults.QuantiserResult)) {
                case MonochromeType.Bw:
                    if (!addonSettings().CacheDisabled && game != null)
                        bwGames.Add(game.Id);
                    return FilterDefault();
                case MonochromeType.Sepia:
                    if (!addonSettings().CacheDisabled && game != null)
                        sepiaGames.Add(game.Id);
                    return MaterialColorHelper.ConvertHexToArgb(Settings.SepiaHex) ?? sepiaArgbFallback;
                case MonochromeType.None:
                    if (!addonSettings().CacheDisabled && game != null)
                        otherGames.Add(game.Id);
                    return FilterDefault();
                default:
                    throw new InvalidOperationException("Invalid Monochrome filter type");
            }

            uint FilterDefault() => base.ApplySeedFilter(currentSeed, colorExtractionResults, game, playniteApi, addonSettings);
        }

        public override void ApplyPaletteFilter(
            ThemePalette palette, ColorExtractionResults colorExtractionResults,
            Game game, IPlayniteAPI playniteApi,
            Func<AddonSettings> addonSettings) {
            if (game != null && !addonSettings().CacheDisabled)
                if (sepiaGames.Contains(game.Id) || otherGames.Contains(game.Id)) return;

            if (game == null || addonSettings().CacheDisabled || !bwGames.Contains(game.Id)) {
                switch (GetMonochromeType(colorExtractionResults.QuantiserResult)) {
                    case MonochromeType.Bw:
                        if (!addonSettings().CacheDisabled && game != null)
                            bwGames.Add(game.Id);
                        break;
                    case MonochromeType.Sepia:
                        if (!addonSettings().CacheDisabled && game != null)
                            sepiaGames.Add(game.Id);
                        return;
                    case MonochromeType.None:
                        if (!addonSettings().CacheDisabled && game != null)
                            otherGames.Add(game.Id);
                        return;
                    default:
                        throw new InvalidOperationException("Invalid Monochrome filter type");
                }
            }

            IReadOnlyCollection<int> tones = Enumerable.Range(0, 101).ToList();

            foreach (KeyValuePair<uint, uint> tone in GenerateGrayPalette(tones))
                palette.SetColor(ColorScheme.MaterialPalette.Neutral, tone.Key, tone.Value);

            foreach (KeyValuePair<uint, uint> tone in GenerateGrayPalette(tones, 0.96f))
                palette.SetColor(ColorScheme.MaterialPalette.NeutralVariant, tone.Key, tone.Value);

            foreach (KeyValuePair<uint, uint> tone in GenerateGrayPalette(tones, 1.05f, -3f))
                palette.SetColor(ColorScheme.MaterialPalette.Primary, tone.Key, tone.Value);

            foreach (KeyValuePair<uint, uint> tone in GenerateGrayPalette(tones, 0.90f,  4f))
                palette.SetColor(ColorScheme.MaterialPalette.Secondary, tone.Key, tone.Value);

            foreach (KeyValuePair<uint, uint> tone in GenerateGrayPalette(tones, 0.98f, -1f))
                palette.SetColor(ColorScheme.MaterialPalette.Tertiary, tone.Key, tone.Value);
        }

        public override uint ApplyBrushFilter(
            string brushKey, uint brushArgb,
            ColorExtractionResults colorExtractionResults,
            Game game, IPlayniteAPI playniteApi,
            Func<AddonSettings> addonSettings) {
            if (brushKey != "GlyphBrush" || game != null &&
                !addonSettings().CacheDisabled &&
                !sepiaGames.Contains(game.Id) &&
                !otherGames.Contains(game.Id)) return FilterDefault();

            MonochromeFilterSettings settings = Settings.SeedStageSettings ? sharedSettings ?? Settings : Settings;
            if (game != null && bwGames.Contains(game.Id) ||
                GetMonochromeType(colorExtractionResults?.QuantiserResult) == MonochromeType.Bw) return settings.Invert ? 0xFF000000 : 0xFFFFFFFF;

            sharedSettings = null;
            return FilterDefault();

            uint FilterDefault() => base.ApplyBrushFilter(brushKey, brushArgb, colorExtractionResults, game, playniteApi, addonSettings);
        }

        private MonochromeType GetMonochromeType([CanBeNull] IReadOnlyDictionary<uint, uint> quantiserResult) {
            if (quantiserResult == null) return MonochromeType.None;

            double totalChroma = 0;
            double averageHue = 0;
            int count = 0;

            foreach (Hct hct in quantiserResult.Keys.Select(Hct.FromInt)) {
                totalChroma += hct.Chroma;
                averageHue += hct.Hue;
                count++;
            }

            if (count == 0)
                return MonochromeType.None;

            double averageChroma = totalChroma / count;
            averageHue /= count;


            MonochromeFilterSettings settings = Settings.SeedStageSettings ? sharedSettings ?? Settings : Settings;
            if (averageChroma < settings.SepiaChromaThreshold &&
                averageHue >= settings.SepiaHueMinThreshold &&
                averageHue <= settings.SepiaHueMaxThreshold)
                return !settings.DisableSepia ? MonochromeType.Sepia : MonochromeType.None;

            return averageChroma < settings.BwChromaThreshold && !settings.DisableBw
                ? MonochromeType.Bw
                : MonochromeType.None;
        }

        private Dictionary<uint, uint> GenerateGrayPalette(
            IEnumerable<int> tones, float contrast = 1f, float brightness = 0f) {
            MonochromeFilterSettings settings = Settings.SeedStageSettings ? sharedSettings ?? Settings : Settings;

            Dictionary<uint, uint> palette = new Dictionary<uint, uint>();
            foreach (int t in tones) {
                float value = t * contrast + brightness;
                value = Math.Max(0f, Math.Min(100f, value));
                uint gray = (uint)Math.Round(value / 100f * 255f);
                uint argb = 0xFF000000u | (gray << 16) | (gray << 8) | gray;
                palette.Add((uint)t, argb ^ ((settings.Invert ? 1 : 0u) * 0x00FFFFFF));
            }
            return palette;
        }

        public override void ClearCache() {
            bwGames.Clear();
            sepiaGames.Clear();
            otherGames.Clear();
            sharedSettings = null;
        }

        private enum MonochromeType {
            Bw, Sepia, None,
        }
    }

    public class MonochromeFilterSettings {
        [BoolToggleSetting(
            Label = "Use Settings from seed stage",
            Tooltip = "Reuse the settings from the seed stage for all stages.\n" +
                      "If the filter is not applied in the seed stage the settings for the current stage will be used instead.\n" +
                      "This option must also be enabled on the seed stage itself.")]
        public bool SeedStageSettings { get; [UsedImplicitly] set; } = true;

        [BoolToggleSetting(
            Label ="Invert the B&W Theme.",
            Tooltip = "Inverts the colors used for the palette.")]
        public bool Invert { get; [UsedImplicitly] set; } = false;

        [BoolToggleSetting(
            Label = "Disable Sepia Detection",
            Tooltip = "Don't act on sepia art.")]
        public bool DisableSepia { get; [UsedImplicitly] set; } = false;

        [ColorSetting(
            Label = "Sepia Hex Color",
            Tooltip = "Seed color used when a sepia image is detect.")]
        public string SepiaHex { get; [UsedImplicitly] set; } = "#FF7A00";

        [BoolToggleSetting(
            Label = "Disable B&W Detection",
            Tooltip = "Don't act on black and white art.")]
        public bool DisableBw { get; [UsedImplicitly] set; } = false;

        [DoubleSliderSetting(
            Label = "Chroma Chroma Threshold",
            Tooltip = "Upper limit for the arts average chrome to be consider sepia.",
            Min = 0.0, Max = 50.0, Step = 0.5)]
        public double SepiaChromaThreshold { get; [UsedImplicitly] set; } = 15.0;

        [DoubleSliderSetting(
            Label = "Sepia Hue Minimum Threshold",
            Tooltip = "Lower limit for the arts average hue to be consider sepia.",
            Min = 0.0, Max = 100.0, Step = 1.0)]
        public double SepiaHueMinThreshold { get; [UsedImplicitly] set; } = 25.0;

        [DoubleSliderSetting(
            Label = "Sepia Hue Maximum Threshold",
            Tooltip = "Upper limit for the arts average hue to be consider sepia.",
            Min = 0.0, Max = 100.0, Step = 1.0)]
        public double SepiaHueMaxThreshold { get; [UsedImplicitly] set; } = 65.0;

        [DoubleSliderSetting(
            Label = "B&W Chroma Threshold",
            Tooltip = "Upper limit for the arts average chrome to be consider black and white.",
            Min = 0.0, Max = 50.0, Step = 0.5)]
        public double BwChromaThreshold { get; [UsedImplicitly] set; } = 12.0;
    }
}
