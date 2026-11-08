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
    public class MonochromeFilter : SchemeFilter<MonochromeFilterSettings> {
        private const uint sepiaArgb = 0xFFFF7A00;
        private static readonly HashSet<Guid> bwGames = new HashSet<Guid>();
        private static readonly HashSet<Guid> sepiaGames = new HashSet<Guid>();
        private static readonly HashSet<Guid> otherGames = new HashSet<Guid>();
        private static MonochromeFilterSettings sharedSettings;

        public override Guid Id { get; } = Guid.Parse("dc90aca7-ce80-4867-bc05-229ae6fc6963");
        public override string Name => "Monochrome Filter";

        public override uint ApplySeedFilters(
            uint currentSeed, ColorExtractionResults colorExtractionResults,
            Game game, IPlayniteAPI playniteApi,
            Func<AddonSettings> addonSettings) {
            if (Settings.SeedStageSettings) sharedSettings = Settings;

            if (game != null && !addonSettings().CacheDisabled) {
                if (bwGames.Contains(game.Id) || otherGames.Contains(game.Id)) return FilterDefault();
                if (sepiaGames.Contains(game.Id)) return sepiaArgb;
            }

            switch (GetMonochromeType(colorExtractionResults.QuantiserResult)) {
                case MonochromeType.Bw:
                    if (!addonSettings().CacheDisabled && game != null)
                        bwGames.Add(game.Id);
                    return FilterDefault();
                case MonochromeType.Sepia:
                    if (!addonSettings().CacheDisabled && game != null)
                        sepiaGames.Add(game.Id);
                    return sepiaArgb;
                case MonochromeType.None:
                    if (!addonSettings().CacheDisabled && game != null)
                        otherGames.Add(game.Id);
                    return FilterDefault();
                default:
                    throw new InvalidOperationException("Invalid Monochrome filter type");
            }

            uint FilterDefault() => base.ApplySeedFilters(currentSeed, colorExtractionResults, game, playniteApi, addonSettings);
        }

        public override void ApplyPaletteFilters(
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

        public override uint ApplyBrushFilters(
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

            uint FilterDefault() => base.ApplyBrushFilters(brushKey, brushArgb, colorExtractionResults, game, playniteApi, addonSettings);
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
            Label = "Use Settings from seed stage.",
            Tooltip = "Reuse the settings from the seed stage for all stages. " +
                      "If the filter is not applied in the seed stage the settings for the current stage will be used instead. " +
                      "This option must also be enabled on the seed stage itself.")]
        public bool SeedStageSettings { get; [UsedImplicitly] set; } = true;

        [BoolToggleSetting(Label = "Invert the B&W Theme.", Tooltip = "Invert the B&W Theme.")]
        public bool Invert { get; [UsedImplicitly] set; } = false;

        [BoolToggleSetting(Label = "Disable Sepia Detection", Tooltip = "Don't act on sepia art.")]
        public bool DisableSepia { get; [UsedImplicitly] set; } = false;

        [BoolToggleSetting(Label = "Disable B&W Detection", Tooltip = "Don't act on black and white art.")]
        public bool DisableBw { get; [UsedImplicitly] set; } = false;

        [DoubleSliderSetting(Label = "Chroma Croma Threshold", Min = 0.0, Max = 50.0, Step = 0.5)]
        public double SepiaChromaThreshold { get; [UsedImplicitly] set; } = 15.0;

        [DoubleSliderSetting(Label = "Sepia Hue Minimum Threshold", Min = 0.0, Max = 100.0, Step = 1.0)]
        public double SepiaHueMinThreshold { get; [UsedImplicitly] set; } = 25.0;

        [DoubleSliderSetting(Label = "Sepia Hue Maximum Threshold", Min = 0.0, Max = 100.0, Step = 1.0)]
        public double SepiaHueMaxThreshold { get; [UsedImplicitly] set; } = 65.0;

        [DoubleSliderSetting(Label = "B&W Croma Threshold", Min = 0.0, Max = 50.0, Step = 0.5)]
        public double BwChromaThreshold { get; [UsedImplicitly] set; } = 12.0;
    }
}
