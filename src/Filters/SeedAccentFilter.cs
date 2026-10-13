using System.Collections.Generic;
using MaterialColorUtilities.Utils;

namespace DynamicThemeColor.Filters {
    public class SeedAccentFilter : IColorFilter{
        public string Name => "Seed Color For Accents";
        public ColorProcessingStage[] ProcessingStage { get; } = { ColorProcessingStage.Variable };

        public uint ApplyFilter(uint input) {
            return input;
        }

        public void ApplyFilter(ColorPalette palette, Dictionary<string, (uint, byte)> colorMap, ColorProcessingStage currentStage) {
            uint seedArgb = palette.SeedColor;

            byte accentAlpha = 255;
            if (colorMap.TryGetValue("AccentColor", out (uint color, byte alpha) existingAccent)) {
                accentAlpha = existingAccent.alpha;
            }

            colorMap["AccentColor"] = (seedArgb, accentAlpha);
        }
    }
}
