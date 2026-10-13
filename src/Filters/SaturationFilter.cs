using System;
using System.Collections.Generic;
using DynamicThemeColor.Settings;

namespace DynamicThemeColor.Filters {
    public class SaturationFilter : IColorFilter {
        public string Name => "Saturation";

        public ColorProcessingStage[] ProcessingStage => new[] {
            ColorProcessingStage.Seed,
            ColorProcessingStage.Variable,
        };

        private readonly AddonSettings settings;

        public SaturationFilter(AddonSettings settings) {
            this.settings = settings;
        }

        public uint ApplyFilter(uint input) {
            double slider = settings.SaturationSlider;
            if (Math.Abs(slider) < 0.005) return input;

            byte a = (byte)(input >> 24);
            byte r = (byte)(input >> 16);
            byte g = (byte)(input >> 8);
            byte b = (byte)input;

            double luma = 0.299 * r + 0.587 * g + 0.114 * b;
            double satMult = 1.0 + slider;

            double outR = luma + (r - luma) * satMult;
            double outG = luma + (g - luma) * satMult;
            double outB = luma + (b - luma) * satMult;

            byte finalR = (byte)Math.Max(0, Math.Min(255, Math.Round(outR)));
            byte finalG = (byte)Math.Max(0, Math.Min(255, Math.Round(outG)));
            byte finalB = (byte)Math.Max(0, Math.Min(255, Math.Round(outB)));

            return (uint)((a << 24) | (finalR << 16) | (finalG << 8) | finalB);
        }

        public void ApplyFilter(ColorPalette palette, Dictionary<string, (uint, byte)> colorMap, ColorProcessingStage currentStage) {
            if (!colorMap.TryGetValue("AccentColor", out (uint color, byte alpha) accent)) return;
            uint adjustedColor = ApplyFilter(accent.color);
            colorMap["AccentColor"] = (adjustedColor, accent.alpha);
        }
    }
}
