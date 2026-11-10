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
    public class SaturationFilter : SchemeFilter<SaturationFilterSettings> {
        public override Guid Id => Guid.Parse("bf7c519b-1a3e-4f0e-8c97-7292dfc04323");
        public override string Name => "Saturation Filter";
        public override string Author => "Evito";
        public override int Version => 1;
        public override string Description => "Adjust the saturation for the primary color.";

        public override uint ApplySeedFilters(uint currentSeed, ColorExtractionResults colorExtractionResults, Game game,
            IPlayniteAPI playniteApi, Func<AddonSettings> addonSettings) {
            return AdjustSaturation(currentSeed, Settings.Saturation);
        }

        public override void ApplyPaletteFilters(ThemePalette palette, ColorExtractionResults colorExtractionResults, Game game,
            IPlayniteAPI playniteApi, Func<AddonSettings> addonSettings) {

            if (Settings.PaletteToneMin < 0) Settings.PaletteToneMin = 0;
            if (Settings.PaletteToneMax < 0) Settings.PaletteToneMax = 0;
            if (Settings.PaletteToneMin > 100) Settings.PaletteToneMin = 100;
            if (Settings.PaletteToneMax > 100) Settings.PaletteToneMax = 100;
            if (Settings.PaletteToneMin > Settings.PaletteToneMax) return;
            int count = Settings.PaletteToneMax + 1 - Settings.PaletteToneMin;

            foreach (uint tone in Enumerable.Range(Settings.PaletteToneMin, count)) {
                uint previousColor = palette.GetColor(Settings.Palette, tone);
                palette.SetColor(Settings.Palette, tone, AdjustSaturation(previousColor, Settings.Saturation));
            }
        }

        public override uint ApplyBrushFilters(string brushKey, uint brushArgb, ColorExtractionResults colorExtractionResults, Game game,
            IPlayniteAPI playniteApi, Func<AddonSettings> addonSettings) {

            switch (brushKey) {
                case "GlyphBrush":
                case "HighlightGlyphBrush":
                case "CheckBoxCheckMarkBrush":
                case "MainPanelBackgroundBrush":
                    return AdjustSaturation(brushArgb, Settings.Saturation);
                default:
                    return brushArgb;
            }
        }

        private static uint AdjustSaturation(uint argb, double sat) {
            if (Math.Abs(sat) < 0.005) return argb;

            byte a = (byte)(argb >> 24);
            byte r = (byte)(argb >> 16);
            byte g = (byte)(argb >> 8);
            byte b = (byte)argb;

            double luma = 0.299 * r + 0.587 * g + 0.114 * b;
            double satMult = 1.0 + sat;

            double outR = luma + (r - luma) * satMult;
            double outG = luma + (g - luma) * satMult;
            double outB = luma + (b - luma) * satMult;

            byte finalR = (byte)Math.Max(0, Math.Min(255, Math.Round(outR)));
            byte finalG = (byte)Math.Max(0, Math.Min(255, Math.Round(outG)));
            byte finalB = (byte)Math.Max(0, Math.Min(255, Math.Round(outB)));

            return (uint)((a << 24) | (finalR << 16) | (finalG << 8) | finalB);
        }
    }

    public class SaturationFilterSettings {
        [DoubleSliderSetting(
            Label = "Saturation",
            Tooltip = "Boost (or reduce) the saturation of the accent color\n\nThis potentially results in poor contrast. Use with care.",
            Min = -1.0, Max = 4.0, Step = 0.05)]
        public double Saturation { get; [UsedImplicitly] set; } = 0.0;

        [EnumDropdownSetting(
            Label = "Applicable Palette",
            Tooltip = "Which palettes accent should be modified.")]
        public ColorScheme.MaterialPalette Palette { get; [UsedImplicitly] set; } = ColorScheme.MaterialPalette.Primary;

        [IntSliderSetting(
            Label = "Minimum Tone",
            Tooltip = "Minium Tone (generally darker color) that should be effected by the filter.",
            Min = 0, Max = 100, Step = 1)]
        public int PaletteToneMin { get; [UsedImplicitly] set; } = 70;

        [IntSliderSetting(
            Label = "Maximum Tone",
            Tooltip = "Maximum Tone (generally darker color) that should be effected by the filter.",
            Min = 0, Max = 100, Step = 1)]
        public int PaletteToneMax { get; [UsedImplicitly] set; } = 100;
    }
}
