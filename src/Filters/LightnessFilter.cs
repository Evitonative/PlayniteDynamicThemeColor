using System;
using System.Collections.Generic;
using DynamicThemeColor.Settings;

namespace DynamicThemeColor.Filters {
    public class LightnessFilter : IColorFilter {
        public string Name => "Lightness";

        public ColorProcessingStage[] ProcessingStage => new[] {
            ColorProcessingStage.Seed,
            ColorProcessingStage.Variable,
        };

        private readonly AddonSettings settings;

        public LightnessFilter(AddonSettings settings) {
            this.settings = settings;
        }

        public uint ApplyFilter(uint input) {
            double slider = settings.LightnessSlider;
            if (Math.Abs(slider) < 0.005) return input;

            byte a = (byte)(input >> 24);
            double outR = (byte)(input >> 16);
            double outG = (byte)(input >> 8);
            double outB = (byte)input;

            if (slider < 0) {
                double darkenMult = 1.0 + slider;
                outR *= darkenMult;
                outG *= darkenMult;
                outB *= darkenMult;
            } else if (slider > 0) {
                outR += (255 - outR) * slider;
                outG += (255 - outG) * slider;
                outB += (255 - outB) * slider;
            }

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
