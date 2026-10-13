using System.Collections.Generic;
using MaterialColorUtilities.Utils;

namespace DynamicThemeColor.Filters {
    public class AccentContrastFilter : IColorFilter {
        public string Name => "Accent Contrast Improvement";
        public ColorProcessingStage[] ProcessingStage { get; } = { ColorProcessingStage.Variable };

        public uint ApplyFilter(uint input) {
            return input;
        }

        public void ApplyFilter(ColorPalette palette, Dictionary<string, (uint, byte)> colorMap, ColorProcessingStage currentStage) {
            if (!colorMap.TryGetValue("AccentColor", out (uint color, byte alpha) accent)) {
                return;
            }

            double lightness = ColorUtils.LStarFromArgb(accent.color);
            uint onAccentColor = lightness <= 40
                ? palette.Palette.Primary[90]
                : palette.Palette.Primary[10];

            byte onAccentAlpha = 255;
            if (colorMap.TryGetValue("OnAccentColor", out (uint color, byte alpha) existingOnAccent)) {
                onAccentAlpha = existingOnAccent.alpha;
            }

            colorMap["OnAccentColor"] = (onAccentColor, onAccentAlpha);
        }
    }
}
