using System;
using System.Linq;
using DynamicThemeColor.Model;
using DynamicThemeColor.Model.Filters;
using DynamicThemeColor.Utils;
using JetBrains.Annotations;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace DynamicThemeColor.Filters {
    [UsedImplicitly]
    [SchemeFilter("6afe64ec-78b8-4bb5-bb74-09bd3a20b085",
        "Lightness", "Evito", 1,
        "Adjust the saturation for the primary color.")]
    public class LightnessFilter : SchemeFilter<LightnessFilterSettings> {
        public override uint ApplySeedFilter(uint currentSeed, ColorExtractionResults colorExtractionResults, Game game,
            IPlayniteAPI playniteApi, Func<AddonSettings> addonSettings) {
            return AdjustLightness(currentSeed, Settings.Lightness);
        }

        public override void ApplyPaletteFilter(ThemePalette palette, ColorExtractionResults colorExtractionResults, Game game,
            IPlayniteAPI playniteApi, Func<AddonSettings> addonSettings) {

            if (Settings.PaletteToneMin < 0) Settings.PaletteToneMin = 0;
            if (Settings.PaletteToneMax < 0) Settings.PaletteToneMax = 0;
            if (Settings.PaletteToneMin > 100) Settings.PaletteToneMin = 100;
            if (Settings.PaletteToneMax > 100) Settings.PaletteToneMax = 100;
            if (Settings.PaletteToneMin > Settings.PaletteToneMax) return;
            int count = Settings.PaletteToneMax + 1 - Settings.PaletteToneMin;

            foreach (uint tone in Enumerable.Range(Settings.PaletteToneMin, count)) {
                uint previousColor = palette.GetColor(Settings.Palette, tone);
                palette.SetColor(Settings.Palette, tone, AdjustLightness(previousColor, Settings.Lightness));
            }
        }

        public override uint ApplyBrushFilter(string brushKey, uint brushArgb, ColorExtractionResults colorExtractionResults, Game game,
            IPlayniteAPI playniteApi, Func<AddonSettings> addonSettings) {

            switch (brushKey) {
                case "GlyphBrush":
                case "HighlightGlyphBrush":
                case "CheckBoxCheckMarkBrush":
                case "MainPanelBackgroundBrush":
                    return AdjustLightness(brushArgb, Settings.Lightness);
                default:
                    return brushArgb;
            }
        }

        private static uint AdjustLightness(uint argb, double light) {
            if (Math.Abs(light) < 0.005) return argb;

            byte a = (byte)(argb >> 24);
            byte r = (byte)(argb >> 16);
            byte g = (byte)(argb >> 8);
            byte b = (byte)argb;

            double outR = r;
            double outG = g;
            double outB = b;

            if (light < 0) {
                double darkenMult = 1.0 + light;
                outR *= darkenMult;
                outG *= darkenMult;
                outB *= darkenMult;
            }
            else if (light > 0) {
                outR += (255 - outR) * light;
                outG += (255 - outG) * light;
                outB += (255 - outB) * light;
            }

            byte finalR = (byte)Math.Max(0, Math.Min(255, Math.Round(outR)));
            byte finalG = (byte)Math.Max(0, Math.Min(255, Math.Round(outG)));
            byte finalB = (byte)Math.Max(0, Math.Min(255, Math.Round(outB)));

            return (uint)((a << 24) | (finalR << 16) | (finalG << 8) | finalB);
        }
    }

    public class LightnessFilterSettings {
        [DoubleSliderSetting(
            Label = "Lightness", Stages = new []{ FilterStage.Seed, FilterStage.Palette, FilterStage.Brush },
            Tooltip = "Boost (or reduce) the lightness of the accent color\n\nThis potentially results in poor contrast. Use with care.",
            Min = -1.0, Max = 1.0, Step = 0.01)]
        public double Lightness { get; [UsedImplicitly] set; } = 0.0;

        [EnumDropdownSetting(
            Label = "Applicable Palette", Stages = new []{ FilterStage.Palette },
            Tooltip = "Which palettes accent should be modified.")]
        public ColorScheme.MaterialPalette Palette { get; [UsedImplicitly] set; } = ColorScheme.MaterialPalette.Primary;

        [IntSliderSetting(
            Label = "Minimum Tone", Stages = new []{ FilterStage.Palette },
            Tooltip = "Minium Tone (generally darker color) that should be effected by the filter.",
            Min = 0, Max = 100, Step = 1)]
        public int PaletteToneMin { get; [UsedImplicitly] set; } = 70;

        [IntSliderSetting(
            Label = "Maximum Tone", Stages = new []{ FilterStage.Palette },
            Tooltip = "Maximum Tone (generally darker color) that should be effected by the filter.",
            Min = 0, Max = 100, Step = 1)]
        public int PaletteToneMax { get; [UsedImplicitly] set; } = 100;
    }
}
